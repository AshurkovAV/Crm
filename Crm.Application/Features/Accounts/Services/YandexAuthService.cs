using Crm.Application.Features.Accounts.DTOs;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Crm.Application.Features.Accounts.Services
{
    public class YandexAuthService : IYandexAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        public YandexAuthService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {           
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }
        public async Task<YandexTokenResponse> GetTokenAsync(string code)
        {
            // Обмен кода на access_token
            var tokenResponse = await ExchangeCodeForToken(code);

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();

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
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("OAuth", accessToken);

            var response = await client.GetAsync("https://login.yandex.ru/info?format=json");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<YandexUserInfoResponse>(content);
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
