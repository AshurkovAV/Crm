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
    public class ProjectDataController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private ICrmRepository _crmRepository;

        public ProjectDataController(
            ILogger<HomeController> logger,
            ICrmRepository crmRepository)
        {
            _logger = logger;
            _crmRepository = crmRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public object Get(DataSourceLoadOptions loadOptions)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null) {
                return DataSourceLoader.Load(_crmRepository.GetProjects(Convert.ToInt32(userId)), loadOptions);
            }

            return DataSourceLoader.Load("", loadOptions);
        }

        [HttpPost]
        public HttpResponseMessage Post(Wet form)
        {
            Console.WriteLine(@$"Вставить новую запись {DateTime.Now}");
            var key = Convert.ToInt32(form.key);
            var values = form.values;
            var resultData = _crmRepository.GetProject(key);            

            JsonConvert.PopulateObject(values, resultData.Data);
            var result = _crmRepository.InsertProject(resultData.Data);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //var resultUser = _crmRepository.InsertProjectUser(new Entity.ModelsCrm.ProjectUser { ProjectId = result.Id, UserId = userId });

            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.Created;

            return response;
        }

        [HttpPut]
        public HttpResponseMessage Put(Wet form)
        {
            Console.WriteLine(@$"Обновиь запись {DateTime.Now}");
            var key = Convert.ToInt32(form.key);
            var values = form.values;
            var resultData = _crmRepository.GetProject(key);

            JsonConvert.PopulateObject(values, resultData.Data);

            var result = _crmRepository.UpdataProject(resultData.Data);
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            return response;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var itemsToDelete = _crmRepository.DeleteProject(id);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }


    public class Wet
    {
        public string key { get; set; }
        public string values { get; set; }
    }
}
