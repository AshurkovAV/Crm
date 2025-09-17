using Crm.Application.Features.Accounts.Commands.CreateUser;
using Crm.Application.Features.Accounts.DTOs;
using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using MediatR;
using Newtonsoft.Json;
using System.Net.Http.Headers;


namespace Crm.Application.Features.Accounts.Commands.ExternalAuth.Yandex
{
    public class YandexAuthCommandHandler : IRequestHandler<CreateUserYandexCommand, CreateUserResult>
    {
        private IUserRepository _userRepository;
        public YandexAuthCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }      

        public async Task<CreateUserResult> Handle(CreateUserYandexCommand request, CancellationToken cancellationToken)
        {
            var pathBase = "https://oauth.yandex.ru";
            var client = new HttpClient();
            Uri baseUri = new Uri(pathBase);
            client.BaseAddress = baseUri;

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
            values.Add(new KeyValuePair<string, string>("code", $"{request.Code}"));
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
            return new CreateUserResult {
            UserToken = usertooken,
            } ;
        }
    }
}
