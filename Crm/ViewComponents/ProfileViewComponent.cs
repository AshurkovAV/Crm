using Crm.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Crm.ViewComponents
{
    public class ProfileViewComponent : ViewComponent
    {        

        private readonly IProfileService _profileService;

        public ProfileViewComponent(IProfileService profileService)
        {
            _profileService = profileService;
        }

        public IViewComponentResult Invoke()
        {
            var model = _profileService.GetProfileData();
            return View(model);
        }
    }
}
