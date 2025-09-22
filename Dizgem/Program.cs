using Dizgem;
using Dizgem.Data;
using Dizgem.Middleware;
using Dizgem.Models;
using Dizgem.Services;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using System;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownProxies.Clear();
        options.KnownNetworks.Clear();
});

// === YEN� EKLENECEK HTML SANITIZER AYARI ===
builder.Services.AddSingleton<IHtmlSanitizer>(provider =>
{
    // Yeni bir sanitizer nesnesi olu�turuyoruz.
    var sanitizer = new HtmlSanitizer();

    // Temizleme i�leminden sonra kalmas�na izin verdi�imiz HTML etiketleri:
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

    // �zin verilen �zellikler (attributes)
    sanitizer.AllowedAttributes.Add("href");
    sanitizer.AllowedAttributes.Add("src");
    sanitizer.AllowedAttributes.Add("alt");
    sanitizer.AllowedAttributes.Add("class"); // Bootstrap s�n�flar� i�in
    sanitizer.AllowedAttributes.Add("style"); 
    sanitizer.AllowedAttributes.Add("rtl"); 
    sanitizer.AllowedAttributes.Add("type"); 
    sanitizer.AllowedAttributes.Add("value"); 
    sanitizer.AllowedAttributes.Add("name"); 
    sanitizer.AllowedAttributes.Add("id"); 
    sanitizer.AllowedAttributes.Add("media"); 


    // Yap�land�r�lm�� sanitizer nesnesini d�nd�r�yoruz.
    return sanitizer;
});

// -----------------------------
// Serilog (appsettings kontroll�)
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
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();


builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new ActiveThemeViewLocationExpander());
});

// ConnectionString provider: kurulumdan sonra bellekte g�ncellenebilir
builder.Services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

// DbContext: her scope'ta provider'dan g�ncel connection string'i al
builder.Services.AddDbContext<ApplicationDbContext>((sp, opts) =>
{
    var prov = sp.GetRequiredService<IConnectionStringProvider>();
    var cs = prov.Current;

    // Kurulum tamamlanana kadar bo� olabilir; bo�sa SQL Server'� ba�lama
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

        // Kurulumda admini EmailConfirmed=true olu�turacaksan gerek yok:
        // options.SignIn.RequireConfirmedEmail = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie'nin temel ayarlar�
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(180);

    // Giri� yap�lmam��sa (401 Unauthorized) y�nlendirilecek sayfa.
    // Projenizde bir AccountController ve Login action'� oldu�unu varsay�yoruz.
    options.LoginPath = "/Dizgem/Account/Login";

    // Giri� yap�lm�� ANCAK yetkisi yoksa (403 Forbidden) y�nlendirilecek sayfa.
    // �rne�in normal bir kullan�c�n�n admin paneline girmeye �al��mas�.
    options.AccessDeniedPath = "/Dizgem/Account/AccessDenied";

    options.SlidingExpiration = true;
});

// Tema Ayarlar� i�in kullan�lan scope
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IEditorJsHtmlParser, EditorJsHtmlParser>();
builder.Services.AddScoped<ISlugService, SlugService>();
builder.Services.AddScoped<IExcerptService, ExcerptService>();
builder.Services.AddScoped<ISeoService, SeoService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IMenuService, MenuService>();


var app = builder.Build();

// -----------------------------
// Middleware Pipeline
// -----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Bu middleware, di�er y�nlendirme middleware'lerinden �NCE gelmelidir.
// Gelen istekteki proxy ba�l�klar�n� okur ve request �emas�n� (http/https) g�nceller.
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();


var themesPath = Path.Combine(builder.Environment.ContentRootPath, "Themes");
if (Directory.Exists(themesPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(themesPath),
        RequestPath = "/themes" // Taray�c�da bu URL ile eri�ilecek
    });
}

app.UseThemeStaticFiles();

app.UseRouting();

// Auth middleware HER ZAMAN aktif olmal�
app.UseAuthentication();
app.UseAuthorization();

// Kurulum kontrol� (per-request)
// Connection string yoksa /Install'a y�nlendir; /Install �zerindeyken b�rak ge�sin.
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
// /dizgem admin giri�i i�in route tan�m�
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// /Install ile ba�layan t�m istekleri, PageDetail route'undan �nce yakalar
// ve do�ruca InstallController'a y�nlendirir.
app.MapControllerRoute(
    name: "Install",
    pattern: "Install/{action=Index}/{id?}",
    defaults: new { controller = "Install" });

// 2. Ar�iv Sayfas� Route'u
// /archive/YYYY/MM format�ndaki URL'leri yakalar.
app.MapControllerRoute(
    name: "PostArchive",
    pattern: "archive/{year:int:min(2000)}/{month:int:range(1,12)}",
    defaults: new { controller = "Post", action = "Index" });

// 3. Yaz� Detay Sayfas� Route'u
// /post/slug-degeri format�ndaki URL'leri yakalar.
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

// 4. Varsay�lan Route
// Di�er t�m istekleri kar��lar. Her zaman en sonda olmal�d�r.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// �stersen �zel bir "install" route�u da ayr�ca a��k tutabilirsin;
// ancak default route zaten /Install/Index�i de ��zer.
// app.MapControllerRoute(
//     name: "install",
//     pattern: "{controller=Install}/{action=Index}/{id?}");

app.Run();
