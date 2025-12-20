using Crm.Entity.ModelsCrm;
using Crm.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Crm.Controllers
{
    public class TestController : Controller
    {
        [HttpGet("/test-session")]
        public IActionResult TestSession()
        {
            var session = HttpContext.Session;

            var response = new
            {
                SessionId = session.Id,
                IsAvailable = session.IsAvailable,
                User = session.GetCurrentUser(),
                Keys = session.Keys.ToArray(),
                Cookies = HttpContext.Request.Cookies.Keys.ToArray()
            };

            return Json(response);
        }

        [HttpGet("/test-set-session")]
        public IActionResult SetTestSession()
        {
            var testUser = new User
            {
                Id = 1,
                DefaultEmail = "test@test.com",
                Login = "testuser",
                DisplayName = "Test User",
                FirstName = "Test",
                LastName = "User",
                Role = "User",
                IsActive = true
            };

            HttpContext.Session.SetCurrentUser(testUser);

            return Content($"Session set for: {testUser.DefaultEmail}");
        }
    }
}
