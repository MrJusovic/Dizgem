using System.ComponentModel.DataAnnotations;

namespace Dizgem.Models
{
    /// <summary>
    /// GrapesJS formlarından gelen verileri saklamak için kullanılır.
    /// </summary>
    public class FormSubmission
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid FormHandlerId { get; set; }
        public FormHandler FormHandler { get; set; }

        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Formdan gelen tüm verilerin JSON formatında serileştirilmiş hali.
        /// </summary>
        public string DataJson { get; set; }
    }
}
