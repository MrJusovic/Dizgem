using Dizgem;
using Dizgem.Data;
using Dizgem.Filters;
using Dizgem.Middleware;
using Dizgem.Models;
using Dizgem.Services;
using Ganss.Xss;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using System;
using System.Reflection;

// UYGULAMA BAŞLANGIÇ NOKTASI

// Güncelleme kontrolünü ve işlemini yapacak statik metot
static void ApplyUpdateIfAvailable()
{
    var rootPath = Directory.GetCurrentDirectory();
    var flagPath = Path.Combine(rootPath, "update.flag");
    var updateSourcePath = Path.Combine(rootPath, "_update", "new_version");
    var backupPath = Path.Combine(rootPath, "_backup_" + Guid.NewGuid().ToString("N").Substring(0, 8));

    if (!File.Exists(flagPath))
        return;

    void Log(string message)
    {
        var logFilePath = Path.Combine(rootPath, "update_log.txt");
        File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
    }

    Log("Güncelleme başlatıldı.");

    try
    {
        // ← ÖNEMLİ: Tüm içerik kopyalansın diye kök olarak doğrudan new_version'ı al.
        var sourceRoot = updateSourcePath;

        if (!Directory.Exists(sourceRoot) || !Directory.EnumerateFileSystemEntries(sourceRoot).Any())
        {
            Log("Kaynak dizin boş veya yok. İşlem iptal.");
            return;
        }

        Log($"Kaynak dizin: {sourceRoot}");

        Log("Yedekleme klasörü oluşturuluyor: " + backupPath);
        Directory.CreateDirectory(backupPath);

        // Tüm dosyaları tek tek işle (akışlı): 
        var sourceFiles = Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories).ToList();
        Log($"{sourceFiles.Count} adet dosya kopyalanacak.");

        // 1) Var olanları yedekleyip taşı (gölgele)
        foreach (var file in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceRoot, file);
            var destinationPath = Path.Combine(rootPath, relativePath);

            if (File.Exists(destinationPath))
            {
                try
                {
                    var backupFilePath = Path.Combine(backupPath, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(backupFilePath)!);
                    Log($"Yedekleniyor ve yeniden adlandırılıyor: {destinationPath}");
                    File.Move(destinationPath, backupFilePath);
                }
                catch (Exception ex2)
                {
                    Log($"[ERR] Move(backup) : {ex2.Message}");
                }
            }
        }

        // 2) Yeni dosyaları yerine kopyala (üzerine yazma davranışı biz taşıdığımız için burada net)
        Log("Yeni dosyalar kopyalanıyor...");
        foreach (var file in sourceFiles)
        {
            try
            {
                var relativePath = Path.GetRelativePath(sourceRoot, file);
                var destinationPath = Path.Combine(rootPath, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(file, destinationPath, overwrite: true);
            }
            catch (Exception ex2)
            {
                Log($"[ERR] Relative Move(backup) : {ex2.Message}");
            }

        }

        Log("Güncelleme başarıyla tamamlandı. Temizlik yapılıyor.");
    }
    catch (Exception ex)
    {
        Log($"HATA OLUŞTU: {ex.Message}");
        Log("Güncelleme geri alınıyor...");
        try
        {
            if (Directory.Exists(backupPath))
            {
                foreach (var file in Directory.EnumerateFiles(backupPath, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(backupPath, file);
                    var destinationPath = Path.Combine(rootPath, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    File.Copy(file, destinationPath, overwrite: true);
                }
            }
            Log("Geri alma işlemi tamamlandı.");
        }
        catch (Exception rollbackEx)
        {
            Log($"GERİ ALMA SIRASINDA HATA: {rollbackEx.Message}");
        }
    }
    finally
    {
        Log("Geçici dosyalar siliniyor...");
        try
        {
            var updateFolder = Path.Combine(rootPath, "_update");
            if (Directory.Exists(updateFolder))
                Directory.Delete(updateFolder, recursive: true);

            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, recursive: true);

            // Güncelleme başarıyla bittiyse bayrağı kaldırmak çoğu senaryoda istenir:
            // if (File.Exists(flagPath)) File.Delete(flagPath);
        }
        catch (Exception cleanupEx)
        {
            Log($"Temizlik sırasında hata: {cleanupEx.Message}");
        }

        Log("Temizlik tamamlandı.");
    }
}

