using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Crm.Core.Features.Email.Models;
using Crm.Core.Features.Email.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Crm.Controllers.Email
{
    [Authorize]
    public class SendMailController : Controller
    {
        private readonly IMailService _mailService;
        private readonly ILogger<SendMailController> _logger;
        public SendMailController(IMailService mailService, ILogger<SendMailController> logger)
        {
            _mailService = mailService;
            _logger = logger;
        }

        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> SendEmail()
        {

            try
            {
                Mail mail;
                using (var reader = new StreamReader(Request.Body))
                {
                    var jsonString = await reader.ReadToEndAsync();
                    mail = JsonConvert.DeserializeObject<Mail>(jsonString);
                }

                if (mail == null)
                {
                    return BadRequest("Неверный формат данных");
                }

                var result = await _mailService.SendEmailAsync(mail);

                return Ok(new { Success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в контроллере SendMail");
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }
    }
}
