using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public class AiRequestViewModel
    {
        [Required(ErrorMessage = "İşlenecek metin boş olamaz.")]
        public string Text { get; set; }

        public string PromptHint { get; set; } // Opsiyonel: "blog yazısı tonunda", "resmi bir dille" gibi ipuçları
    }
}
