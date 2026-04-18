using Crm.Application.Features.Accounts.Commands.CreateCompany;
using Crm.Entity.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Crm.Controllers.Company
{
    [Route("api/[controller]")]
    [Authorize]
    public class CompaniesController : ControllerBase
    {
        private readonly IMediator _mediator;        
        private readonly ICrmRepository _crmRepository;

        public CompaniesController(
            IMediator mediator,
            ICrmRepository crmRepository)
        {
            _mediator = mediator;
            _crmRepository = crmRepository; 
            
        }

        /// <summary>
        /// Создать новую компанию
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "Пользователь не авторизован" });

                var command = new CreateCompanyCommand
                {
                    CompanyName = request.Name,                    
                    OwnerId = int.Parse(userId)
                };

                var result = await _mediator.Send(command);

                if (result.Succeeded)
                {
                    return Ok(new
                    {
                        success = true,
                        companyId = result.CompanyId,
                        name = result.CompanyBase.Name,
                        message = $"Компания \"{result.CompanyBase.Name}\" успешно создана"
                    });
                }
                else
                {
                    return BadRequest(new { message = result.Errors.FirstOrDefault()?.ToString() ?? "Не удалось создать компанию" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Внутренняя ошибка: {ex.Message}" });
            }
        }

        /// <summary>
        /// Получить список компаний текущего пользователя
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyCompanies()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var companies = await _crmRepository.GetMyCompanies(int.Parse(userId));
                

            return Ok(new { data = companies });
        }
        
    }

    // Request модели
    public class CreateCompanyRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public class SwitchCompanyRequest
    {
        public int CompanyId { get; set; }
    }
}
