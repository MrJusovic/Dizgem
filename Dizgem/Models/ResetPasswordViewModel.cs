using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class ResetPasswordViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Yeni şifre zorunludur.")]
        [StringLength(100, ErrorMessage = "{0} en az {2} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre (Tekrar)")]
        [Compare("Password", ErrorMessage = "Şifreler uyuşmuyor.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }
}
