using System.Globalization;

namespace Dizgem.Models
{
    /// <summary>
    /// Blog arşivi için tek bir öğeyi temsil eder (örn: Eylül 2025 (12))
    /// </summary>
    public class PostArchiveItemViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int PostCount { get; set; }

        /// <summary>
        /// Ay adını Türkçe olarak döndürür. Örn: "Eylül"
        /// </summary>
        public string MonthName => new CultureInfo("tr-TR").DateTimeFormat.GetMonthName(Month);
    }
}
