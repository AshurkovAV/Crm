using Crm.Application.Interfaces;
using Crm.Entity.Services;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace Crm.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class UserDataController : Controller
    {
        private readonly ILogger<UserDataController> _logger;
        private ICrmRepository _crmRepository;
        private IUserContextService _userContextService;
        private IUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;

        public UserDataController(
            ILogger<UserDataController> logger,
            ICrmRepository crmRepository, 
            IUserContextService userContextService,
            IUserRepository userRepository,
            ICompanyRepository companyRepository)
        {
            _logger = logger;
            _crmRepository = crmRepository;
            _userContextService = userContextService;
            _userRepository = userRepository;
            _companyRepository = companyRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("GetUsers")]
        public object Get(DataSourceLoadOptions loadOptions)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Пользователь не авторизован" });
            }
            var users = _companyRepository.GetCompanyUsersByUserIdAsync(int.Parse(userId)).Result
           .Select(u => new
           {
               Id = u.UserId,
               FullName = u.DisplayName ?? u.Email ?? $"{u.FirstName} {u.LastName}",
               Email = u.Email
           })
           .ToList();

            return DataSourceLoader.Load(users, loadOptions);

        }       
    }
}
