using Crm.Application.Interfaces;
using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Security.Claims;

namespace Crm.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class UserDataController : Controller
    {
        private readonly ILogger<TaskDataController> _logger;
        private ICrmRepository _crmRepository;
        private IUserContextService _userContextService;
        private IUserRepository _userRepository;

        public UserDataController(
            ILogger<TaskDataController> logger,
            ICrmRepository crmRepository, 
            IUserContextService userContextService,
            IUserRepository userRepository)
        {
            _logger = logger;
            _crmRepository = crmRepository;
            _userContextService = userContextService;
            _userRepository = userRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("GetUsers")]
        public object Get(DataSourceLoadOptions loadOptions)
        {
            var users = _userRepository.GetUsers()
           .Select(u => new
           {
               Id = u.Id,
               FullName = u.DisplayName ?? u.DefaultEmail ?? $"{u.FirstName} {u.LastName}",
               Email = u.DefaultEmail
           })
           .ToList();

            return DataSourceLoader.Load(users, loadOptions);

        }       
    }
}
