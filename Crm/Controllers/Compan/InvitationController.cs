using Crm.Models.Requests;
using Crm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Crm.Controllers.Compan
{
    [Authorize]
    [Route("[controller]")]
    public class InvitationController : Controller
    {
        private readonly IInvitationService _invitationService;

        public InvitationController(IInvitationService invitationService)
        {
            _invitationService = invitationService;
        }

        // GET: api/Invitation/GenerateLink
        [HttpGet("GenerateLink")]
        public async Task<IActionResult> GenerateInvitationLink([FromQuery] int? departmentId = null)
        {
            var result = await _invitationService.GenerateInvitationLinkAsync(departmentId);
            return Ok(result);
        }

        // POST: api/Invitation/SendEmail
        [HttpPost("SendEmail")]
        public async Task<IActionResult> SendEmailInvitations([FromBody] EmailInvitationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Пользователь не авторизован" });
            }
            var result = await _invitationService.SendEmailInvitationsAsync(request, int.Parse(userId));
            return Ok(result);
        }

        //// POST: api/Invitation/SendSms
        //[HttpPost("SendSms")]
        //public async Task<IActionResult> SendSmsInvitations([FromBody] SmsInvitationRequest request)
        //{
        //    var result = await _invitationService.SendSmsInvitationsAsync(request);
        //    return Ok(result);
        //}

        // GET: api/Invitation/Check/{code}
        // [HttpGet("Check/{code}")]
        //public async Task<IActionResult> CheckInvitation(string code)
        //{
        //    var result = await _invitationService.CheckInvitationAsync(code);
        //    return Ok(result);
        //}

        //// GET: api/Invitation/Stats
        //[HttpGet("Stats")]
        //public async Task<IActionResult> GetStats()
        //{
        //    var stats = await _invitationService.GetStatsAsync();
        //    return Ok(stats);
        //}
    }
}
