using Crm.Application.Features.Accounts.Services;
using Crm.Application.Features.Email.Services;
using Crm.Core.Features.Email.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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
            services.AddScoped<IMailService, MailService>();

            return services;
        }
    }
}
