using Crm.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Crm.ViewComponents
{
    public class IconViewComponent : ViewComponent
    {        

        private readonly IProfileService _profileService;

        public IconViewComponent(IProfileService profileService)
        {
            _profileService = profileService;
        }

        public IViewComponentResult Invoke()
        {
            var model = _profileService.GetProfileData();
            return View(model); // Будет искать Views/Shared/Components/Profile/Icon.cshtml
        }
    }
}
