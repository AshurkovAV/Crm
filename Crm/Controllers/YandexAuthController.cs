using Crm.Application.Features.Accounts.Commands.ExternalAuth.Yandex;
using Crm.Application.Features.Accounts.DTOs;
using Crm.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Crm.Controllers
{
    [Route("[controller]")]
    public class YandexAuthController: Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        private readonly Crm.Application.Interfaces.IAuthenticationService _authenticationService;
        private readonly ILogger<YandexAuthController> _logger;

        public YandexAuthController(IMediator mediator,
            IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        Crm.Application.Interfaces.IAuthenticationService authenticationService,
        ILogger<YandexAuthController> logger)
        {
            _mediator = mediator;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _authenticationService = authenticationService;
            _logger = logger;
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
            var command = new YandexAuthCommand
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
            // Callback открывается во всплывающем окне. Раньше при любой ошибке здесь делался
            // редирект на Account/Login — и в попапе появлялся экран ввода пароля. Теперь попап
            // всегда получает страницу, которая сообщает результат основному окну и закрывается.
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("Яндекс вернул ошибку авторизации: {Error} {Description}", error, error_description);
                return AuthResult(false, error: "Вход через Яндекс отменён или не разрешён.");
            }

            try
            {
                var result = await _mediator.Send(new CreateUserYandexCommand { Code = code });

                if (!result.Succeeded || result.User == null)
                {
                    _logger.LogWarning("Не удалось создать/обновить пользователя Яндекса: {Errors}", string.Join("; ", result.Errors));
                    return AuthResult(false, error: result.Errors.FirstOrDefault() ?? "Не удалось войти через Яндекс.");
                }

                // Куку выставляем прямо здесь, по пользователю, которого только что сохранили.
                // Раньше вход делался отдельным GET /Account/YandexAuthSuccess?email=..., который
                // логинил любого пользователя по email из адресной строки.
                await _authenticationService.AuthenticateWithCookiesAsync(result.User);

                return AuthResult(true, result.User.DefaultEmail, result.User.DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка авторизации через Яндекс");
                return AuthResult(false, error: "Ошибка при входе через Яндекс. Попробуйте ещё раз.");
            }
        }

        private IActionResult AuthResult(bool succeeded, string? email = null, string? name = null, string? error = null)
        {
            return View("~/Views/Account/YandexAuthSuccess.cshtml", new YandexAuthSuccessViewModel
            {
                Succeeded = succeeded,
                Email = email ?? string.Empty,
                Name = name ?? string.Empty,
                Error = error ?? string.Empty
            });
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
       
    
}