// 2. ADIM: Veritabanı migration'ını uygular ve geçici dosyaları temizler.
static void ApplyMigrationsAndCleanup(IHost app)
{
    var rootPath = Directory.GetCurrentDirectory();
    var flagPath = Path.Combine(rootPath, "update.flag");

    // Sadece bir güncelleme yapıldıysa bu bloğu çalıştır.
    if (!File.Exists(flagPath))
    {
        return;
    }

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Güncelleme bayrağı bulundu. Veritabanı migration'ları uygulanıyor...");
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var csProvider = services.GetRequiredService<IConnectionStringProvider>();

            if (!string.IsNullOrWhiteSpace(csProvider.Current))
            {
                dbContext.Database.Migrate();
                logger.LogInformation("Veritabanı migration'ları başarıyla uygulandı.");
            }
            else
            {
                logger.LogWarning("Migration atlanıyor: Veritabanı bağlantısı bulunamadı.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Güncelleme sonrası veritabanı migration'ı sırasında bir hata oluştu.");
        }
        finally
        {
            // İşlem başarılı da olsa başarısız da olsa güncelleme dosyalarını ve bayrağını temizle.
            logger.LogInformation("Güncelleme dosyaları temizleniyor.");
            if (Directory.Exists(Path.Combine(rootPath, "_update")))
            {
                Directory.Delete(Path.Combine(rootPath, "_update"), true);
            }
            File.Delete(flagPath);
        }
    }
}

// === GÜNCELLEMEYİ UYGULA ===
// Bu komut, Program.cs'deki diğer her şeyden önce çalışmalıdır.
ApplyUpdateIfAvailable();

static void CleanupOldVersions()
{
    var rootPath = Directory.GetCurrentDirectory();
    var entryAssembly = Assembly.GetEntryAssembly();
    if (entryAssembly == null)
        return;

    var currentAssemblyName = entryAssembly.GetName().Name;
    var currentVersionDll = Path.GetFileName(entryAssembly.Location);
    var currentBaseName = Path.GetFileNameWithoutExtension(currentVersionDll);

    // "Dizgem_v*.dll" formatında eski sürümleri bul
    var versionedDlls = Directory.GetFiles(rootPath, $"{currentAssemblyName}_v*.dll", SearchOption.TopDirectoryOnly);

    foreach (var dll in versionedDlls)
    {
        var dllName = Path.GetFileName(dll);

        // Şu anki çalışanın kendisi değilse, silebiliriz
        if (!dllName.Equals(currentVersionDll, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Dosya erişim kısıtlıysa bayrakları temizle
                if (File.Exists(dll))
                    File.SetAttributes(dll, FileAttributes.Normal);

                File.Delete(dll);
                Console.WriteLine($"Silindi: {dllName}");

                // Aynı isimde .pdb, .deps.json, .runtimeconfig.json dosyalarını da kaldır
                var relatedExtensions = new[] { ".pdb", ".deps.json", ".runtimeconfig.json" };
                foreach (var ext in relatedExtensions)
                {
                    var relatedPath = Path.ChangeExtension(dll, ext);
                    if (File.Exists(relatedPath))
                    {
                        try
                        {
                            File.SetAttributes(relatedPath, FileAttributes.Normal);
                            File.Delete(relatedPath);
                            Console.WriteLine($"Silindi: {Path.GetFileName(relatedPath)}");
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"[WARN] {relatedPath} silinemedi: {ex2.Message}");
                        }
                    }
                }
            }
            catch (IOException ioex)
            {
                // Dosya kilitliyse, bir sonraki açılışta silinmek üzere yeniden adlandır
                try
                {
                    var tempName = dll + ".delete";
                    File.Move(dll, tempName, overwrite: true);
                    Console.WriteLine($"Kilitliydi, yeniden adlandırıldı: {dllName}");
                }
                catch
                {
                    Console.WriteLine($"[WARN] {dllName} silinemedi: {ioex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERR] {dllName} silinemedi: {ex.Message}");
            }
        }
    }
}

// === TEMİZLİĞİ BAŞLAT ===
CleanupOldVersions();




Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);



builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();
});

