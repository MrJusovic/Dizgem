using Dizgem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Encodings.Web;

namespace Dizgem.Controllers
{
    [Area("Dizgem")]
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly UrlEncoder _urlEncoder;

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public ManageController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            UrlEncoder urlEncoder)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _urlEncoder = urlEncoder;
        }

        /// <summary>
        /// AJAX ile çağrılır ve modal pencerede QR kod oluşturmak için
        /// gerekli bilgileri JSON formatında döndürür.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAuthenticatorSetupInfo()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Mevcut anahtarı sıfırlayıp yenisini oluşturuyoruz.
            await _userManager.ResetAuthenticatorKeyAsync(user);
            var token = await _userManager.GetAuthenticatorKeyAsync(user);

            var authenticatorUri = string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode("Dizgem CMS"), // QR Kodu okutan uygulamada görünecek başlık
                _urlEncoder.Encode(user.Email),
                token);

            // Gerekli bilgileri JSON olarak döndür.
            return Json(new { sharedKey = token, authenticatorUri = authenticatorUri });
        }

        /// <summary>
        /// Modal'dan gelen doğrulama kodunu AJAX ile alır ve işler.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAuthenticatorCode([FromBody] EnableAuthenticatorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Geçersiz kod formatı." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                return Json(new { success = false, message = "Doğrulama kodu geçersiz." });
            }

            // Kod doğruysa 2FA'yı etkinleştir ve kurtarma kodlarını oluştur.
            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            // Başarılı sonucu ve kurtarma kodlarını JSON olarak döndür.
            return Json(new { success = true, recoveryCodes = recoveryCodes.ToArray() });
        }

        /// <summary>
        /// 2FA'yı devre dışı bırakmak için AJAX ile çağrılır.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2fa()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2faResult.Succeeded)
            {
                return StatusCode(500, new { success = false, message = "2FA devre dışı bırakılamadı." });
            }

            // 2FA devre dışı bırakıldıktan sonra anahtarı sıfırlamak iyi bir pratiktir.
            await _userManager.ResetAuthenticatorKeyAsync(user);

            return Json(new { success = true });
        }
    }
}
