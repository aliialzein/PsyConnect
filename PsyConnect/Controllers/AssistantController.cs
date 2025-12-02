using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsyConnect.Services;

namespace PsyConnect.Controllers
{
    [Authorize]
    public class AssistantController : Controller
    {
        private readonly IChatbotService _chatbot;

        public AssistantController(IChatbotService chatbot)
        {
            _chatbot = chatbot;
        }

        [HttpGet]
        public IActionResult Chat()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromForm] string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return BadRequest("Message is required.");

            var reply = await _chatbot.GetReplyAsync(message);

            return Json(new { reply });
        }

        [HttpGet]
        public async Task<IActionResult> Test()
        {
            var reply = await _chatbot.GetReplyAsync("What is psychotherapy?");
            return Content(reply);
        }
    }
}