using Crm.Entity.DTO;
using Crm.Entity.Entities;
using Crm.Entity.Services;
using Crm.Models;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Crm.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class CompanyController : Controller
    {
        private readonly ILogger<CompanyController> _logger;
        private ICompanyRepository _companyRepository;
        private IUserRepository _userRepository;

        public CompanyController(
            ICompanyRepository companyRepository,
            IUserRepository userRepository,
            ILogger<CompanyController> logger)
        {
                _companyRepository = companyRepository;            
            _userRepository = userRepository;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("GetUsers")]
        public async Task<object> Get(DataSourceLoadOptions loadOptions)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            var currentUserId = int.Parse(userId);

            // Получаем пользователя
            var currentUser = _userRepository.GetUserById(currentUserId);

            // Получаем ID компании пользователя (может быть null)
            var companyData = await _companyRepository.GetCompanyUsersByUserIdAsync(currentUserId);

            List<CompanyUserDto> users;

            if (companyData != null)
            {
                // Если у пользователя есть компания - показываем всех сотрудников этой компании
                users = companyData;
            }
            else
            {
                // Если компании нет - показываем только самого пользователя
                users = new List<CompanyUserDto>
        {
            new CompanyUserDto
            {
                UserId = currentUser.Data.Id,
                DisplayName = currentUser.Data.DisplayName,
                Email = currentUser.Data.DefaultEmail,
                AvatarUrl = currentUser.Data.DefaultAvatarId,
                Role = "Владелец (без компании)"
                
            }
        };
            }

            return DataSourceLoader.Load(users, loadOptions);

        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран");

            // Проверка на тип файла
            if (!file.ContentType.StartsWith("image/"))
                return BadRequest("Можно загружать только изображения");

            // Сохраняем файл
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine("wwwroot/uploads/avatars", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var avatarUrl = $"/uploads/avatars/{fileName}";
            return Json(new { avatarUrl });
        }

        [HttpDelete]
        public IActionResult Delete(int key)
        {
            try
            {                
                var dataUser = _userRepository.GetUserById(key);
                if (dataUser.Success)
                {
                    var data = _companyRepository.GetCompanyUserToUserId(dataUser.Data.Id, dataUser.Data.CurrentCompanyId);
                    if (data.Result.Success)
                    {
                        if (data.Result.Data.Role != "Admin")
                        {
                            data.Result.Data.IsActive = false;
                            _companyRepository.AddOrUpdateAsync(data.Result.Data);
                            return Ok(new { success = true, message = "Сотрудник успешно удален" });
                        }
                    }
                }
                return Ok(new { success = false, message = "Ошибка удаления" });

            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
        private string GenerateInvitationCode()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "")
                .Substring(0, 16);
        }

    }
}
