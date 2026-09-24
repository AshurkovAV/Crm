using Crm.Application.Interfaces;
using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Services.Notifications;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Ganss.Xss;
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
        private static readonly string[] AllowedDescriptionImageContentTypes =
        {
            "image/png", "image/jpeg", "image/webp", "image/gif"
        };

        private const long MaxDescriptionImageSizeBytes = 10 * 1024 * 1024; // 10 МБ

        private static readonly HtmlSanitizer DescriptionSanitizer = CreateDescriptionSanitizer();

        private readonly ILogger<TaskDataController> _logger;
        private ICrmRepository _crmRepository;
        private IUserContextService _userContextService;
        private IUserRepository _userRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ITaskNotificationDispatcher _taskNotificationDispatcher;

        public TaskDataController(
            ILogger<TaskDataController> logger,
            ICrmRepository crmRepository,
            IUserContextService userContextService,
            IUserRepository userRepository,
            IWebHostEnvironment webHostEnvironment,
            ITaskNotificationDispatcher taskNotificationDispatcher)
        {
            _logger = logger;
            _crmRepository = crmRepository;
            _userContextService = userContextService;
            _userRepository = userRepository;
            _webHostEnvironment = webHostEnvironment;
            _taskNotificationDispatcher = taskNotificationDispatcher;
        }

        /// <summary>
        /// POST /TaskData/upload-image — загрузка картинки, вставляемой в описание задачи
        /// (HtmlEditor: paste/drop/кнопка "Изображение"). Не привязана к Id задачи, так как
        /// у новой задачи ещё нет Id на момент вставки картинки в редактор.
        /// </summary>
        [HttpPost("upload-image")]
        [RequestSizeLimit(MaxDescriptionImageSizeBytes)]
        public async Task<IActionResult> UploadDescriptionImage(IFormFile? file)
        {
            var userId = GetUserId();
            if (!userId.HasValue)
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл не передан." });

            if (file.Length > MaxDescriptionImageSizeBytes)
                return BadRequest(new { message = "Файл слишком большой (максимум 10 МБ)." });

            var contentType = file.ContentType?.ToLowerInvariant();
            if (contentType == null || !AllowedDescriptionImageContentTypes.Contains(contentType))
                return BadRequest(new { message = "Допускаются только изображения (PNG, JPG, WEBP, GIF)." });

            var extension = contentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg"
            };

            var folderAbsolute = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tasks");
            Directory.CreateDirectory(folderAbsolute);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(folderAbsolute, fileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            return Json(new { url = $"/uploads/tasks/{fileName}" });
        }

        /// <summary>
        /// Санитайзер описания задачи: разрешает базовые теги форматирования из панели
        /// HtmlEditor (жирный/курсив/подчёркнутый/списки/ссылка/изображение) и запрещает
        /// картинки с src вне папки /uploads/tasks/ (чтобы через описание нельзя было
        /// затащить произвольный внешний контент или сослаться на чужой файл на сервере).
        /// </summary>
        private static HtmlSanitizer CreateDescriptionSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedTags.Clear();
            foreach (var tag in new[] { "p", "br", "b", "strong", "i", "em", "u", "s", "ul", "ol", "li", "a", "img", "blockquote", "h1", "h2", "h3" })
                sanitizer.AllowedTags.Add(tag);

            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.Add("href");
            sanitizer.AllowedAttributes.Add("src");
            sanitizer.AllowedAttributes.Add("alt");
            sanitizer.AllowedAttributes.Add("target");
            sanitizer.AllowedAttributes.Add("rel");

            sanitizer.AllowedCssProperties.Clear();

            sanitizer.FilterUrl += (sender, args) =>
            {
                if (string.Equals(args.Tag.TagName, "img", StringComparison.OrdinalIgnoreCase)
                    && !args.OriginalUrl.StartsWith("/uploads/tasks/", StringComparison.OrdinalIgnoreCase))
                {
                    args.SanitizedUrl = null;
                }
            };

            return sanitizer;
        }

        private static string? SanitizeDescription(string? html)
            => string.IsNullOrWhiteSpace(html) ? html : DescriptionSanitizer.Sanitize(html);

        /// <summary>
        /// Пустое описание с точки зрения бизнес-правила "обязательно" — учитывает
        /// HtmlEditor, который для пустого поля всё равно отдаёт "&lt;p&gt;&lt;br&gt;&lt;/p&gt;".
        /// </summary>
        private static bool IsDescriptionEmpty(string? html)
            => string.IsNullOrWhiteSpace(Regex.Replace(html ?? string.Empty, "<[^>]*>", string.Empty).Trim());

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
                // ���������� ������ � ��������������� Tags � ������
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
        public async Task<HttpResponseMessage> Post(Wet form)
        {
            Console.WriteLine(@$"�������� ����� ������ {DateTime.Now}");

            var key = Convert.ToInt32(form.key);
            var values = form.values;

            // �������������� �����: �� ������� JSON � ������ ����� �������
            values = ConvertTagsJsonToString(values);           

            var resultData = _crmRepository.GetTaskCrm(key);

            // ��������� ������ ������
            JsonConvert.PopulateObject(values, resultData.Data);

            resultData.Data.Description = SanitizeDescription(resultData.Data.Description);
            if (IsDescriptionEmpty(resultData.Data.Description))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(
                        JsonConvert.SerializeObject(new { message = "Описание задачи обязательно." }),
                        System.Text.Encoding.UTF8, "application/json")
                };
            }

            // ������������� ��������� ����
            resultData.Data.Activity = DateTime.Now;
            resultData.Data.CreatedDate = DateTime.Now;
            resultData.Data.ModifiedDate = DateTime.Now;            

            if (string.IsNullOrEmpty(resultData.Data.Priority))
                resultData.Data.Priority = "�������";

            // ������������� ������
            var userId = _userContextService.GetCurrentUserId();
            resultData.Data.Author = userId;

            // �������� ���������
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

            // ��������� � ����
            var result = _crmRepository.InsertTaskCrm(resultData.Data);

            await NotifyAssigneeAsync(resultData.Data, TaskNotificationKind.Created, new List<string>(), userId);

            HttpResponseMessage response = new HttpResponseMessage();
            response.StatusCode = HttpStatusCode.Created;

            return response;
        }


        /// <summary>
        /// ����������� JSON ������ ����� � ������ ����� �������
        /// ������: {"Tags":["������","�����"]} -> {"Tags":"������,�����"}
        /// </summary>
        private string ConvertTagsJsonToString(string json)
        {
            // ���� ���� Tags � ��������
            var pattern = @"""Tags"":\s*\[(.*?)\]";

            return System.Text.RegularExpressions.Regex.Replace(json, pattern, match =>
            {
                var tagsContent = match.Groups[1].Value;

                if (string.IsNullOrWhiteSpace(tagsContent))
                    return @"""Tags"":null";

                // ��������� �������� ����� �� �������
                var tags = System.Text.RegularExpressions.Regex.Matches(tagsContent, @"""([^""]*)""")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                if (!tags.Any())
                    return @"""Tags"":null";

                // ��������� ����� �������
                var tagsString = string.Join(",", tags);

                return $@"""Tags"":""{tagsString}""";
            });
        }

        [HttpPut]
        public async Task<HttpResponseMessage> Put(int key, Wet form)
        {
            Console.WriteLine(@$"������� ������ {DateTime.Now}");
            var values = form.values;
            var task = _crmRepository.GetTaskCrm(key);

            // Снимок "до" — чтобы после сохранения понять, что реально изменилось, и стоит ли
            // об этом писать исполнителю (например, простой перенос карточки в канбане меняет
            // только Status — на это отдельное письмо не шлём, чтобы не спамить).
            var oldName = task.Data.Name;
            var oldDeadline = task.Data.Deadline;
            var oldPriority = task.Data.Priority;
            var oldDescription = task.Data.Description;
            var oldAssignee = task.Data.Assignee;

            // ������������� ��������
            var updateData = JsonConvert.DeserializeObject<Dictionary<string, object>>(values);

            if (updateData.ContainsKey("Tags"))
            {
                // ��������� ���: ����� ���� �������� ����� ��� �������� int
                var tagsObject = updateData["Tags"];

                List<string> tagsList = new List<string>();

                // ���� ��� JArray (������)
                if (tagsObject is Newtonsoft.Json.Linq.JArray jArray)
                {
                    tagsList = jArray.Select(t => t.ToString()).ToList();
                }
                // ���� ��� ��� List<string>
                else if (tagsObject is List<string> stringList)
                {
                    tagsList = stringList;
                }
                // ���� ��� string[]
                else if (tagsObject is string[] stringArray)
                {
                    tagsList = stringArray.ToList();
                }

                // ����������� ������ ����� � ������ ����� �������
                var tagsString = string.Join(",", tagsList.Select(t => t.Trim()));

                // ��������� Tags � ������
                task.Data.Tags = tagsString;

                // ������� Tags �� ����������� ������, ����� �� ������������ ��������
                updateData.Remove("Tags");
                values = JsonConvert.SerializeObject(updateData);
            }
            task.Data.Activity = DateTime.Now;
            task.Data.ModifiedDate = DateTime.Now;
            // ��������� ��������� ����
            var descriptionTouched = updateData.ContainsKey("Description");
            JsonConvert.PopulateObject(values, task.Data);

            if (descriptionTouched)
            {
                task.Data.Description = SanitizeDescription(task.Data.Description);
                if (IsDescriptionEmpty(task.Data.Description))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent(
                            JsonConvert.SerializeObject(new { message = "Описание задачи обязательно." }),
                            System.Text.Encoding.UTF8, "application/json")
                    };
                }
            }

            var result = _crmRepository.UpdataTaskCrm(task.Data);

            var reassigned = oldAssignee != task.Data.Assignee;
            var changedFields = new List<string>();
            if (!reassigned)
            {
                if (oldName != task.Data.Name) changedFields.Add("Название");
                if (oldDeadline != task.Data.Deadline) changedFields.Add("Срок");
                if (oldPriority != task.Data.Priority) changedFields.Add("Приоритет");
                if (oldDescription != task.Data.Description) changedFields.Add("Описание");
            }

            // Если сменили исполнителя — для НОВОГО исполнителя это по сути новая задача,
            // даже если строка в базе технически обновилась, а не создалась заново.
            if (reassigned || changedFields.Count > 0)
            {
                var kind = reassigned ? TaskNotificationKind.Created : TaskNotificationKind.Updated;
                await NotifyAssigneeAsync(task.Data, kind, changedFields, GetUserId() ?? 0);
            }

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

        /// <summary>
        /// Уведомляет исполнителя задачи (сейчас — по email, канал регистрируется в DI,
        /// см. Crm/Services/Notifications). Не бросает исключения наружу: сбой уведомления
        /// не должен ломать сохранение задачи.
        /// </summary>
        private async Task NotifyAssigneeAsync(TaskCrm taskData, TaskNotificationKind kind, List<string> changedFields, int currentUserId)
        {
            try
            {
                if (!taskData.Assignee.HasValue)
                    return;

                // Не шлём человеку письмо о его же собственном действии (сам себе поставил/поменял задачу).
                if (taskData.Assignee.Value == currentUserId)
                    return;

                var assignee = _userRepository.GetUserById(taskData.Assignee.Value)?.Data;
                if (assignee == null || string.IsNullOrWhiteSpace(assignee.DefaultEmail))
                    return;

                string? authorName = null;
                if (taskData.Author.HasValue)
                    authorName = _userRepository.GetUserById(taskData.Author.Value)?.Data?.DisplayName;

                string? projectName = null;
                if (taskData.ProjectId.HasValue)
                    projectName = _crmRepository.GetProject(taskData.ProjectId.Value)?.Data?.Name;

                var context = new TaskNotificationContext
                {
                    Kind = kind,
                    TaskId = taskData.Id,
                    TaskName = taskData.Name,
                    Description = taskData.Description,
                    Deadline = taskData.Deadline,
                    Priority = taskData.Priority,
                    ProjectName = projectName,
                    AuthorName = authorName,
                    AssigneeUserId = assignee.Id,
                    AssigneeName = assignee.DisplayName ?? assignee.DefaultEmail,
                    AssigneeEmail = assignee.DefaultEmail,
                    ChangedFields = changedFields
                };

                await _taskNotificationDispatcher.DispatchAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось отправить уведомление по задаче {TaskId}", taskData.Id);
            }
        }

        private void SyncProjectParticipants(int projectId, List<int> selectedUserIds)
        {
            try
            {
                // 1. �������� ������� �������� ���������� �������
                var currentParticipants = _crmRepository.GetProjectUsers(projectId)
                    .Where(pu => pu.IsActive)
                    .ToList();

                var currentUserIds = currentParticipants.Select(pu => pu.UserId).ToList();

                // 2. ����������, ���� ����� ��������
                var usersToAdd = selectedUserIds.Except(currentUserIds).ToList();

                // 3. ����������, ���� ����� �������������� (�������)
                var usersToDeactivate = currentUserIds.Except(selectedUserIds).ToList();

                // 4. ��������� ����� ����������
                foreach (var userId in usersToAdd)
                {
                    var projectUser = new ProjectUser
                    {
                        ProjectId = projectId,
                        UserId = userId,
                        Role = "Participant", // ��� ������ ���� �� ���������
                        JoinedDate = DateTime.Now,
                        IsActive = true
                    };

                    _crmRepository.AddProjectUser(projectUser);
                }

                // 5. ������������ ��������� ����������
                foreach (var userId in usersToDeactivate)
                {
                    var projectUser = currentParticipants.FirstOrDefault(pu => pu.UserId == userId);
                    if (projectUser != null)
                    {
                        projectUser.IsActive = false;                        
                        _crmRepository.UpdateProjectUser(projectUser);
                    }
                }

                // 6. ���������� ����� ���������, ���� �� ����� ��������
                var previouslyDeactivated = _crmRepository.GetProjectUsers(projectId)
                    .Where(pu => !pu.IsActive && selectedUserIds.Contains(pu.UserId))
                    .ToList();

                foreach (var projectUser in previouslyDeactivated)
                {
                    projectUser.IsActive = true;
                    projectUser.JoinedDate = DateTime.Now;
                    _crmRepository.UpdateProjectUser(projectUser);
                }

                Console.WriteLine($"���������������� ���������� ������� {projectId}: " +
                                 $"��������� {usersToAdd.Count}, " +
                                 $"�������������� {usersToDeactivate.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ������������� ����������: {ex.Message}");
                throw;
            }
        }
    }
}
