using System.Security.Claims;
using EduBoost.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduBoost.Controllers.Api
{
    [ApiController]
    [Route("api/chatbot")]
    [Produces("application/json")]
    public class ChatbotController : ControllerBase
    {
        private readonly IGeminiChatService _chatService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IGeminiChatService chatService,
            ILogger<ChatbotController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Sends a message to the 24/7 EduBoost AI Assistant powered by Google Gemini.
        /// </summary>
        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ChatResponseDto
                {
                    Success = false,
                    Error = "Message cannot be empty."
                });
            }

            var userEmail = User.Identity?.IsAuthenticated == true
                ? User.Identity.Name
                : "EduBoost Staff";

            var userRole = User.IsInRole("Admin")
                ? "Admin"
                : (User.IsInRole("Teacher") ? "Teacher" : "Staff");

            try
            {
                var response = await _chatService.SendMessageAsync(
                    request.Message,
                    request.History,
                    userEmail,
                    userRole);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message.");
                return StatusCode(500, new ChatResponseDto
                {
                    Success = false,
                    Error = "An unexpected error occurred while communicating with the AI service.",
                    Reply = "Sorry, I encountered a temporary issue. Please try again shortly."
                });
            }
        }

        /// <summary>
        /// Health check and metadata endpoint for the AI chatbot.
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var userRole = User.IsInRole("Admin")
                ? "Admin"
                : (User.IsInRole("Teacher") ? "Teacher" : "Guest");

            return Ok(new
            {
                online = true,
                service = "EduBoost 24/7 AI Assistant",
                provider = "Google Gemini API",
                user = User.Identity?.Name ?? "Anonymous",
                role = userRole
            });
        }
    }
}
