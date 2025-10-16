namespace Crm.Core.Features.Account.Interfaces
{
    public interface IRememberDeviceService
    {
        Task<bool> SaveRememberTokenAsync(string email, string rememberToken, string deviceId);
        Task<bool> ValidateRememberTokenAsync(string email, string rememberToken, string deviceId);
        Task<bool> RemoveRememberTokenAsync(string email, string deviceId);
        Task<List<string>> GetUserDevicesAsync(string email);
    }
}
