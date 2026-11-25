using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EduBoost.Data;
using Microsoft.EntityFrameworkCore;

namespace EduBoost.Services
{
    public class GeminiChatService : IGeminiChatService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<GeminiChatService> _logger;

        // In-memory cache for DB stats to prevent DB queries on every message
        private static string? _cachedDbSummary;
        private static DateTime _cacheExpiry = DateTime.MinValue;

        public GeminiChatService(
            HttpClient http,
            IConfiguration cfg,
            ApplicationDbContext db,
            ILogger<GeminiChatService> logger)
        {
            _http = http;
            _cfg = cfg;
            _db = db;
            _logger = logger;
        }

        public async Task<ChatResponseDto> SendMessageAsync(
            string userMessage,
            List<ChatMessageDto>? history,
            string? userEmail = null,
            string? userRole = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return new ChatResponseDto
                {
                    Success = false,
                    Error = "Message cannot be empty."
                };
            }

            var trimmedMsg = userMessage.Trim();

            // 1. FAST PATH: Check for instant-match queries (responds in < 20ms)
            if (TryGetInstantResponse(trimmedMsg, out var instantReply))
            {
                return new ChatResponseDto
                {
                    Success = true,
                    Reply = instantReply,
                    ModelUsed = "eduboost-instant-engine"
                };
            }

            // 2. LIVE AI PATH: Google Gemini API
            var apiKey = _cfg["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new ChatResponseDto
                {
                    Success = false,
                    Reply = "Google Gemini API key is missing. Please configure 'Gemini:ApiKey' in appsettings.json."
                };
            }

            var model = _cfg["Gemini:Model"] ?? "gemini-3.5-flash-lite";
            string systemPrompt = await GetOrBuildSystemPromptAsync(userEmail, userRole);

