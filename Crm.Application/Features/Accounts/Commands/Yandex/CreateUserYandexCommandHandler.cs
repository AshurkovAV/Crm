using Crm.Application.Features.Accounts.DTOs;
using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application.Features.Accounts.Commands.Yandex
{
    public class CreateUserYandexCommandHandler
    {
        private IUserRepository _userRepository;

        public CreateUserYandexCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        public async Task<UserToken> Get(string code)
        {
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
            var usertooken = _userRepository.InsertUserToken(new UserToken
            {
                AccessToken = tookeniser.access_token,
                ExpiresIn = tookeniser.expires_in,
                RefreshToken = tookeniser.refresh_token,
                Scope = tookeniser.scope,
                TokenType = tookeniser.token_type
            });
            return usertooken;
        }
    }
}
