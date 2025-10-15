using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    /// <summary>
    /// Kullanıcı tarafından oluşturulan bir formun nasıl işleneceğini tanımlar.
    /// </summary>
    public class FormHandler
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Display(Name = "İşleyici Adı")]
        [Required(ErrorMessage = "İşleyici adı zorunludur.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Display(Name = "Benzersiz Tanımlayıcı")]
        [Required]
        [StringLength(150)]
        public string UniqueIdentifier { get; set; }

        [Display(Name = "Aksiyon Türü")]
        [Required(ErrorMessage = "Lütfen bir aksiyon türü seçin.")]
        public FormActionType ActionType { get; set; }

        [Display(Name = "Aksiyon Hedefi")]
        [Required(ErrorMessage = "Aksiyon hedefi zorunludur.")]
        [StringLength(255)]
        public string ActionTarget { get; set; }

        [Display(Name = "Başarı Mesajı")]
        [Required(ErrorMessage = "Başarı mesajı zorunludur.")]
        [StringLength(500)]
        public string SuccessMessage { get; set; }
    }
}