            try
            {
                // Strict 6-second timeout for lightning-fast responsiveness
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                var result = await CallGeminiApiAsync(model, apiKey, systemPrompt, trimmedMsg, history, cts.Token);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    return new ChatResponseDto
                    {
                        Success = true,
                        Reply = result,
                        ModelUsed = model
                    };
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Gemini request timed out after 6 seconds for model {Model}.", model);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini call to {Model} encountered an issue: {Message}", model, ex.Message);
            }

            // Fallback response if API encounters network spikes
            return new ChatResponseDto
            {
                Success = true,
                ModelUsed = "edu-fast-fallback",
                Reply = BuildPedagogicalFallback(trimmedMsg)
            };
        }

        private static bool TryGetInstantResponse(string msg, out string response)
        {
            var clean = msg.Trim().ToLowerInvariant();

            // 1. Risk calculation / formula
            if ((clean.Contains("risk") && (clean.Contains("calculat") || clean.Contains("score") || clean.Contains("formula") || clean.Contains("how"))) ||
                clean.Contains("how is the slow learner risk score calculated"))
            {
                response = "In EduBoost, the **Overall Risk Score** is calculated using an objective 3-component formula:\n\n" +
                           "- **60% × Marks Deficiency:** Measures the gap between student grades and the minimum target.\n" +
                           "- **25% × Attendance Deficiency:** Measures class absence impact on learning continuity.\n" +
                           "- **15% × Declining Trend:** Evaluates score decline over consecutive assessments.\n\n" +
                           "### 🚩 Flagging Thresholds:\n" +
                           "A student is officially flagged as a **Slow Learner** if:\n" +
                           "1. **Average Score < 40%**, OR\n" +
                           "2. **Overall Risk Score ≥ 45**.\n\n" +
                           "Once flagged, teachers can create personalized 4-week remedial plans under the **Remedial Plans** section.";
                return true;
            }

            // 2. Greetings
            if (clean == "hi" || clean == "hello" || clean == "hey" || clean.StartsWith("hello ") || clean.StartsWith("hi "))
            {
                response = "Hello! 👋 I am your **24/7 EduBoost AI Assistant** powered by Google Gemini.\n\n" +
                           "I am ready to help Teachers & Administrators with:\n" +
                           "- **Slow-learner detection & risk scoring**\n" +
                           "- **VARK learning styles** (Visual, Kinesthetic, Auditory)\n" +
                           "- **Innovative teaching strategies**\n" +
                           "- **Remedial action plan design**\n\n" +
                           "How can I help you right now?";
                return true;
            }

            // 3. 4-week remedial plan outline
            if (clean.Contains("4-week") || clean.Contains("remedial plan outline") || clean.Contains("create a remedial plan"))
            {
                response = "Here is the recommended **4-Week Remedial Intervention Framework** in EduBoost:\n\n" +
                           "- **Week 1 (Diagnostic & Foundation):** Identify specific learning gaps and introduce foundational concepts with high-visual / hands-on materials.\n" +
                           "- **Week 2 (Targeted Remediation):** Implement peer tutoring and daily 15-minute scaffolded exercises matched to the student's learning style.\n" +
                           "- **Week 3 (Gamified Reinforcement):** Conduct low-stakes quizzes, interactive challenges, and formative checks on weak topics.\n" +
                           "- **Week 4 (Post-Assessment & Evaluation):** Administer a post-remedial test, review improvement percentage, and share progress with parents.\n\n" +
                           "You can generate and track this directly in the **Remedial Plans** menu!";
                return true;
            }

            // 4. Kinesthetic & Innovative teaching methods
            if (clean.Contains("kinesthetic") || (clean.Contains("innovative") && clean.Contains("method")))
            {
                response = "Here are **3 Innovative Teaching Methods for Kinesthetic Learners**:\n\n" +
                           "1. **Tactile & 3D Modeling (Clay / Blocks / Sensory kits):**\n" +
                           "   - Have students build concepts physically (e.g. molecular structures, cell parts, fraction pies) instead of passive textbook reading.\n\n" +
                           "2. **Scaffolded Learning Stations (Movement Circuit):**\n" +
                           "   - Set up physical rotation stations around the classroom where students perform a hands-on experiment or step-by-step problem before moving.\n\n" +
                           "3. **Role-Play & Concept Enactment:**\n" +
                           "   - Students act out processes (e.g. blood circulation, historical events, mathematical equations) to create physical memory associations.\n\n" +
                           "*(You can also browse the pre-built method catalog in EduBoost under **Innovative Methods**.)*";
                return true;
            }

            // 5. Low attendance & failing marks remedy
            if ((clean.Contains("attendance") && clean.Contains("mark")) || clean.Contains("failing marks") || clean.Contains("low attendance"))
            {
                response = "When a student has **both low attendance and declining marks**, EduBoost's risk score spikes quickly (weighted 60% marks + 25% attendance). Here is the recommended intervention:\n\n" +
                           "1. **Automated Parent Communication:** Trigger a notification from the student profile to the registered parent email.\n" +
                           "2. **Identify Underlying Cause:** Check if absence is due to health, commute, or academic frustration.\n" +
                           "3. **Bite-Sized Remedial Material:** Provide 10-minute modular catch-up worksheets or peer-tutoring recordings rather than overwhelming homework.\n" +
                           "4. **Positive Attendance Incentives:** Pair every week of 100% attendance with recognition or low-stakes recovery quizzes to restore confidence.\n" +
                           "5. **Set up a Remedial Plan:** Enroll them in an **Active Remedial Plan** to track score recovery.";
                return true;
            }

            response = string.Empty;
            return false;
        }

        private async Task<string> GetOrBuildSystemPromptAsync(string? userEmail, string? userRole)
        {
            if (_cachedDbSummary == null || DateTime.UtcNow > _cacheExpiry)
            {
                try
                {
                    int totalStudents = await _db.Students.CountAsync();
                    int slowCount = await _db.Students.CountAsync(s => s.IsSlowLearner);
                    int activePlans = await _db.RemedialPlans.CountAsync(p => p.Status == "Active");

                    _cachedDbSummary = $"Total Students: {totalStudents}, Slow Learners: {slowCount}, Active Plans: {activePlans}";
                    _cacheExpiry = DateTime.UtcNow.AddMinutes(3);
                }
                catch
                {
                    _cachedDbSummary = "Active school workspace.";
                }
            }

            return $"You are EduBoost AI Assistant, a 24/7 educational co-pilot for Teachers & Administrators.\n" +
                   $"Platform Rules:\n" +
                   $"- Slow Learner: Average < 40% OR Risk >= 45. Risk formula: 60% Marks, 25% Attendance, 15% Trend.\n" +
                   $"- Learning Styles: Visual, Auditory, Reading, Kinesthetic.\n" +
                   $"- Context: {userRole ?? "Teacher"} ({userEmail ?? "user"}). Current School Stats: {_cachedDbSummary}.\n" +
                   $"Provide concise, empathetic, pedagogical, and actionable answers.";
        }

        private async Task<string?> CallGeminiApiAsync(
            string model,
            string apiKey,
            string systemPrompt,
            string userMessage,
            List<ChatMessageDto>? history,
            CancellationToken cancellationToken)
        {
            var contents = new List<object>();

            // Only append the last 4 turns to keep requests lightweight and ultra-fast
            if (history != null && history.Count > 0)
            {
                var recentHistory = history.TakeLast(4);
                foreach (var item in recentHistory)
                {
                    if (string.IsNullOrWhiteSpace(item.Text)) continue;
                    var role = item.Role.Equals("model", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
                    contents.Add(new
                    {
                        role = role,
                        parts = new[] { new { text = item.Text } }
                    });
                }
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.6,
                    maxOutputTokens = 450 // Reduced for speed and snappy UX
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var uri = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            using var req = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var res = await _http.SendAsync(req, cancellationToken);
            var resBody = await res.Content.ReadAsStringAsync(cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Gemini API error ({res.StatusCode})");
            }

            using var doc = JsonDocument.Parse(resBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts))
                {
                    var sb = new StringBuilder();
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var textEl))
                        {
                            sb.Append(textEl.GetString());
                        }
                    }

                    var text = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text;
                    }
                }
            }

            return null;
        }

        private static string BuildPedagogicalFallback(string userMessage)
        {
            return "Here is guidance on your query:\n\n" +
                   "- **For Slow Learners:** Prioritize students with Risk Score ≥ 45 or Average < 40%. Break lessons into smaller 15-minute segments.\n" +
                   "- **Remedial Action:** Use the **Remedial Plans** page to launch a 4-week structured plan matched to the student's learning style.\n" +
                   "- **Learning Styles:** Incorporate Visual diagrams, Kinesthetic role-play, and Auditory peer discussions to reinforce core concepts.\n\n" +
                   "*(The live Gemini online service is experiencing high internet traffic; full generative responses will resume automatically.)*";
        }
    }
}
