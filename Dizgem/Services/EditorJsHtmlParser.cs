using Dizgem.Helpers;
using Ganss.Xss;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace Dizgem.Services
{
    public class EditorJsHtmlParser : IEditorJsHtmlParser
    {
        private readonly IHtmlSanitizer _htmlSanitizer;

        // Adım 1: DI (Dependency Injection) ile Program.cs'de yapılandırdığımız
        // IHtmlSanitizer servisini constructor'da enjekte ediyoruz.
        public EditorJsHtmlParser(IHtmlSanitizer htmlSanitizer)
        {
            _htmlSanitizer = htmlSanitizer;
        }

        public string Parse(string editorJsJson)
        {
            if (string.IsNullOrWhiteSpace(editorJsJson))
                return "";

            string generatedHtml;
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(editorJsJson))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("blocks", out JsonElement blocks))
                    {
                        // JSON'dan ham HTML'i oluşturan mevcut metodumuz
                        generatedHtml = ParseBlocks(blocks);
                    }
                    else
                    {
                        return "";
                    }
                }
            }
            catch (JsonException)
            {
                return "<p>İçerik formatı hatalı.</p>";
            }

            // Adım 2: Oluşturulan ham HTML'i, veritabanına göndermeden önce
            // IHtmlSanitizer servisimizden geçirerek temizliyoruz.
            string sanitizedHtml = _htmlSanitizer.Sanitize(generatedHtml);

            // Adım 3: Sadece güvenli ve temizlenmiş HTML'i geri döndürüyoruz.
            return sanitizedHtml;
        }

        public string SanitizeRawBlocks(string editorJsJson)
        {
            if (string.IsNullOrWhiteSpace(editorJsJson))
                return editorJsJson;

            try
            {
                var rootNode = JsonNode.Parse(editorJsJson);
                if (rootNode == null) return editorJsJson;

                var blocks = rootNode["blocks"]?.AsArray();
                if (blocks == null) return editorJsJson;

                foreach (var blockNode in blocks)
                {
                    if (blockNode?["type"]?.GetValue<string>() == "raw")
                    {
                        var dataNode = blockNode["data"];
                        if (dataNode?["html"] != null)
                        {
                            string rawHtml = dataNode["html"].GetValue<string>();
                            // Sadece bu bloğun içeriğini temizle
                            string sanitizedHtml = _htmlSanitizer.Sanitize(rawHtml);
                            // Temizlenmiş HTML'i JSON'daki yerine koy
                            dataNode["html"] = JsonValue.Create(sanitizedHtml);
                        }
                    }
                }

                // Değiştirilmiş JSON'u string olarak geri döndür
                return rootNode.ToJsonString();
            }
            catch (JsonException ex)
            {
                LoggerHelper.LogException(ex);
                // Parse hatası olursa orijinal veriyi geri döndür, bozma.
                return editorJsJson;
            }
        }

        // Bu metodun içeriğinde bir değişiklik yapmaya gerek yok.
        private string ParseBlocks(JsonElement blocks)
        {
            var stringBuilder = new StringBuilder();
            foreach (JsonElement block in blocks.EnumerateArray())
            {
                // Her ihtimale karşı 'type' ve 'data' property'lerinin varlığını kontrol edelim.
                if (!block.TryGetProperty("type", out var typeElement) || !block.TryGetProperty("data", out var data))
                {
                    continue; // Geçerli bir blok değilse atla
                }

                string type = typeElement.GetString();

                switch (type)
                {
                    case "header":
                        int level = data.GetProperty("level").GetInt32();
                        stringBuilder.Append($"<h{level}>{HttpUtility.HtmlEncode(data.GetProperty("text").GetString())}</h{level}>");
                        break;

                    case "paragraph":
                        // Paragraf içinde <a>, <b>, <i> gibi inline etiketler olabileceği için
                        // metni doğrudan alıyoruz. Sanitizer zaten bunları temizleyecektir.
                        stringBuilder.Append($"<p>{data.GetProperty("text").GetString()}</p>");
                        break;

                    case "list":
                        string listTag = data.GetProperty("style").GetString() == "ordered" ? "ol" : "ul";
                        stringBuilder.Append($"<{listTag}>");
                        foreach (JsonElement item in data.GetProperty("items").EnumerateArray())
                        {
                            stringBuilder.Append($"<li>{item.GetString()}</li>"); // Inline etiketler için encode etmiyoruz
                        }
                        stringBuilder.Append($"</{listTag}>");
                        break;

                    case "image":
                        string url = data.GetProperty("file").GetProperty("url").GetString();
                        data.TryGetProperty("caption", out var captionElement);
                        string caption = captionElement.GetString() ?? "";
                        stringBuilder.Append($"<figure><img src='{url}' alt='{HttpUtility.HtmlEncode(caption)}' class='img-fluid' /><figcaption>{HttpUtility.HtmlEncode(caption)}</figcaption></figure>");
                        break;

                    case "layout":
                        // Bu kısım zaten doğru çalışıyor, olduğu gibi bırakıyoruz.
                        if (data.TryGetProperty("layout", out var layout) && data.TryGetProperty("items", out var columns))
                        {
                            string[] colLayouts = layout.GetString()?.Split('-');
                            if (colLayouts == null) break;
                            int totalRatio = colLayouts.Select(int.Parse).Sum();
                            stringBuilder.Append("<div class='row'>");
                            for (int i = 0; i < columns.GetArrayLength(); i++)
                            {
                                int ratio = int.Parse(colLayouts[i]);
                                int colWidth = (int)Math.Round((double)ratio / totalRatio * 12);
                                stringBuilder.Append($"<div class='col-md-{colWidth}'>");
                                stringBuilder.Append(ParseBlocks(columns[i]));
                                stringBuilder.Append("</div>");
                            }
                            stringBuilder.Append("</div>");
                        }
                        break;

                    // === YENİ EKLENEN BLOK TÜRLERİ ===

                    case "code":
                        // Code bloğunun içeriği HER ZAMAN encode edilmelidir.
                        string codeContent = HttpUtility.HtmlEncode(data.GetProperty("code").GetString());
                        stringBuilder.Append($"<pre><code class='prettyprint'>{codeContent}</code></pre>");
                        break;

                    case "quote":
                        string quoteText = data.GetProperty("text").GetString();
                        data.TryGetProperty("caption", out var quoteCaptionElement);
                        string quoteCaption = quoteCaptionElement.GetString() ?? "";
                        stringBuilder.Append($"<blockquote><p>{quoteText}</p><footer>{HttpUtility.HtmlEncode(quoteCaption)}</footer></blockquote>");
                        break;

                    case "delimiter":
                        stringBuilder.Append("<hr>");
                        break;

                    case "table":
                        data.TryGetProperty("withHeadings", out var withHeadingsElement);
                        bool hasHeader = withHeadingsElement.GetBoolean();
                        stringBuilder.Append("<table class='table table-bordered'>");
                        if (data.TryGetProperty("content", out var rows))
                        {
                            for (int i = 0; i < rows.GetArrayLength(); i++)
                            {
                                var row = rows[i];
                                if (i == 0 && hasHeader)
                                {
                                    stringBuilder.Append("<thead><tr>");
                                    foreach (var cell in row.EnumerateArray())
                                    {
                                        stringBuilder.Append($"<th>{cell.GetString()}</th>");
                                    }
                                    stringBuilder.Append("</tr></thead><tbody>");
                                }
                                else
                                {
                                    if (i == 1 && !hasHeader) stringBuilder.Append("<tbody>"); // Başlık yoksa ilk satırda tbody'yi başlat
                                    if (i == 0 && !hasHeader) stringBuilder.Append("<tbody>"); // Tek satırlı ve başlıksız tablo için
                                    stringBuilder.Append("<tr>");
                                    foreach (var cell in row.EnumerateArray())
                                    {
                                        stringBuilder.Append($"<td>{cell.GetString()}</td>");
                                    }
                                    stringBuilder.Append("</tr>");
                                }
                            }
                            stringBuilder.Append("</tbody>");
                        }
                        stringBuilder.Append("</table>");
                        break;

                    case "embed":
                        // Güvenlik için sadece belirli servislere izin veriyoruz.
                        string service = data.GetProperty("service").GetString()?.ToLowerInvariant();
                        var allowedServices = new[] { "youtube", "vimeo", "coub" };
                        if (allowedServices.Contains(service))
                        {
                            string embedUrl = data.GetProperty("embed").GetString();
                            stringBuilder.Append($"<div class='ratio ratio-16x9'><iframe src='{embedUrl}' allowfullscreen></iframe></div>");
                        }
                        break;

                    case "warning":
                        data.TryGetProperty("title", out var warningTitleElement);
                        data.TryGetProperty("message", out var warningMessageElement);
                        stringBuilder.Append($"<div class='alert alert-warning'><strong>{HttpUtility.HtmlEncode(warningTitleElement.GetString())}</strong> {warningMessageElement.GetString()}</div>");
                        break;
                    case "raw":
                        stringBuilder.Append(data.GetProperty("html").GetString());
                        break;
                }
            }
            return stringBuilder.ToString();
        }
    }
}
