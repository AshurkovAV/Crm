using Crm.Application.Features.Accounts.Commands.Yandex;
using Crm.Application.Features.Accounts.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace Crm.Controllers
{
    [Route("[controller]")]
    public class YandexAuthController: Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;

        public YandexAuthController(IMediator mediator,
            IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
        {
            _mediator = mediator;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration; 
        }

        [HttpGet("signin")] // GET /api/yandexauth/signin
        public IActionResult SignIn()
        {
            // 1. Генерация state-параметра для защиты от CSRF
            // 2. Формирование URL для перенаправления на Yandex OAuth
            // 3. Redirect(redirectUrl);
            throw new NotImplementedException();
        }

        [HttpGet("callback1")] // GET /yandexauth/callback
        public IActionResult Callback1()
        {
            return Ok();
        }

            [HttpGet("callback1")] // GET /api/yandexauth/callback
        public async Task<IActionResult> Callback1(string code, string cid, string deviceId)
        {
            // 1. Валидация state-параметра
            // 2. Обмен кода (code) на access_token
            // 3. Получение данных пользователя с Yandex API
            // 4. Вызов команды на создание/логин пользователя
            var pathBase = "https://oauth.yandex.ru";
            var client = new HttpClient();
            Uri baseUri = new Uri(pathBase);
            client.BaseAddress = baseUri;

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
            values.Add(new KeyValuePair<string, string>("code", $"{code}"));
            var content = new FormUrlEncodedContent(values);

            var base64EncodedAuthenticationString = "MzZkNTEzNDcyZDkyNGVhMjkwMTQwMWQ1MTk1OGJjNWM6N2RiYTRlMjVhN2IzNGEyMTk4MmVkZjc0NjZkMDFlZGI=";

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/token");
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            requestMessage.Content = content;

            var response = await client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var tookeniser = JsonConvert.DeserializeObject<UserTokenJson>(responseBody);
            var command = new CreateUserYandexCommand
            {
                AccessToken = code
            };

          //  var result = await _mediator.Send(command);

            // 5. Возврат результата (например, JWT-токен в куки или в теле ответа)
            return Ok();
        }


        [HttpGet("callback")]
        public async Task<IActionResult> Callback(
         string code,
         string cid,
         string? error = null,
         string? error_description = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(error))
                {
                    return RedirectToAction("Login", "Account", new { error = "yandex_auth_failed" });
                }             

                // Обмен кода на access_token
                var tokenResponse = await ExchangeCodeForToken(code);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();                
                        
                    return RedirectToAction("Login", "Account", new { error = "token_exchange_failed" });
                }

                // Чтение токена из ответа с Newtonsoft.Json
                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonConvert.DeserializeObject<YandexTokenResponse>(tokenContent);

                // Получение информации о пользователе
                var userInfo = await GetUserInfo(tokenData.AccessToken);

                // Ваша логика создания/авторизации пользователя
                // var result = await _userService.AuthenticateYandexUser(userInfo);

                return RedirectToAction("YandexAuthSuccess", "Account", new
                {
                    token = tokenData.AccessToken,
                    email = userInfo.DefaultEmail,
                    name = userInfo.DisplayName
                });
            }
            catch (Exception ex)
            {
                
                return RedirectToAction("Login", "Account", new { error = "internal_error" });
            }
        }

        private async Task<YandexUserInfoResponse> GetUserInfo(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("OAuth", accessToken);

            var response = await client.GetAsync("https://login.yandex.ru/info?format=json");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<YandexUserInfoResponse>(content);
        }

        private bool ValidateState(string state)
        {
            return !string.IsNullOrEmpty(state);
        }


        private async Task<HttpResponseMessage> ExchangeCodeForToken(string code)
        {
            var client = _httpClientFactory.CreateClient();

            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["client_id"] = _configuration["YandexOAuth:ClientId"],
                ["client_secret"] = _configuration["YandexOAuth:ClientSecret"]
            };

            var content = new FormUrlEncodedContent(requestData);
            return await client.PostAsync("https://oauth.yandex.ru/token", content);
        }
    }

    // Модели с атрибутами Newtonsoft.Json
    public class YandexTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class YandexUserInfoResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("real_name")]
        public string RealName { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("default_email")]
        public string DefaultEmail { get; set; }

        [JsonProperty("emails")]
        public List<string> Emails { get; set; }
    }
}
