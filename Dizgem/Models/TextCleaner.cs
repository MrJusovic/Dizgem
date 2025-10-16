using System.Text.RegularExpressions;

public static class TextCleaner
{
    /// <summary>
    /// Bir metin içerisindeki çoklu boşlukları tek boşluğa, 
    /// çoklu boş satırları ise tek bir satır arasına indirger.
    /// </summary>
    /// <param name="inputText">Temizlenecek metin.</param>
    /// <returns>Temizlenmiş metin.</returns>
    public static string CleanWhitespace(this string inputText)
    {
        if (string.IsNullOrEmpty(inputText))
        {
            return inputText;
        }

        // 1. Adım: Birden fazla olan yatay boşlukları tek boşluğa indirge.
        // Örnek: "Hello     World" -> "Hello World"
        string step1 = Regex.Replace(inputText, @"[ ]+", " ");

        // 2. Adım: İki veya daha fazla olan yeni satır karakterini tek bir yeni satıra indirge.
        // Bu, Windows (\r\n) ve Unix/Linux (\n) formatlarındaki satır sonlarını da kapsar.
        // Örnek: "Satır1\n\n\nSatır2" -> "Satır1\nSatır2"
        string step2 = Regex.Replace(step1, @"(\r\n|\n){2,}", "\n");

        // 3. Adım (Önerilir): Metnin başındaki ve sonundaki tüm boşlukları temizle.
        string cleanedText = step2.Trim();

        step2 = Regex.Replace(cleanedText, @"(\r\n|\n){2,}", "\n");

        // 3. Adım (Önerilir): Metnin başındaki ve sonundaki tüm boşlukları temizle.
        cleanedText = step2.Trim();

        return cleanedText;
    }
}
