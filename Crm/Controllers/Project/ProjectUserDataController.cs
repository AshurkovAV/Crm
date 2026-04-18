using Crm.Application.Interfaces;
using Crm.Entity.Services;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;

namespace Crm.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ProjectUserDataController : Controller
    {
        private readonly ILogger<ProjectUserDataController> _logger;
        private ICrmRepository _crmRepository;
        private IUserContextService _userContextService;
        private readonly ICompanyRepository _companyRepository;

        public ProjectUserDataController(
            ILogger<ProjectUserDataController> logger,
            ICrmRepository crmRepository,
            ICompanyRepository companyRepository,
        IUserContextService userContextService)
        {
            _logger = logger;
            _crmRepository = crmRepository;
            _userContextService = userContextService;  
            _companyRepository = companyRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public object GetParticipants(DataSourceLoadOptions loadOptions)
        {
            var userId = _userContextService.GetCurrentUserId();           
            
            
            if (userId != null)
            {
                // Добавить поле DisplayName для отображения
                var users = _companyRepository.GetCompanyUsersByUserIdAsync(userId).Result
                    .Select(u => new {
                        Id = u.UserId,
                        DisplayName = (u.DisplayName?.Trim() ?? u.Email?.Trim()) ?? string.Empty,
                        FirstName = u.FirstName,
                        LastName = u.LastName,                        
                        Email = u.Email
                    });
                return DataSourceLoader.Load(users, loadOptions);
            }
            return DataSourceLoader.Load(new List<object>(), loadOptions);
        }
    }
  
}
