using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryPOS.Models;

namespace InventoryPOS.Services
{
    /// <summary>
    /// Integrates with the Google AI (Gemini) Generative Language API to analyze
    /// item pictures and produce structured listing attributes (title, description,
    /// size, brand, category, sub-category).
    /// </summary>
    /// <remarks>
    /// Requires a valid Gemini API key configured via <see cref="UiState.GoogleAiApiKey"/>.
    /// The API key is passed by the caller; this class does not persist it.
    /// </remarks>
    public class GoogleAiService : IDisposable
    {
        // A single HttpClient instance per service lifetime avoids socket exhaustion.
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly LoggerService _logger;
        private bool _disposed;

        // Gemini model — pinned for deterministic output behaviour.
        private const string ModelName = "gemini-1.5-flash";

        // Prompt asking the model to return ONLY JSON with the exact fields we consume.
        private const string SystemPrompt =
            "You are a helpful e-commerce assistant. Analyze the uploaded images of a clothing item and return ONLY a JSON object — no extra text, no markdown formatting, no code fences. The JSON must have exactly these fields:\n" +
            "title: a concise product title, max 80 characters\n" +
            "description: a detailed product description suitable for a resale listing\n" +
            "size: the clothing size shown on the item (e.g. XS, S, M, L, XL, 2XL, One Size) — do NOT include measurements or units like 'inches' or 'cm'\n" +
            "brand: the brand name if visible, otherwise \"Unknown\"\n" +
            "category: a broad product category (e.g. Clothing, Accessories, Shoes)\n" +
            "subCategory: a specific sub-category (e.g. Shirt, Pants, Dress, Jacket, Sweater, Skirt, Hat)";

        public GoogleAiService(string apiKey)
        {
            _apiKey = apiKey;
            _logger = LoggerService.Instance;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(90);
        }

        /// <summary>
        /// Uploads up to the first three images for <paramref name="imagePaths"/>
        /// to the Gemini API and returns the parsed <see cref="AiFillResult"/>.
        /// Returns <c>null</c> when the API call fails or the response cannot be parsed.
        /// </summary>
        public async Task<AiFillResult?> AnalyzeImagesAsync(List<string> imagePaths)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogError("GoogleAiService invoked without an API key.");
                return null;
            }

            if (imagePaths == null || imagePaths.Count == 0)
            {
                _logger.LogWarning("GoogleAiService received an empty image list.");
                return null;
            }

            // Take only the first three images as requested.
            var imagesToSend = imagePaths.Take(3).ToList();

            try
            {
                var requestJson = BuildRequestJson(imagesToSend);
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={_apiKey}";

                _logger.LogInfo($"Sending {imagesToSend.Count} image(s) to Gemini API.");

                using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                using var httpResponse = await _httpClient.PostAsync(endpoint, content);

                var responseBody = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"Gemini API returned HTTP {(int)httpResponse.StatusCode}: {responseBody}");
                    return null;
                }

                // Extract the generated text from the first candidate.
                using var doc = JsonDocument.Parse(responseBody);
                var text = ExtractResponseText(doc);

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Gemini API response contained no text.");
                    return null;
                }

                var result = ParseAiResponse(text);
                if (result == null)
                {
                    _logger.LogWarning($"Failed to parse Gemini response as JSON. Raw text: {text[..Math.Min(200, text.Length)]}...");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("GoogleAiService.AnalyzeImagesAsync failed", ex);
                return null;
            }
        }

        /// <summary>
        /// Builds the JSON request payload for the Gemini API, embedding each
        /// image as a base-64 <c>inlineData</c> part alongside the system prompt.
        /// </summary>
        private string BuildRequestJson(List<string> imagePaths)
        {
            var parts = new List<Dictionary<string, object?>>
            {
                new() { ["text"] = SystemPrompt }
            };

            foreach (var path in imagePaths)
            {
                var bytes = File.ReadAllBytes(path);
                var base64 = Convert.ToBase64String(bytes);
                var mimeType = GetMimeType(path);

                parts.Add(new Dictionary<string, object?>
                {
                    ["inlineData"] = new Dictionary<string, object?>
                    {
                        ["mimeType"] = mimeType,
                        ["data"] = base64
                    }
                });
            }

            var request = new Dictionary<string, object>
            {
                ["contents"] = new List<Dictionary<string, object?>>
                {
                    new() { ["role"] = "user", ["parts"] = parts }
                }
            };

            return JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }

        /// <summary>
        /// Navigates the Gemini JSON response to extract the first text part.
        /// </summary>
        private static string? ExtractResponseText(JsonDocument doc)
        {
            try
            {
                var root = doc.RootElement;
                var candidates = root.GetProperty("candidates").EnumerateArray();
                var firstCandidate = candidates.First();
                var content = firstCandidate.GetProperty("content");
                var parts = content.GetProperty("parts").EnumerateArray();
                var firstPart = parts.First();
                return firstPart.GetProperty("text").GetString();
            }
            catch (KeyNotFoundException ex)
            {
                throw new InvalidOperationException("Unexpected Gemini response structure.", ex);
            }
        }

        /// <summary>
        /// Parses the raw AI text into an <see cref="AiFillResult"/>.
        /// Strips markdown code fences if present before deserializing.
        /// </summary>
        private AiFillResult? ParseAiResponse(string text)
        {
            // Some models wrap the JSON in markdown fences — strip them.
            var trimmed = text.Trim();
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                var lastBacktick = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (lastBacktick > 0)
                {
                    trimmed = trimmed.Substring(3, lastBacktick - 3).Trim();
                }
            }

            return JsonSerializer.Deserialize<AiFillResult>(trimmed,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static string GetMimeType(string filePath)
        {
            var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                ".tiff" => "image/tiff",
                _ => "image/jpeg"
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
        }
    }
}
