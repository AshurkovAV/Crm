using Crm.Core.Features.Account.Interfaces;
using Crm.Entity.ModelsCrm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Entity.Infrastructure.Services
{
    public class RememberDeviceService : IRememberDeviceService
    {

        private readonly ILogger<RememberDeviceService> _logger;

        public RememberDeviceService(ILogger<RememberDeviceService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> SaveRememberTokenAsync(string email, string rememberToken, string deviceId)
        {
            try
            {
                using (var db = new CrmContext())
                {
                    // Удаляем старый токен для этого устройства (если есть)
                    var existing = await db.RememberedDevices
                        .FirstOrDefaultAsync(rd => rd.Email == email && rd.DeviceId == deviceId);

                    if (existing != null)
                    {
                        db.RememberedDevices.Remove(existing);
                    }

                    // Сохраняем новый токен
                    var rememberedDevice = new RememberedDevice
                    {
                        Email = email,
                        RememberToken = rememberToken,
                        DeviceId = deviceId,
                        Expiration = DateTime.UtcNow.AddDays(30),
                        CreatedAt = DateTime.UtcNow
                    };

                    db.RememberedDevices.Add(rememberedDevice);
                    await db.SaveChangesAsync();

                    return true;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении remember token для {Email}", email);
                return false;
            }
        }

        public async Task<bool> ValidateRememberTokenAsync(string email, string rememberToken, string deviceId)
        {
            using (var db = new CrmContext())
            {
                var device = await db.RememberedDevices
                .FirstOrDefaultAsync(rd => rd.Email == email
                                        && rd.DeviceId == deviceId
                                        && rd.RememberToken == rememberToken
                                        && rd.Expiration > DateTime.UtcNow);

                return device != null;
            }
            
        }

        public async Task<bool> RemoveRememberTokenAsync(string email, string deviceId)
        {
            using (var db = new CrmContext())
            {
                var device = await db.RememberedDevices
                .FirstOrDefaultAsync(rd => rd.Email == email && rd.DeviceId == deviceId);

                if (device != null)
                {
                    db.RememberedDevices.Remove(device);
                    await db.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            
        }

        public async Task<List<string>> GetUserDevicesAsync(string email)
        {
            using (var db = new CrmContext())
            {
                return await db.RememberedDevices
                .Where(rd => rd.Email == email && rd.Expiration > DateTime.UtcNow)
                .Select(rd => rd.DeviceId)
                .ToListAsync();
            }
            
        }
    }
}
