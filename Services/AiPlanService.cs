using System.Text;
using System.Text.Json;

namespace EduBoost.Services
{
    public class AiPlanService : IAiPlanService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public AiPlanService(HttpClient http, IConfiguration cfg)
        {
            _http = http; _cfg = cfg;
        }

        public async Task<string> GeneratePlanAsync(string studentName, string subject,
            double avgPercent, string learningStyle, double attendance)
        {
            var prompt = $@"You are an expert teacher. Create a 4-week remedial plan.
Student: {studentName}. Subject: {subject}. Average score: {avgPercent:F1}%.
Attendance: {attendance}%. Learning style: {learningStyle}.
Give week-by-week activities, 2 innovative teaching methods suited to the
learning style, a simple weekly test idea and tips for parents. Keep it concise.";

            var geminiKey = _cfg["Gemini:ApiKey"];
            if (!string.IsNullOrWhiteSpace(geminiKey))
            {
                try
                {
                    var model = _cfg["Gemini:Model"] ?? "gemini-3.5-flash-lite";
                    var geminiPayload = JsonSerializer.Serialize(new
                    {
                        contents = new[]
                        {
                            new { role = "user", parts = new[] { new { text = prompt } } }
                        }
                    });

                    var gUri = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={geminiKey}";
                    var gReq = new HttpRequestMessage(HttpMethod.Post, gUri)
                    {
                        Content = new StringContent(geminiPayload, Encoding.UTF8, "application/json")
                    };

                    var gRes = await _http.SendAsync(gReq);
                    if (gRes.IsSuccessStatusCode)
                    {
                        using var gDoc = JsonDocument.Parse(await gRes.Content.ReadAsStringAsync());
                        var root = gDoc.RootElement;
                        if (root.TryGetProperty("candidates", out var cands) && cands.GetArrayLength() > 0)
                        {
                            var parts = cands[0].GetProperty("content").GetProperty("parts");
                            var sb = new StringBuilder();
                            foreach (var p in parts.EnumerateArray())
                            {
                                if (p.TryGetProperty("text", out var t)) sb.Append(t.GetString());
                            }
                            if (sb.Length > 0) return sb.ToString();
                        }
                    }
                }
                catch
                {
                    // Fall through to Anthropic or fallback
                }
            }

            var key = _cfg["Ai:ApiKey"];
            if (string.IsNullOrWhiteSpace(key) || key.StartsWith("PUT"))
                return FallbackPlan(subject, learningStyle);   // works without internet/API

            var body = JsonSerializer.Serialize(new
            {
                model = _cfg["Ai:Model"],
                max_tokens = 800,
                messages = new[] { new { role = "user", content = prompt } }
            });

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            req.Headers.Add("x-api-key", key);
            req.Headers.Add("anthropic-version", "2023-06-01");

            try
            {
                var res = await _http.SendAsync(req);
                res.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                return doc.RootElement.GetProperty("content")[0]
                          .GetProperty("text").GetString() ?? FallbackPlan(subject, learningStyle);
            }
            catch (Exception)
            {
                return FallbackPlan(subject, learningStyle);   // exception handling (syllabus!)
            }
        }

        private static string FallbackPlan(string subject, string style) =>
            $"Week 1: Revise basics of {subject} with {style}-friendly material.\n" +
            "Week 2: Peer tutoring + daily 15-minute practice.\n" +
            "Week 3: Gamified quiz on weak topics.\n" +
            "Week 4: Re-test and review progress with parents.";
    }
}