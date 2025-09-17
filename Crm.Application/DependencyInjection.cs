using Crm.Application.Features.Accounts.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Crm.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Регистрируем все обработчики команд из текущей сборки
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // Здесь можно зарегистрировать другие сервисы уровня приложения
            // services.AddScoped<IUserService, UserService>();
            services.AddScoped<IYandexAuthService, YandexAuthService>();
            return services;
        }
    }
}
