using Crm.Models;
using Crm.Entity.Services;
using Crm.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Crm.Controllers.Online
{
    [Authorize]
    public class OnlineController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICompanyRepository _companyRepository;
        private readonly IChatRepository _chatRepository;
        private readonly ChatTypingStore _typingStore;

        public OnlineController(
            ILogger<HomeController> logger,
            ICompanyRepository companyRepository,
            IChatRepository chatRepository,
            ChatTypingStore typingStore)
        {
            _logger = logger;
            _companyRepository = companyRepository;
            _chatRepository = chatRepository;
            _typingStore = typingStore;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Contacts()
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            var contacts = await _companyRepository.GetCompanyUsersByUserIdAsync(userId.Value);
            return Json(contacts.Select(contact => new
            {
                id = contact.UserId,
                name = contact.FullName,
                role = contact.DisplayRole,
                avatarUrl = contact.AvatarUrl,
                isCurrentUser = contact.IsCurrentUser
            }));
        }

        [HttpGet]
        public async Task<IActionResult> Messages(int userId, int take = 100)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || userId <= 0 || userId == currentUserId.Value)
                return BadRequest();

            var messages = await _chatRepository.GetConversationAsync(currentUserId.Value, userId, take);
            return Json(messages.Select(message => new
            {
                id = message.Id,
                text = message.Text,
                sentAt = message.SentAt,
                incoming = message.SenderUserId != currentUserId.Value
            }));
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || request.RecipientUserId <= 0)
                return Unauthorized();

            var text = request.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text) || text.Length > 4000)
                return BadRequest(new { message = "Сообщение должно содержать от 1 до 4000 символов." });

            var message = await _chatRepository.SendMessageAsync(
                currentUserId.Value,
                request.RecipientUserId,
                text);

            if (message == null)
                return BadRequest(new { message = "Пользователь не входит в вашу текущую компанию." });

            return Json(new
            {
                id = message.Id,
                text = message.Text,
                sentAt = message.SentAt,
                incoming = false
            });
        }

        [HttpPost]
        public async Task<IActionResult> Typing([FromBody] TypingRequest request)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || !await IsCompanyContactAsync(currentUserId.Value, request.RecipientUserId))
                return BadRequest();

            _typingStore.SetTyping(currentUserId.Value, request.RecipientUserId, request.IsTyping);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> TypingStatus(int userId)
        {
            var currentUserId = GetUserId();
            if (!currentUserId.HasValue || !await IsCompanyContactAsync(currentUserId.Value, userId))
                return BadRequest();

            return Json(new
            {
                isTyping = _typingStore.IsTyping(userId, currentUserId.Value)
            });
        }

        private async Task<bool> IsCompanyContactAsync(int currentUserId, int contactUserId)
        {
            if (contactUserId <= 0 || contactUserId == currentUserId)
                return false;

            var contacts = await _companyRepository.GetCompanyUsersByUserIdAsync(currentUserId);
            return contacts.Any(contact => contact.UserId == contactUserId && contact.IsActive);
        }

        private int? GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }

        public sealed class SendMessageRequest
        {
            public int RecipientUserId { get; set; }
            public string? Text { get; set; }
        }

        public sealed class TypingRequest
        {
            public int RecipientUserId { get; set; }
            public bool IsTyping { get; set; }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
