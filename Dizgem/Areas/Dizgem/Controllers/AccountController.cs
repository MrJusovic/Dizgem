using Dizgem.Models;
using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Serilog;
using System.Text;

namespace Dizgem.Areas.Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
        }
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home", new { area = "Dizgem" });
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
            
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home", new { area = "Dizgem" });
            }
            else
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Önce kullanıcıyı kullanıcı adına göre ara
            var user = await _userManager.FindByNameAsync(model.Username);

            // Eğer kullanıcı adı bulunamazsa, e-posta adresine göre ara
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync(model.Username);
            }

            // Eğer kullanıcı bulunduysa, şifre ile giriş yapmayı dene
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    Log.Information($"Kullanıcı {user.UserName} başarıyla giriş yaptı.");
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home", new { area = "Dizgem" });
                    }
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction("LoginWith2fa", "Account", new { area = "Dizgem", rememberMe = model.RememberMe, returnUrl = returnUrl });
                }
                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                    return View(model);
                }

                //if (result.Succeeded)
                //{
                //    Log.Information($"Kullanıcı {user.UserName} başarıyla giriş yaptı.");
                //    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                //    {
                //        return Redirect(returnUrl);
                //    }
                //    else
                //    {
                //        return RedirectToAction("Index", "Home", new { area = "Dizgem" });
                //    }
                //}
            }

            // Giriş başarısızsa hata mesajı göster
            ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return NotFound("İki faktörlü kimlik doğrulama kullanıcısı yüklenemedi.");
            }
            var model = new LoginWith2faViewModel { RememberMe = rememberMe };
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return NotFound("İki faktörlü kimlik doğrulama kullanıcısı yüklenemedi.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, model.RememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home", new { area = "Dizgem" });
                }
            }
            else if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Geçersiz doğrulama kodu.");
                return View(model);
            }
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Profile(string? statusMessage = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                DisplayName = user.DisplayName,
                Email = user.Email,
                Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                StatusMessage = statusMessage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
                return View(model);
            }

            if (user.DisplayName != model.DisplayName)
            {
                user.DisplayName = model.DisplayName;
                await _userManager.UpdateAsync(user);
            }

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, "OldPasswordPlaceholder", model.NewPassword);
                // Not: Güvenlik için eski şifre de istenmelidir. Bu örnekte basitleştirilmiştir.
                if (!changePasswordResult.Succeeded)
                {
                    // Hataları işle
                }
            }
            // Kullanıcının claim'lerini içeren cookie'nin yenilenmesini tetiklemek için
            // güvenlik damgasını güncelliyoruz.
            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction("Index", new { statusMessage = "Profil başarıyla güncellendi." });
        }

        [HttpPost]
        public async Task<IActionResult> CustomizeProfile([FromBody]CustomizeProfileModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return NotFound();
            }

            switch (model.field)
            {
                case "color_theme_layout":
                    user.color_theme_layout = model.val;
                    break;
                case "theme_layout":
                    user.theme_layout = model.val;
                    break;
                case "page_layout":
                    user.page_layout = model.val;
                    break;
                case "layout":
                    user.layout = model.val;
                    break;
                case "sidebar_type":
                    user.sidebar_type = model.val;
                    break;
                default:
                    break;
            }

            await _userManager.UpdateAsync(user);


            // Kullanıcının claim'lerini içeren cookie'nin yenilenmesini tetiklemek için
            // güvenlik damgasını güncelliyoruz.
            await _userManager.UpdateSecurityStampAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            return Ok(new { statusMessage = "Profil başarıyla güncellendi." });
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
           return View();

        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Kullanıcı bulunamazsa veya e-postası onaylı değilse, yine de onay sayfasına yönlendir.
                    // Bu, sistemde hangi e-postaların kayıtlı olduğunun dışarı sızmasını engeller.
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Action("ResetPassword", "Account", new { area = "Dizgem", code, email = user.Email }, protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(
                    model.Email,
                    "Dizgem Şifre Sıfırlama Talebi",
                    $"Lütfen şifrenizi sıfırlamak için <a href='{callbackUrl}'>buraya tıklayın</a>.");

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null, string email = null)
        {
            if (code == null || email == null)
            {
                return BadRequest("Şifre sıfırlama için bir kod ve e-posta sağlanmalıdır.");
            }
            var model = new ResetPasswordViewModel { Code = code, Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Kullanıcı bulunamazsa bile hata mesajı verme, başarıya yönlendir.
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }

            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            var result = await _userManager.ResetPasswordAsync(user, code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            // Bu, şifrenin başarıyla sıfırlandığını belirten ve giriş yapmaya yönlendiren bir view.
            return View();
        }
    }
}
