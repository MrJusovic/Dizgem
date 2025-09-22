using Dizgem.Data;
using Dizgem.Helpers;
using Dizgem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dizgem.Controllers
{
    public class InstallController : Controller
    {
        private readonly IConnectionStringProvider _csProvider;
        private readonly IConfiguration _cfg;
        private readonly IWebHostEnvironment _env;

        public InstallController(IConnectionStringProvider csProvider, IConfiguration cfg, IWebHostEnvironment env)
        {
            _csProvider = csProvider;
            _cfg = cfg;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        private string CreateConnectionString(string server, string database, string user, string password)
        {
            // Windows kimlik doğrulaması (user/pass boş ise)
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password))
            {
                return $"Server={server};Database={database};Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True";
            }

            // SQL Server kimlik doğrulaması
            return $"Server={server};Database={database};User Id={user};Password={password};MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True";
        }

        // appsettings.json içindeki ConnectionStrings:DefaultConnection'ı güncelle
        private void SaveConnectionStringToAppSettings(string connectionString)
        {
            // İçerik kökü (proje kökü)
            var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            if (!System.IO.File.Exists(appSettingsPath))
            {
                throw new FileNotFoundException("appsettings.json bulunamadı.", appSettingsPath);
            }

            var json = System.IO.File.ReadAllText(appSettingsPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.Clone();

            // Mevcut JSON'u mutable objeye taşı
            var mutable = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                          ?? new Dictionary<string, object>();

            // ConnectionStrings düğümü
            Dictionary<string, object> connNode;
            if (mutable.ContainsKey("ConnectionStrings"))
            {
                connNode = JsonSerializer.Deserialize<Dictionary<string, object>>(mutable["ConnectionStrings"].ToString()!)
                           ?? new Dictionary<string, object>();
            }
            else
            {
                connNode = new Dictionary<string, object>();
            }

            connNode["DefaultConnection"] = connectionString;
            mutable["ConnectionStrings"] = connNode;

            // Pretty print ile geri yaz
            var newJson = JsonSerializer.Serialize(mutable, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(appSettingsPath, newJson);
        }

        // POST: /Install/ - Bağlantı Testini Yapar
        [HttpPost]
        [HttpPost]
        public IActionResult TestConnection([FromBody] InstallViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.DatabaseServer) ||
                string.IsNullOrWhiteSpace(model.DatabaseName))
            {
                return Json(new { success = false, message = "Sunucu ve veritabanı adı zorunludur." });
            }

            var connectionString = CreateConnectionString(
                model.DatabaseServer,
                model.DatabaseName,
                model.DatabaseUser,
                model.DatabasePassword
            );

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();  // Kimlik bilgisi / ağ erişimi doğru mu?
                conn.Close();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                LoggerHelper.LogException(ex);
                return Json(new { success = false, message = "Bağlantı başarısız: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompleteInstallation([FromBody] InstallViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
            }

            // Veritabanı bağlantı dizesini oluştur
            var connectionString = CreateConnectionString(model.DatabaseServer, model.DatabaseName, model.DatabaseUser, model.DatabasePassword);

            try
            {
                // 1) appsettings.json'a yaz
                SaveConnectionStringToAppSettings(connectionString);

                // 2) Bellekte güncelle (sonraki scope'larda DbContext yeni CS ile açılacak)
                _csProvider.Set(connectionString);

                // 3) Yeni scope ile migrate + seed
                using var scope = HttpContext.RequestServices.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.MigrateAsync();

                var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                // Admin rolünü oluştur
                const string adminRole = "Admin";
                if (!await roleMgr.RoleExistsAsync(adminRole))
                {
                    var roleCreate = await roleMgr.CreateAsync(new IdentityRole<Guid>(adminRole));
                    if (!roleCreate.Succeeded)
                    {
                        var msg = string.Join(", ", roleCreate.Errors.Select(e => e.Description));
                        return Json(new { success = false, message = "Rol oluşturulamadı: " + msg });
                    }
                }

                // Kullanıcı ve şifreyi manuel olarak hashle ve normalleştir

                var email = (model.AdminEmail ?? string.Empty).Trim();
                var userName = model.AdminUsername; // model.AdminUsername isterseniz kullanabilirsiniz

                var adminUser = await userMgr.FindByEmailAsync(email);
                if (adminUser == null)
                {
                    adminUser = new User
                    {
                        UserName = userName,
                        Email = email,
                        EmailConfirmed = true,     // Confirm şartını devre dışı bırakmadıysanız bu gerekli
                        DisplayName = "Admin"
                    };

                    var createRes = await userMgr.CreateAsync(adminUser, model.AdminPassword);
                    if (!createRes.Succeeded)
                    {
                        var msg = string.Join(", ", createRes.Errors.Select(e => e.Description));
                        return Json(new { success = false, message = "Admin kullanıcı oluşturulamadı: " + msg });
                    }

                    var roleRes = await userMgr.AddToRoleAsync(adminUser, adminRole);
                    if (!roleRes.Succeeded)
                    {
                        var msg = string.Join(", ", roleRes.Errors.Select(e => e.Description));
                        return Json(new { success = false, message = "Rol atama hatası: " + msg });
                    }
                }


                // 4) Site ayarlarını kaydet (varsa güncelle)
                var siteTitle = await db.Settings.FirstOrDefaultAsync(s => s.Key == "SiteTitle");
                var siteDesc = await db.Settings.FirstOrDefaultAsync(s => s.Key == "SiteDescription");

                if (siteTitle == null)
                    db.Settings.Add(new Settings { Key = "SiteTitle", Value = model.SiteTitle ?? "Dizgem" });
                else
                    siteTitle.Value = model.SiteTitle ?? "Dizgem";

                if (siteDesc == null)
                    db.Settings.Add(new Settings { Key = "SiteDescription", Value = model.SiteDescription ?? "" });
                else
                    siteDesc.Value = model.SiteDescription ?? "";

                await db.SaveChangesAsync();

                // 5) Başarı
                return Json(new { success = true, redirectUrl = "/Dizgem/Account/Login" });
            }
            catch (Exception ex)
            {
                LoggerHelper.LogException(ex);
                return Json(new { success = false, message = "Kurulum sırasında bir hata oluştu: " + ex.Message });
            }
        }

        // appsettings.json dosyasını güncelleyen metot
        //private void UpdateAppSettings(string newConnectionString)
        //{
        //    var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
        //    var jsonText = System.IO.File.ReadAllText(appSettingsPath);
        //    var jsonObject = JsonNode.Parse(jsonText);

        //    if (jsonObject != null && jsonObject["ConnectionStrings"] is JsonObject connectionStrings)
        //    {
        //        connectionStrings["DefaultConnection"] = newConnectionString;
        //        var updatedJson = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
        //        System.IO.File.WriteAllText(appSettingsPath, updatedJson);
        //    }
        //}
    }
}
