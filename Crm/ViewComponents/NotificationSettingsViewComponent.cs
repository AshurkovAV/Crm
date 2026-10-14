using System.Security.Claims;
using Crm.Controllers;
using Crm.Services.Notifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Crm.ViewComponents
{
    /// <summary>Карточка «Уведомления» в профиле: подключение MAX и переключатели по событиям и каналам.</summary>
    public class NotificationSettingsViewComponent : ViewComponent
    {
        private readonly NotificationOptions _options;

        public NotificationSettingsViewComponent(IOptions<NotificationOptions> options) => _options = options.Value;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claim = (User as ClaimsPrincipal)?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(claim, out var userId))
                return Content(string.Empty);
            return View(await NotificationSettingsState.LoadAsync(userId, _options));
        }
    }
}
