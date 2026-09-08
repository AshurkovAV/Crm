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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Crm.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class TaskDataController : Controller
    {
        private readonly ILogger<TaskDataController> _logger;
        private ICrmRepository _crmRepository;
        private IUserContextService _userContextService;
        private IUserRepository _userRepository;

        public TaskDataController(
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
        public object Get(DataSourceLoadOptions loadOptions)
        {
            var userId = _userContextService.GetCurrentUserId();
            if (userId != null)
            {              
                var tasks = _crmRepository.GetTasksWithAccess(Convert.ToInt32(userId));
                // Проецируем данные с преобразованием Tags в массив
                var tasksWithTagsArray = tasks.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Activity,
                    t.Deadline,
                    t.Assignee,
                    t.Author,
                    t.ProjectId,
                    Tags = string.IsNullOrEmpty(t.Tags)
                        ? new string[0]
                        : t.Tags.Split(',').Select(tag => tag.Trim()).ToArray(),
                    t.Status,
                    t.Priority,
                    t.IsOverdue,
                    t.Description,
                    t.Comments,
                    t.CreatedDate,
                    t.ModifiedDate
                });
                return DataSourceLoader.Load(tasksWithTagsArray, loadOptions);
            }
            return DataSourceLoader.Load(new List<object>(), loadOptions);

        }

        [HttpPost]
        public HttpResponseMessage Post(Wet form)
        {
            Console.WriteLine(@$"Вставить новую задачу {DateTime.Now}");

            var key = Convert.ToInt32(form.key);
            var values = form.values;

            // ПРЕОБРАЗОВАНИЕ ТЕГОВ: из массива JSON в строку через запятую
            values = ConvertTagsJsonToString(values);           

            var resultData = _crmRepository.GetTaskCrm(key);

            // Заполняем данные задачи
            JsonConvert.PopulateObject(values, resultData.Data);

            // Устанавливаем служебные поля
            resultData.Data.Activity = DateTime.Now;
            resultData.Data.CreatedDate = DateTime.Now;
            resultData.Data.ModifiedDate = DateTime.Now;            

            if (string.IsNullOrEmpty(resultData.Data.Priority))
                resultData.Data.Priority = "Средний";

            // Устанавливаем автора
            var userId = _userContextService.GetCurrentUserId();
            resultData.Data.Author = userId;

            // Проверка просрочки
            if (resultData.Data.Deadline.HasValue && resultData.Data.Deadline < DateTime.Now)
            {
                resultData.Data.IsOverdue = true;
                if (resultData.Data.Status != 4 && 
                    resultData.Data.Status != 5)
                    resultData.Data.Status = 6;
            }
            else
            {
                resultData.Data.IsOverdue = false;
            }

            // Вставляем в базу
            var result = _crmRepository.InsertTaskCrm(resultData.Data);

            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.Created;

            return response;
        }


        /// <summary>
        /// Преобразует JSON массив тегов в строку через запятую
        /// Пример: {"Tags":["Срочно","Важно"]} -> {"Tags":"Срочно,Важно"}
        /// </summary>
        private string ConvertTagsJsonToString(string json)
        {
            // Ищем поле Tags с массивом
            var pattern = @"""Tags"":\s*\[(.*?)\]";

            return System.Text.RegularExpressions.Regex.Replace(json, pattern, match =>
            {
                var tagsContent = match.Groups[1].Value;

                if (string.IsNullOrWhiteSpace(tagsContent))
                    return @"""Tags"":null";

                // Извлекаем значения тегов из массива
                var tags = System.Text.RegularExpressions.Regex.Matches(tagsContent, @"""([^""]*)""")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                if (!tags.Any())
                    return @"""Tags"":null";

                // Соединяем через запятую
                var tagsString = string.Join(",", tags);

                return $@"""Tags"":""{tagsString}""";
            });
        }

        [HttpPut]
        public HttpResponseMessage Put(int key, Wet form)
        {
            Console.WriteLine(@$"Обновиь запись {DateTime.Now}");          
            var values = form.values;
            var task = _crmRepository.GetTaskCrm(key);

            // Десериализуем значения
            var updateData = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);

            if (updateData.ContainsKey("Tags"))
            {
                // Проверяем тип: может быть массивом строк или массивом int
                var tagsObject = updateData["Tags"];

                List<string> tagsList = new List<string>();

                // Если это JArray (массив)
                if (tagsObject is Newtonsoft.Json.Linq.JArray jArray)
                {
                    tagsList = jArray.Select(t => t.ToString()).ToList();
                }
                // Если это уже List<string>
                else if (tagsObject is List<string> stringList)
                {
                    tagsList = stringList;
                }
                // Если это string[]
                else if (tagsObject is string[] stringArray)
                {
                    tagsList = stringArray.ToList();
                }

                // Преобразуем массив тегов в строку через запятую
                var tagsString = string.Join(",", tagsList.Select(t => t.Trim()));

                // Обновляем Tags в задаче
                task.Data.Tags = tagsString;

                // Удаляем Tags из обновляемых данных, чтобы не обрабатывать повторно
                updateData.Remove("Tags");
                values = JsonConvert.SerializeObject(updateData);
            }
            task.Data.Activity = DateTime.Now;
            task.Data.ModifiedDate = DateTime.Now;
            // Обновляем остальные поля
            JsonConvert.PopulateObject(values, task.Data);

            var result = _crmRepository.UpdataTaskCrm(task.Data);
            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.OK;

            return response;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();
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
        private int? GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
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
}
