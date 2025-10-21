using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi girin.")]
        public string Email { get; set; }
    }
}
