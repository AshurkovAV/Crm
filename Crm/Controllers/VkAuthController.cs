using Crm.Application.Features.Accounts.Commands.Yandex;
using Crm.Application.Features.Accounts.DTOs;
using Crm.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

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

        [HttpPost("callback")]
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
