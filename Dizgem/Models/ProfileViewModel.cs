using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Görünen ad zorunludur.")]
        [Display(Name = "Görünen Ad")]
        public string DisplayName { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Yeni Şifre")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre (Tekrar)")]
        [Compare("NewPassword", ErrorMessage = "Yeni şifreler eşleşmiyor.")]
        public string? ConfirmPassword { get; set; }

        // Güvenlik Ayarları
        public bool Is2faEnabled { get; set; }
        public int RecoveryCodesLeft { get; set; }
        public bool HasAuthenticator { get; set; }
        public string? StatusMessage { get; set; }
    }
}
