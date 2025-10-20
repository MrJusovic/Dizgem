using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    public enum CommentStatus
    {
        [Display(Name = "Genel Ayarı Kullan")]
        UseGlobal = 0,

        [Display(Name = "Yorumlara İzin Ver")]
        Open = 1,

        [Display(Name = "Yorumlara İzin Verme")]
        Closed = 2
    }
}
