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
    public class ProjectDataController : Controller
    {
        private readonly ILogger<ProjectDataController> _logger;
        private ICrmRepository _crmRepository;
        private IUserContextService _userContextService;

        public ProjectDataController(
            ILogger<ProjectDataController> logger,
            ICrmRepository crmRepository, 
            IUserContextService userContextService)
        {
            _logger = logger;
            _crmRepository = crmRepository;
            _userContextService = userContextService;  
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public object Get(DataSourceLoadOptions loadOptions)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (userId != null)
            {
              
                var projects = _crmRepository.GetProjectsWithParticipantsDTO(Convert.ToInt32(userId));
                return DataSourceLoader.Load(projects, loadOptions);
            }
            return DataSourceLoader.Load(new List<object>(), loadOptions);

        }

        [HttpPost]
        public HttpResponseMessage Post(Wet form)
        {
            Console.WriteLine(@$"Вставить новую запись {DateTime.Now}");
            var key = Convert.ToInt32(form.key);
            var values = form.values;
            var resultData = _crmRepository.GetProject(key);

            // Простое решение: преобразуем массив Participants в строку
            // Проверяем, содержит ли values массив Participants
            if (values.Contains("\"Participants\":["))
            {
                // Заменяем массив на строку с числами через запятую
                values = System.Text.RegularExpressions.Regex.Replace(
                    values,
                    "\"Participants\":\\[(.*?)\\]",
                    match =>
                    {
                        var numbers = match.Groups[1].Value;
                        // Убираем пробелы и преобразуем в строку с запятыми
                        var numberString = string.Join(",",
                            numbers.Split(',')
                                   .Select(n => n.Trim()));
                        return $"\"Participants\":\"{numberString}\"";
                    }
                );
            }

            JsonConvert.PopulateObject(values, resultData.Data);
            var result = _crmRepository.InsertProject(resultData.Data);
            var userId = _userContextService.GetCurrentUserId();

            var resultUser = _crmRepository.InsertProjectUser(new Entity.ModelsCrm.ProjectUser
            {
                ProjectId = result.Id,
                Role = "ProjectOwner", //Владелец проекта(создатель, полные права)
                UserId = userId
            });

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
            var project = _crmRepository.GetProject(key);

            // Десериализуем значения
            var updateData = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);

            // Обрабатываем Participants отдельно
            if (updateData.ContainsKey("Participants"))
            {
                var participantsIds = JsonConvert.DeserializeObject<List<int>>(
                    updateData["Participants"].ToString()
                );

                // Синхронизируем участников
                SyncProjectParticipants(key, participantsIds);

                // Удаляем Participants из обновляемых данных
                updateData.Remove("Participants");
                values = JsonConvert.SerializeObject(updateData);
            }

            // Обновляем остальные поля
            JsonConvert.PopulateObject(values, project.Data);

            var result = _crmRepository.UpdataProject(project.Data);
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


        private void SyncProjectParticipants(int projectId, List<int> selectedUserIds)
        {
            try
            {
                // 1. Получаем текущих активных участников проекта
                var currentParticipants = _crmRepository.GetProjectUsers(projectId)
                    .Where(pu => pu.IsActive)
                    .ToList();

                var currentUserIds = currentParticipants.Select(pu => pu.UserId).ToList();

                // 2. Определяем, кого нужно добавить
                var usersToAdd = selectedUserIds.Except(currentUserIds).ToList();

                // 3. Определяем, кого нужно деактивировать (удалить)
                var usersToDeactivate = currentUserIds.Except(selectedUserIds).ToList();

                // 4. Добавляем новых участников
                foreach (var userId in usersToAdd)
                {
                    var projectUser = new ProjectUser
                    {
                        ProjectId = projectId,
                        UserId = userId,
                        Role = "Participant", // или другая роль по умолчанию
                        JoinedDate = DateTime.Now,
                        IsActive = true
                    };

                    _crmRepository.AddProjectUser(projectUser);
                }

                // 5. Деактивируем удаленных участников
                foreach (var userId in usersToDeactivate)
                {
                    var projectUser = currentParticipants.FirstOrDefault(pu => pu.UserId == userId);
                    if (projectUser != null)
                    {
                        projectUser.IsActive = false;                        
                        _crmRepository.UpdateProjectUser(projectUser);
                    }
                }

                // 6. Активируем ранее удаленных, если их снова добавили
                var previouslyDeactivated = _crmRepository.GetProjectUsers(projectId)
                    .Where(pu => !pu.IsActive && selectedUserIds.Contains(pu.UserId))
                    .ToList();

                foreach (var projectUser in previouslyDeactivated)
                {
                    projectUser.IsActive = true;
                    projectUser.JoinedDate = DateTime.Now;
                    _crmRepository.UpdateProjectUser(projectUser);
                }

                Console.WriteLine($"Синхронизировано участников проекта {projectId}: " +
                                 $"добавлено {usersToAdd.Count}, " +
                                 $"деактивировано {usersToDeactivate.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка синхронизации участников: {ex.Message}");
                throw;
            }
        }
    }


    public class Wet
    {
        public string key { get; set; }
        public string values { get; set; }
    }





}
