using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class EnableAuthenticatorViewModel
    {
        [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
        [StringLength(6, ErrorMessage = "{0} en az {2} ve en fazla {1} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [Display(Name = "Doğrulama Kodu")]
        public string Code { get; set; }

        public string SharedKey { get; set; }
        public string AuthenticatorUri { get; set; }
    }

    public class LoginWith2faViewModel
    {
        [Required(ErrorMessage = "Doğrulama kodu zorunludur.")]
        [StringLength(7, ErrorMessage = "{0} en az {2} ve en fazla {1} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [Display(Name = "Doğrulama Kodu")]
        public string TwoFactorCode { get; set; }

        [Display(Name = "Bu makineyi hatırla")]
        public bool RememberMachine { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ShowRecoveryCodesViewModel
    {
        public string[] RecoveryCodes { get; set; }
    }
}
