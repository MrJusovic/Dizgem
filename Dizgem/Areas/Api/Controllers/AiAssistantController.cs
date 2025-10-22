using Dizgem.Models;
using Dizgem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace Dizgem.Areas.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AiAssistantController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISettingsService _settingService;

        public AiAssistantController(IHttpClientFactory httpClientFactory, ISettingsService settingsService)
        {
            _httpClientFactory = httpClientFactory;
            _settingService = settingsService;
        }

        [HttpPost("ImproveText")]
        public async Task<IActionResult> ImproveText([FromBody] AiRequestViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model?.Text))
            {
                return BadRequest(new AiResponseViewModel { Success = false, ErrorMessage = "İşlenecek metin boş olamaz." });
            }

            string systemPrompt = "Aşağıdaki metni dilbilgisi, akıcılık ve okunabilirlik açısından iyileştir. Anlamını değiştirme, sadece daha profesyonel ve ilgi çekici hale getir.";
            string userQuery = model.Text;

            try
            {
                string generatedText = await CallGeminiApiAsync(systemPrompt, userQuery);
                return Ok(new AiResponseViewModel { Success = true, GeneratedText = generatedText });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AiResponseViewModel { Success = false, ErrorMessage = $"Metin iyileştirilirken hata oluştu: {ex.Message}" });
            }
        }

        // === YENİ METOTLAR ===

        [HttpPost("SuggestTitles")]
        public async Task<IActionResult> SuggestTitles([FromBody] AiRequestViewModel request)
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
            {
                return BadRequest(new AiResponseViewModel { Success = false, ErrorMessage = "Başlık önermek için içerik metni boş olamaz." });
            }

            string systemPrompt = "Aşağıdaki içerik için SEO uyumlu, ilgi çekici ve kısa 5 adet blog yazısı başlığı öner. Sadece başlıkları liste olarak döndür, başına numara veya tire koyma, her başlığı yeni bir satıra yaz.";
            string userQuery = request.Text; // İçeriğin ilk ~500 karakterini göndermek daha verimli olabilir.

            try
            {
                string generatedText = await CallGeminiApiAsync(systemPrompt, userQuery);
                var titles = generatedText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return Ok(new AiResponseViewModel { Success = true, Suggestions = titles.ToList() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AiResponseViewModel { Success = false, ErrorMessage = $"Başlık önerileri alınırken hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost("GenerateSummary")]
        public async Task<IActionResult> GenerateSummary([FromBody] AiRequestViewModel request)
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
            {
                return BadRequest(new AiResponseViewModel { Success = false, ErrorMessage = "Özet oluşturmak için içerik metni boş olamaz." });
            }

            string systemPrompt = "Aşağıdaki metnin kısa (en fazla 2 cümle), ilgi çekici bir özetini ve SEO uyumlu bir meta açıklamasını oluştur. Önce özeti 'Özet:' başlığıyla, sonra açıklamayı 'Açıklama:' başlığıyla yeni satırlarda döndür.";
            string userQuery = request.Text;

            try
            {
                string generatedText = await CallGeminiApiAsync(systemPrompt, userQuery);
                // Yanıtı parse edip ilgili alanlara ayıralım (Basit bir örnek)
                string summary = "";
                string description = "";
                var lines = generatedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.StartsWith("Özet:")) summary = line.Substring("Özet:".Length).Trim();
                    else if (line.StartsWith("Açıklama:")) description = line.Substring("Açıklama:".Length).Trim();
                }

                return Ok(new AiResponseViewModel { Success = true, GeneratedSummary = summary, GeneratedDescription = description });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AiResponseViewModel { Success = false, ErrorMessage = $"Özet oluşturulurken hata oluştu: {ex.Message}" });
            }
        }

        [HttpPost("GenerateCoverImage")]
        public async Task<IActionResult> GenerateCoverImage([FromBody] AiRequestViewModel request)
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
            {
                return BadRequest(new AiResponseViewModel { Success = false, ErrorMessage = "Resim oluşturmak için metin (başlık veya özet) boş olamaz." });
            }

            // Imagen 3 modelini kullanacağız (API anahtarı aynı)
            string userPrompt = $"Blog yazısı için kapak fotoğrafı: {request.Text}"; // Prompt'u özelleştirebilirsiniz

            try
            {
                if (string.IsNullOrEmpty(_settingService.Current.GeminiAPIKey))
                {
                    return StatusCode(500, new AiResponseViewModel { Success = false, ErrorMessage = "Görsel oluşturma için API anahtarı yapılandırılmamış." });
                }

                string imageUrl = await CallImagenApiAsync(userPrompt, _settingService.Current.GeminiAPIKey);
                return Ok(new AiResponseViewModel { Success = true, GeneratedImageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AiResponseViewModel { Success = false, ErrorMessage = $"Kapak fotoğrafı oluşturulurken hata oluştu: {ex.Message}" });
            }
        }

        // =======================

        // Gemini metin üretme API'sini çağıran metot
        private async Task<string> CallGeminiApiAsync(string systemPrompt, string userQuery, int maxRetries = 3)
        {
            if (string.IsNullOrEmpty(_settingService.Current.GeminiAPIKey))
            {
                throw new InvalidOperationException("Gemini API anahtarı (GeminiApiKey) yapılandırmada bulunamadı.");
            }

            const string model = "gemini-2.5-flash-preview-09-2025";
            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_settingService.Current.GeminiAPIKey}";

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = userQuery } } } },
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } }
            };

            var httpClient = _httpClientFactory.CreateClient();
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            int attempt = 0;
            TimeSpan delay = TimeSpan.FromSeconds(1);

            while (attempt < maxRetries)
            {
                try
                {
                    var response = await httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                        {
                            // Daha sağlam JSON parse etme
                            var candidate = doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0 ? candidates[0] : default;
                            var text = candidate.TryGetProperty("content", out var contentElement) && contentElement.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0 ? parts[0].TryGetProperty("text", out var textElement) ? textElement.GetString() : null : null;

                            if (!string.IsNullOrEmpty(text)) { return text.Trim(); }
                            else { throw new Exception("AI yanıtında geçerli metin bulunamadı."); }
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // 429
                    {
                        attempt++;
                        if (attempt >= maxRetries) throw new Exception($"AI API hız limitine ulaşıldı ve {maxRetries} deneme başarısız oldu.");
                        await Task.Delay(delay);
                        delay *= 2;
                        continue;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"AI API hatası: {response.StatusCode} - {errorContent}");
                    }
                }
                catch (HttpRequestException httpEx) when (attempt < maxRetries - 1) // Son deneme hariç tekrar dene
                {
                    attempt++;
                    await Task.Delay(delay);
                    delay *= 2;
                }
                // Diğer hatalar veya son denemedeki ağ hatası doğrudan fırlatılır
            }
            throw new Exception($"AI API'sine yapılan tüm denemeler ({maxRetries}) başarısız oldu."); // Bu satıra normalde ulaşılmamalı
        }

        // Imagen 3 resim üretme API'sini çağıran metot
        private async Task<string> CallImagenApiAsync(string prompt, string apiKey, int maxRetries = 3)
        {
            // Imagen 3 modelini ve API endpoint'ini kullanıyoruz
            const string model = "imagen-3.0-generate-002"; // Önerilen model
            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:predict?key={apiKey}";

            var payload = new
            {
                instances = new[] { new { prompt = prompt } },
                parameters = new { sampleCount = 1 } // Sadece 1 resim istiyoruz
            };

            var httpClient = _httpClientFactory.CreateClient();
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            int attempt = 0;
            TimeSpan delay = TimeSpan.FromSeconds(1);

            while (attempt < maxRetries)
            {
                try
                {
                    var response = await httpClient.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                        {
                            var prediction = doc.RootElement.TryGetProperty("predictions", out var predictions) && predictions.GetArrayLength() > 0 ? predictions[0] : default;
                            var base64Image = prediction.TryGetProperty("bytesBase64Encoded", out var imageElement) ? imageElement.GetString() : null;

                            if (!string.IsNullOrEmpty(base64Image))
                            {
                                // Base64 verisini data URL formatına çevir
                                return $"data:image/png;base64,{base64Image}";
                            }
                            else
                            {
                                throw new Exception("AI yanıtında geçerli resim verisi bulunamadı.");
                            }
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // 429
                    {
                        attempt++;
                        if (attempt >= maxRetries) throw new Exception($"Imagen API hız limitine ulaşıldı ve {maxRetries} deneme başarısız oldu.");
                        await Task.Delay(delay);
                        delay *= 2;
                        continue;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Imagen API hatası: {response.StatusCode} - {errorContent}");
                    }
                }
                catch (HttpRequestException httpEx) when (attempt < maxRetries - 1)
                {
                    attempt++;
                    await Task.Delay(delay);
                    delay *= 2;
                }
            }
            throw new Exception($"Imagen API'sine yapılan tüm denemeler ({maxRetries}) başarısız oldu.");
        }
    }
}
