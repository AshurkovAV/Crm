using Crm.Application.Features.Accounts.DTOs;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Crm.Application.Features.Accounts.Services
{
    public class YandexAuthService : IYandexAuthService
    {
        // Без явного таймаута HttpClient ждёт по умолчанию 100 секунд — снаружи это выглядит
        // как "тихо повисло" (пользователь видит попап, который просто ничего не делает), а в
        // логе до истечения этого времени нет вообще ничего. С коротким таймаутом зависшее
        // соединение падает быстро и с понятным исключением в логе.
        private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(15);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<YandexAuthService> _logger;

        public YandexAuthService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<YandexAuthService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task<YandexTokenResponse> GetTokenAsync(string code)
        {
            // Обмен кода на access_token
            HttpResponseMessage tokenResponse;
            try
            {
                tokenResponse = await ExchangeCodeForToken(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось обменять code на access_token на oauth.yandex.ru/token");
                throw;
            }

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogError("oauth.yandex.ru/token вернул {StatusCode}: {Body}", tokenResponse.StatusCode, errorContent);

                throw new Exception("token_exchange_failed");
            }
            // Чтение токена из ответа с Newtonsoft.Json
            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<YandexTokenResponse>(tokenContent);

            // Получение информации о пользователе
            var userInfo = await GetUserInfoAsync(tokenData.AccessToken);

            return JsonConvert.DeserializeObject<YandexTokenResponse>(tokenContent);
        }

        public async Task<YandexUserInfoResponse> GetUserInfoAsync(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = HttpTimeout;
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("OAuth", accessToken);

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync("https://login.yandex.ru/info?format=json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось получить данные пользователя с login.yandex.ru/info");
                throw;
            }
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<YandexUserInfoResponse>(content);
        }


        private async Task<HttpResponseMessage> ExchangeCodeForToken(string code)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = HttpTimeout;

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