// === YENİ EKLENECEK HTML SANITIZER AYARI ===
builder.Services.AddSingleton<IHtmlSanitizer>(provider =>
{
    // Yeni bir sanitizer nesnesi oluşturuyoruz.
    var sanitizer = new HtmlSanitizer();

    // Temizleme işleminden sonra kalmasına izin verdiğimiz HTML etiketleri:
    sanitizer.AllowedTags.Add("a");
    sanitizer.AllowedTags.Add("abbr");
    sanitizer.AllowedTags.Add("address");
    sanitizer.AllowedTags.Add("area");
    sanitizer.AllowedTags.Add("article");
    sanitizer.AllowedTags.Add("aside");
    sanitizer.AllowedTags.Add("audio");
    sanitizer.AllowedTags.Add("b");
    sanitizer.AllowedTags.Add("bdi");
    sanitizer.AllowedTags.Add("bdo");
    sanitizer.AllowedTags.Add("blockquote");
    sanitizer.AllowedTags.Add("br");
    sanitizer.AllowedTags.Add("button");
    sanitizer.AllowedTags.Add("canvas");
    sanitizer.AllowedTags.Add("caption");
    sanitizer.AllowedTags.Add("cite");
    sanitizer.AllowedTags.Add("code");
    sanitizer.AllowedTags.Add("col");
    sanitizer.AllowedTags.Add("colgroup");
    sanitizer.AllowedTags.Add("data");
    sanitizer.AllowedTags.Add("datalist");
    sanitizer.AllowedTags.Add("del");
    sanitizer.AllowedTags.Add("details");
    sanitizer.AllowedTags.Add("dfn");
    sanitizer.AllowedTags.Add("dialog");
    sanitizer.AllowedTags.Add("div");
    sanitizer.AllowedTags.Add("dl");
    sanitizer.AllowedTags.Add("dt");
    sanitizer.AllowedTags.Add("em");
    sanitizer.AllowedTags.Add("fieldset");
    sanitizer.AllowedTags.Add("figcaption");
    sanitizer.AllowedTags.Add("figure");
    sanitizer.AllowedTags.Add("footer");
    sanitizer.AllowedTags.Add("form");
    sanitizer.AllowedTags.Add("h1");
    sanitizer.AllowedTags.Add("h2");
    sanitizer.AllowedTags.Add("h3");
    sanitizer.AllowedTags.Add("h4");
    sanitizer.AllowedTags.Add("h5");
    sanitizer.AllowedTags.Add("h6");
    sanitizer.AllowedTags.Add("header");
    sanitizer.AllowedTags.Add("hgroup");
    sanitizer.AllowedTags.Add("hr");
    sanitizer.AllowedTags.Add("i");
    sanitizer.AllowedTags.Add("img");
    sanitizer.AllowedTags.Add("input");
    sanitizer.AllowedTags.Add("ins");
    sanitizer.AllowedTags.Add("kbd");
    sanitizer.AllowedTags.Add("label");
    sanitizer.AllowedTags.Add("legend");
    sanitizer.AllowedTags.Add("li");
    sanitizer.AllowedTags.Add("main");
    sanitizer.AllowedTags.Add("map");
    sanitizer.AllowedTags.Add("mark");
    sanitizer.AllowedTags.Add("menu");
    sanitizer.AllowedTags.Add("meter");
    sanitizer.AllowedTags.Add("nav");
    sanitizer.AllowedTags.Add("object");
    sanitizer.AllowedTags.Add("ol");
    sanitizer.AllowedTags.Add("optgroup");
    sanitizer.AllowedTags.Add("option");
    sanitizer.AllowedTags.Add("output");
    sanitizer.AllowedTags.Add("p");
    sanitizer.AllowedTags.Add("param");
    sanitizer.AllowedTags.Add("picture");
    sanitizer.AllowedTags.Add("pre");
    sanitizer.AllowedTags.Add("progress");
    sanitizer.AllowedTags.Add("q");
    sanitizer.AllowedTags.Add("rp");
    sanitizer.AllowedTags.Add("rt");
    sanitizer.AllowedTags.Add("ruby");
    sanitizer.AllowedTags.Add("s");
    sanitizer.AllowedTags.Add("samp");
    sanitizer.AllowedTags.Add("section");
    sanitizer.AllowedTags.Add("select");
    sanitizer.AllowedTags.Add("small");
    sanitizer.AllowedTags.Add("source");
    sanitizer.AllowedTags.Add("span");
    sanitizer.AllowedTags.Add("strong");
    sanitizer.AllowedTags.Add("style");
    sanitizer.AllowedTags.Add("sub");
    sanitizer.AllowedTags.Add("summary");
    sanitizer.AllowedTags.Add("sup");
    //sanitizer.AllowedTags.Add("svg");
    //sanitizer.AllowedTags.Add("rect");
    //sanitizer.AllowedTags.Add("polygon");
    sanitizer.AllowedTags.Add("table");
    sanitizer.AllowedTags.Add("tbody");
    sanitizer.AllowedTags.Add("td");
    sanitizer.AllowedTags.Add("template");
    sanitizer.AllowedTags.Add("textarea");
    sanitizer.AllowedTags.Add("tfoot");
    sanitizer.AllowedTags.Add("th");
    sanitizer.AllowedTags.Add("thead");
    sanitizer.AllowedTags.Add("time");
    sanitizer.AllowedTags.Add("tr");
    sanitizer.AllowedTags.Add("track");
    sanitizer.AllowedTags.Add("u");
    sanitizer.AllowedTags.Add("ul");
    sanitizer.AllowedTags.Add("var");
    sanitizer.AllowedTags.Add("wbr");

    // İzin verilen özellikler (attributes)
    sanitizer.AllowedAttributes.Add("href");
    sanitizer.AllowedAttributes.Add("src");
    sanitizer.AllowedAttributes.Add("alt");
    sanitizer.AllowedAttributes.Add("class"); // Bootstrap sınıfları için
    sanitizer.AllowedAttributes.Add("style"); 
    sanitizer.AllowedAttributes.Add("rtl"); 
    sanitizer.AllowedAttributes.Add("type"); 
    sanitizer.AllowedAttributes.Add("value"); 
    sanitizer.AllowedAttributes.Add("name"); 
    sanitizer.AllowedAttributes.Add("id"); 
    sanitizer.AllowedAttributes.Add("media"); 
    sanitizer.AllowedAttributes.Add("data-gjs-type"); 
    sanitizer.AllowedAttributes.Add("gjs-highlightable"); 
    sanitizer.AllowedAttributes.Add("data-masonry"); 


    // Yapılandırılmış sanitizer nesnesini döndürüyoruz.
    return sanitizer;
});

