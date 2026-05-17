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

        // Вариант 1: Внедрение через конструктор (рекомендуемый)
        public ProfileController(
            IUserRepository userRepository,
            ICompanyRepository companyRepository,
            UserService userService)
        {
            _userRepository = userRepository;
            _companyRepository = companyRepository;
            _userService = userService;
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
        public ActionResult SwitchCompany(int companyId)
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
    }
}
