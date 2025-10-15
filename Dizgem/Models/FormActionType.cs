using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    /// <summary>
    /// Form gönderildiğinde hangi aksiyonun alınacağını belirtir.
    /// </summary>
    public enum FormActionType
    {
        [Display(Name = "E-posta Gönder")]
        SendEmail = 1,

        [Display(Name = "Veritabanına Kaydet")]
        SaveToDatabase = 2
    }
}