// -----------------------------
// Serilog (appsettings kontrollü)
// -----------------------------
bool loglamaAcik = builder.Configuration.GetValue<bool>("LoglamaAyarlari:Aktif");
if (loglamaAcik)
{
    builder.Host.UseSerilog((context, loggerConfig) =>
    {
        loggerConfig
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("Logs/dizgem-.txt", rollingInterval: RollingInterval.Day);
    });
}

// -----------------------------
// Services (DI)
// -----------------------------
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

builder.Services.AddHttpClient();

builder.Services.AddMemoryCache();


builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new ActiveThemeViewLocationExpander());
});

// ConnectionString provider: kurulumdan sonra bellekte güncellenebilir
builder.Services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

// DbContext: her scope'ta provider'dan güncel connection string'i al
builder.Services.AddDbContext<ApplicationDbContext>((sp, opts) =>
{
    var prov = sp.GetRequiredService<IConnectionStringProvider>();
    var cs = prov.Current;

    // Kurulum tamamlanana kadar boş olabilir; boşsa SQL Server'ı bağlama
    if (!string.IsNullOrWhiteSpace(cs))
    {
        opts.UseSqlServer(cs);
    }
});

// Identity
builder.Services
    .AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;

        // Kurulumda admini EmailConfirmed=true oluşturacaksan gerek yok:
        // options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, CustomUserClaimsPrincipalFactory>();


builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie'nin temel ayarları
    options.Cookie.HttpOnly = true;

    // Giriş yapılmamışsa (401 Unauthorized) yönlendirilecek sayfa.
    // Projenizde bir AccountController ve Login action'ı olduğunu varsayıyoruz.
    options.LoginPath = "/Dizgem/Account/Login";

    // Giriş yapılmış ANCAK yetkisi yoksa (403 Forbidden) yönlendirilecek sayfa.
    // Örneğin normal bir kullanıcının admin paneline girmeye çalışması.
    options.AccessDeniedPath = "/Dizgem/Account/AccessDenied";

    // Kullanıcının güvenlik damgasının ne sıklıkla kontrol edileceğini belirler.
    // Bu, rolleri veya claim'leri değişen bir kullanıcının oturumunun
    // otomatik olarak güncellenmesini sağlar.
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(180);
});

// Tema Ayarları için kullanılan scope
builder.Services.AddScoped<Ensure2faFilter>();
builder.Services.AddScoped<CustomUserClaimsPrincipalFactory>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IEditorJsHtmlParser, EditorJsHtmlParser>();
builder.Services.AddScoped<ISlugService, SlugService>();
builder.Services.AddScoped<IExcerptService, ExcerptService>();
builder.Services.AddScoped<ISeoService, SeoService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<IUpdateService, UpdateService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddScoped<IFormProcessingService, FormProcessingService>();
builder.Services.AddScoped<IThemeEditorService, ThemeEditorService>();


var app = builder.Build();

ApplyMigrationsAndCleanup(app);

// -----------------------------
// Middleware Pipeline
// -----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Bu middleware, diğer yönlendirme middleware'lerinden ÖNCE gelmelidir.
// Gelen istekteki proxy başlıklarını okur ve request şemasını (http/https) günceller.
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();



var themesPath = Path.Combine(builder.Environment.ContentRootPath, "Themes");
if (Directory.Exists(themesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(themesPath),
        RequestPath = "/themes" // Tarayıcıda bu URL ile erişilecek
    });
}

app.UseThemeStaticFiles();


app.UseRouting();

app.UseMiddleware<FormHandlerMiddleware>();

// Auth middleware HER ZAMAN aktif olmalı
app.UseAuthentication();
app.UseAuthorization();

// Kurulum kontrolü (per-request)
// Connection string yoksa /Install'a yönlendir; /Install üzerindeyken bırak geçsin.
app.Use(async (ctx, next) =>
{
    var prov = ctx.RequestServices.GetRequiredService<IConnectionStringProvider>();
    bool needInstall = string.IsNullOrWhiteSpace(prov.Current);
    bool onInstall = ctx.Request.Path.StartsWithSegments("/Install", StringComparison.OrdinalIgnoreCase);

    if (needInstall && !onInstall)
    {
        ctx.Response.Redirect("/Install");
        return;
    }

    await next();
});


// -----------------------------
// Routes
// -----------------------------

// 1. Admin Area Route'u
// /dizgem admin girişi için route tanımı
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// /Install ile başlayan tüm istekleri, PageDetail route'undan önce yakalar
// ve doğruca InstallController'a yönlendirir.
app.MapControllerRoute(
    name: "Install",
    pattern: "Install/{action=Index}/{id?}",
    defaults: new { controller = "Install" });

// 2. Arşiv Sayfası Route'u
// /archive/YYYY/MM formatındaki URL'leri yakalar.
app.MapControllerRoute(
    name: "PostArchive",
    pattern: "archive/{year:int:min(2000)}/{month:int:range(1,12)}",
    defaults: new { controller = "Post", action = "Index" });

// 3. Yazı Detay Sayfası Route'u
// /post/slug-degeri formatındaki URL'leri yakalar.
app.MapControllerRoute(
    name: "PostDetail",
    pattern: "Post/{slug}",
    defaults: new { controller = "Post", action = "Detail" });

app.MapControllerRoute(
    name: "PostIndex",
    pattern: "Post",
    defaults: new { controller = "Post", action = "Index" });

app.MapControllerRoute(
    name: "PageDetail",
    pattern: "{slug}",
    defaults: new { controller = "Page", action = "Detail" });

// 4. Varsayılan Route
// Diğer tüm istekleri karşılar. Her zaman en sonda olmalıdır.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// İstersen özel bir "install" route’u da ayrıca açık tutabilirsin;
// ancak default route zaten /Install/Index’i de çözer.
// app.MapControllerRoute(
//     name: "install",
//     pattern: "{controller=Install}/{action=Index}/{id?}");

app.Run();
