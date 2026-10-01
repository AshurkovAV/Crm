using Crm.Entity.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Crm.Services;
using System.Security.Claims;
using Crm.Models.Profil;


namespace Crm.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly UserService _userService;
        private readonly AvatarStorage _avatarStorage;

        // Вариант 1: Внедрение через конструктор (рекомендуемый)
        public ProfileController(
            IUserRepository userRepository,
            ICompanyRepository companyRepository,
            UserService userService,
            AvatarStorage avatarStorage)
        {
            _userRepository = userRepository;
            _companyRepository = companyRepository;
            _userService = userService;
            _avatarStorage = avatarStorage;
        }

        // GET: Profile/Index
        public ActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });
            var profile = _userService.GetUserProfile(Convert.ToInt32(userId));

            if (profile == null)
            {
                TempData["Error"] = "Пользователь не найден";
                return RedirectToAction("Index", "Home");
            }

            // Получаем полные данные пользователя из базы
            var user = _userRepository.GetUserById(Convert.ToInt32(userId)).Data;

            // Передаем данные в ViewBag для модального окна
            ViewBag.UserJson = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                // Основная информация
                displayName = user.DisplayName ?? "",
                realName = user.RealName ?? "",
                firstName = user.FirstName ?? "",
                lastName = user.LastName ?? "",
                middleName = user.MiddleName ?? "",           // Отчество
                sex = user.Sex ?? "",
                birthday = user.Birthday?.ToString("yyyy-MM-dd") ?? "",
                birthPlace = user.BirthPlace ?? "",            // Место рождения

                // Контакты
                defaultEmail = user.DefaultEmail ?? "",
                alternativeEmail = user.AlternativeEmail ?? "", // Альтернативный email
                defaultPhone = user.DefaultPhone ?? "",
                alternativePhone = user.AlternativePhone ?? "", // Альтернативный телефон
                workPhone = user.WorkPhone ?? "",               // Рабочий телефон

                // Мессенджеры
                telegram = user.Telegram ?? "",                 // Telegram
                whatsApp = user.WhatsApp ?? "",                 // WhatsApp

                // Работа
                position = user.Position ?? "",                 // Должность
                department = user.Department ?? "",             // Отдел

                // Личное
                address = user.Address ?? "",                   // Адрес
                bio = user.Bio ?? "",                           // О себе
                website = user.Website ?? "",                   // Сайт
                linkedIn = user.LinkedIn ?? ""                  // LinkedIn
            });

            return View("~/Views/Account/Profile.cshtml", profile);
        }

        // POST: Profile/SwitchCompany
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SwitchCompany(int companyId, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });
            var result = _userService.SwitchCurrentCompany(Convert.ToInt32(userId), companyId);

            if (result)
            {
                TempData["Success"] = "Компания успешно переключена";
            }
            else
            {
                TempData["Error"] = "Не удалось переключить компанию";
            }

            // Переключение из бокового меню возвращает на ту же страницу (если в новом режиме
            // компании раздел недоступен, WorkspaceModeFilter перенаправит на задачи)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index");
        }

        // GET: api/profile/data
        [HttpGet("api/profile/data")]
        public async Task<IActionResult> GetProfileData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            var user = _userRepository.GetUserById(Convert.ToInt32(userId)).Data;
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            return Ok(new
            {
                // Основная информация
                displayName = user.DisplayName ?? "",
                realName = user.RealName ?? "",
                firstName = user.FirstName ?? "",
                lastName = user.LastName ?? "",
                middleName = user.MiddleName ?? "",
                sex = user.Sex ?? "",
                birthday = user.Birthday?.ToString("yyyy-MM-dd") ?? "",
                birthPlace = user.BirthPlace ?? "",

                // Контакты
                defaultEmail = user.DefaultEmail ?? "",
                alternativeEmail = user.AlternativeEmail ?? "",
                defaultPhone = user.DefaultPhone ?? "",
                alternativePhone = user.AlternativePhone ?? "",
                workPhone = user.WorkPhone ?? "",

                // Мессенджеры
                telegram = user.Telegram ?? "",
                whatsApp = user.WhatsApp ?? "",

                // Работа
                position = user.Position ?? "",
                department = user.Department ?? "",

                // Личное
                address = user.Address ?? "",
                bio = user.Bio ?? "",
                website = user.Website ?? "",
                linkedIn = user.LinkedIn ?? ""
            });
        }
        
        // PUT: api/profile/edit
        [HttpPut("api/profile/edit")]
        public async Task<IActionResult> EditProfile([FromBody] EditProfileModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            if (string.IsNullOrWhiteSpace(model.DisplayName))
                return BadRequest(new { message = "Отображаемое имя обязательно" });

            var result = await _userService.UpdateProfileAsync(Convert.ToInt32(userId), model);

            if (result)
                return Ok(new { message = "Профиль успешно обновлен" });
            else
                return BadRequest(new { message = "Не удалось обновить профиль" });
        }

        // POST: api/profile/avatar — загрузка фото профиля.
        // Браузер присылает уже обрезанный квадрат (см. Profile.cshtml), сервер проверяет
        // размер и сигнатуру файла и сохраняет его под своим именем.
        [HttpPost("api/profile/avatar")]
        [RequestSizeLimit(AvatarStorage.MaxFileSize + 64 * 1024)]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл не выбран" });
            if (file.Length > AvatarStorage.MaxFileSize)
                return BadRequest(new { message = "Файл больше 5 МБ" });

            byte[] content;
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                content = stream.ToArray();
            }

            var extension = AvatarStorage.DetectExtension(content);
            if (extension == null)
                return BadRequest(new { message = "Поддерживаются только JPG, PNG и WebP" });

            var user = _userRepository.GetUserById(Convert.ToInt32(userId)).Data;
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var oldAvatarId = user.DefaultAvatarId;
            var avatarId = await _avatarStorage.SaveAsync(content, extension);

            user.DefaultAvatarId = avatarId;
            user.IsAvatarEmpty = "false";
            user.ModifiedDate = DateTime.UtcNow;
            await _userRepository.AddOrUpdateAsync(user);

            _avatarStorage.Delete(oldAvatarId);

            return Ok(new { avatarUrl = AvatarStorage.GetUrl(user) });
        }

        // DELETE: api/profile/avatar — удалить фото профиля
        [HttpDelete("api/profile/avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            var user = _userRepository.GetUserById(Convert.ToInt32(userId)).Data;
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var oldAvatarId = user.DefaultAvatarId;
            user.DefaultAvatarId = null;
            user.IsAvatarEmpty = "true";
            user.ModifiedDate = DateTime.UtcNow;
            await _userRepository.AddOrUpdateAsync(user);

            _avatarStorage.Delete(oldAvatarId);

            return Ok(new { message = "Фото удалено" });
        }

        // GET: api/avatars/{id} — отдача фото (этот адрес уже используют списки сотрудников компании)
        [HttpGet("api/avatars/{id}")]
        public IActionResult GetAvatar(string id)
        {
            var path = _avatarStorage.GetPath(id);
            if (path == null)
                return NotFound();

            // Имя файла меняется при каждой загрузке, поэтому кэшировать можно надолго
            Response.Headers.CacheControl = "private, max-age=31536000, immutable";
            return PhysicalFile(path, AvatarStorage.GetContentType(id));
        }
    }
}
