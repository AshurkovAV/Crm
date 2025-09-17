using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Web;

namespace Crm.Controllers
{
    [Route("[controller]")]
    public class VkAuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;

        public VkAuthController(IMediator mediator,
            IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
        {
            _mediator = mediator;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration; 
        }


        [HttpGet("callback")]
        public IActionResult Callback(
        [FromQuery] string access_token,
        [FromQuery] int user_id,
        [FromQuery] long expires_in,
        [FromQuery] string email = "")
        {
            try
            {
                // Валидация токена
                if (string.IsNullOrEmpty(access_token) || user_id <= 0)
                {
                    return Redirect($"{Request.Scheme}://{Request.Host}/auth-error?message=Invalid token data");
                }

                // Здесь ваша бизнес-логика:
                // 1. Проверка токена через VK API (опционально)
                // 2. Поиск/создание пользователя в БД
                // 3. Создание сессии

                // Пример: сохраняем в куки
                Response.Cookies.Append("vk_access_token", access_token, new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddSeconds(expires_in),
                    HttpOnly = true,
                    Secure = true
                });

                Response.Cookies.Append("vk_user_id", user_id.ToString(), new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddSeconds(expires_in),
                    HttpOnly = true,
                    Secure = true
                });

                // Редирект на главную или личный кабинет
                return Redirect($"{Request.Scheme}://{Request.Host}/");
            }
            catch (Exception ex)
            {
                // Редирект на страницу ошибки
                return Redirect($"{Request.Scheme}://{Request.Host}/auth-error?message={HttpUtility.UrlEncode(ex.Message)}");
            }
        }



        [HttpPost("callback1")]
        public async Task<IActionResult> ProcessVkAuth([FromBody] VkAuthRequest request)
        {
            try
            {
                // Валидируем токен через VK API
                var userInfo = await ValidateVkToken(request.AccessToken, request.UserId);

                //// Создаем/находим пользователя
                //var user = await FindOrCreateUser(new UserCreateModel
                //{
                //    Email = request.Email,
                //    ExternalId = request.UserId.ToString(),
                //    Provider = "VK",
                //    FirstName = userInfo.FirstName,
                //    LastName = userInfo.LastName
                //});

                //// Создаем сессию
                //await Authenticate(user.Email);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {                
                return Ok(new { success = false, message = "Ошибка авторизации" });
            }
        }

        private async Task<VkUserInfo> ValidateVkToken(string accessToken, long userId)
        {
            using var client = new HttpClient();
            var url = $"https://api.vk.com/method/users.get?user_ids={userId}&fields=first_name,last_name&access_token={accessToken}&v=5.131";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<VkApiResponse>(content);

            return result.Response.First();
        }
    }
    public class VkAuthRequest
    {
        public string AccessToken { get; set; }
        public long UserId { get; set; }
        public string Email { get; set; }
        public int ExpiresIn { get; set; }
    }
    public class VkApiResponse
    {
        [JsonProperty("response")]
        public List<VkUserInfo> Response { get; set; }
    }

    public class VkUserInfo
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }
    }
}
