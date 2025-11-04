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
using Microsoft.IdentityModel.Logging;
using Serilog;
using System;
using System.Reflection;
using System.Text.Json;

// UYGULAMA BAŞLANGIÇ NOKTASI

// Güncelleme kontrolünü ve işlemini yapacak statik metot
bool ApplyUpdateIfAvailable(Microsoft.Extensions.Logging.ILogger logger)
{
    var rootPath = Directory.GetCurrentDirectory();
    var flagPath = Path.Combine(rootPath, "update.flag");
    var updateSourcePath = Path.Combine(rootPath, "_update", "new_version");
    var backupPath = Path.Combine(rootPath, "_backup_" + Guid.NewGuid().ToString("N")[..8]);

    if (!File.Exists(flagPath))
        return false;

    void Log(string m) => File.AppendAllText(Path.Combine(rootPath, "update_log.txt"),
        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {m}{Environment.NewLine}");

    Log("Güncelleme başlatıldı.");

    try
    {
        var sourceRoot = updateSourcePath;
        if (!Directory.Exists(sourceRoot) || !Directory.EnumerateFileSystemEntries(sourceRoot).Any())
        {
            Log("Kaynak dizin boş veya yok. Update iptal.");
            return false;
        }

        Log($"Kaynak dizin: {sourceRoot}");

        Directory.CreateDirectory(backupPath);

        var sourceFiles = Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories).ToList();
        Log($"{sourceFiles.Count} adet dosya kopyalanacak.");

        foreach (var file in sourceFiles)
        {
            var rel = Path.GetRelativePath(sourceRoot, file);
            var dest = Path.Combine(rootPath, rel);

            if (File.Exists(dest))
            {
                try
                {
                    var bak = Path.Combine(backupPath, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(bak)!);
                    Log($"Yedekleniyor: {dest}");
                    File.Move(dest, bak);
                }
                catch (Exception ex2)
                {
                    Log($"[ERR] backup: {ex2.Message}");
                }
            }
        }

        foreach (var file in sourceFiles)
        {
            var rel = Path.GetRelativePath(sourceRoot, file);
            var dest = Path.Combine(rootPath, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }

        Log("Güncelleme dosyaları kopyalandı (bayrak korunuyor).");
        return true; // ← kritik: restart tetiklenecek
    }
    catch (Exception ex)
    {
        Log($"HATA: {ex.Message}");
        // rollback (kısaltılmış)
        try
        {
            if (Directory.Exists(backupPath))
            {
                foreach (var f in Directory.EnumerateFiles(backupPath, "*", SearchOption.AllDirectories))
                {
                    var rel = Path.GetRelativePath(backupPath, f);
                    var dest = Path.Combine(rootPath, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(f, dest, overwrite: true);
                }
            }
        }
        catch (Exception rex) { Log($"ROLLBACK HATASI: {rex.Message}"); }
        return false;
    }
    finally
    {
        Log("Geçici dosyalar siliniyor...");
        try
        {
            var updateFolder = Path.Combine(rootPath, "_update");
            if (Directory.Exists(updateFolder))
            {
                Directory.Delete(updateFolder, recursive: true);
            }
            if (Directory.Exists(backupPath))
            {

                string uploadsPath = Path.Combine(backupPath, "wwwroot", "uploads");
                if (Directory.Exists(uploadsPath))
                {
                    string rootUploadsPath = Path.Combine(rootPath, "wwwroot", "uploads");
                    Directory.Move(uploadsPath, rootUploadsPath);
                }

                Directory.Delete(backupPath, recursive: true);
            }
        }
        catch (Exception cleanupEx)
        {
            Log($"Temizlik sırasında hata: {cleanupEx.Message}");
        }
        Log("Temizlik tamamlandı.");
    }
}

// 2. ADIM: Veritabanı migration'ını uygular ve geçici dosyaları temizler.
void ApplyMigrationsAndCleanup(IHost app)
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
            if (File.Exists(flagPath))
            {
                File.Delete(flagPath);
            }
        }
    }
}

// === GÜNCELLEMEYİ UYGULA ===
// Bu komut, Program.cs'deki diğer her şeyden önce çalışmalıdır.
//ApplyUpdateIfAvailable();

async void CleanupOldVersions()
{
    var rootPath = Directory.GetCurrentDirectory();
    var manifestPath = Path.Combine(rootPath, "update.manifest.json");

    // Sadece bir güncelleme yapıldıysa bu bloğu çalıştır.
    if (!File.Exists(manifestPath))
    {
        return;
    }

    try
    {
        using var fs = File.OpenRead(manifestPath);
        var json = await JsonDocument.ParseAsync(fs);
        if (json.RootElement.TryGetProperty("remove", out var removeArray) && removeArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in removeArray.EnumerateArray())
            {
                var relPath = item.GetString();
                if (string.IsNullOrWhiteSpace(relPath)) continue;

                var targetPath = Path.Combine(rootPath, relPath);
                if (Directory.Exists(targetPath))
                {
                    Directory.Delete(targetPath, true);
                }
                else if (File.Exists(targetPath))
                {
                    try { File.SetAttributes(targetPath, FileAttributes.Normal); } catch { }
                    File.Delete(targetPath);
                }
            }
        }
        File.Delete(manifestPath);

    }
    catch (Exception ex)
    {
        LogHelper.LogWarning($"Manifest uygulanamadı: {ex.Message}");
    }
}


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
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IGitEventsService, GitEventsService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IMediaService, MediaService>();


var app = builder.Build();

bool updateApplied = ApplyUpdateIfAvailable(app.Logger); // ← sadece True/False dönsün

if (updateApplied)
{
    // Uygulama tam start olduktan 500ms sonra nazikçe durdur.
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            app.Logger.LogInformation("Update uygulandı. Yeni binayı yüklemek için yeniden başlatılıyor...");
            app.Lifetime.StopApplication();
        });
    });
}
else
{
    // Update bayrağı yoksa ya da update bu sefer koşmadıysa, normal akışta migration + temizlik yap.
    // (Eğer bayrağı özellikle bir sonraki açılışa bırakmak istiyorsan, bu bloğu aşağıdaki gibi 2. açılışa taşı)
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        ApplyMigrationsAndCleanup(app);      // yeni binayla çalışacak (updateApplied=false ise zaten flag yoktur ve no-op)
        CleanupOldVersions();                // opsiyonel: eski “_v*.dll”leri sil
    });
}

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
