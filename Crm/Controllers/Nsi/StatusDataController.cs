using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers.Nsi
{
    [Authorize]
    public class StatusDataController : Controller
    {
        private readonly INsiRepository _nsiRepository; // замените на ваш DbContext

        public StatusDataController(INsiRepository nsiRepository)
        {
            _nsiRepository = nsiRepository;             
        }

        [HttpGet]
        public object Get(DataSourceLoadOptions loadOptions)
        {
            var statuses = _nsiRepository.GetRefStatuses()
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
            .ToList();

            return DataSourceLoader.Load(statuses, loadOptions);
        }
    }
}
