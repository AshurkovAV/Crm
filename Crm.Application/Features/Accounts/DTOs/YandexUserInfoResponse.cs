using Newtonsoft.Json;

namespace Crm.Application.Features.Accounts.DTOs
{
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
