using Crm.Models.Compan;
using Crm.Models.Responses;
using Crm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Crm.Entity.Services;

namespace Crm.Controllers.Compan
{
    /// <summary>
    /// Переход сотрудника по ссылке-приглашению /invite/{code}.
    /// Три сценария:
    ///  - человек не зарегистрирован      → форма регистрации (Accept);
    ///  - зарегистрирован, но не вошёл     → вход по паролю или через Яндекс (Login);
    ///  - уже вошёл                         → подтверждение «Присоединиться» (Join),
    ///    либо Conflict, если вошёл под другим email.
    /// После успеха — страница Joined с переходом в CRM.
    /// </summary>
    [AllowAnonymous]
    [Route("invite")]
    public class InviteController : Controller
    {
        private const string JoinedCompanyKey = "InviteJoinedCompany";
        private const string JoinedNewUserKey = "InviteJoinedNewUser";
        private const string JoinedAlreadyKey = "InviteJoinedAlready";

        private readonly IInvitationService _invitationService;
        private readonly IUserRepository _userRepository;
        private readonly Application.Interfaces.IAuthenticationService _authenticationService;
        private readonly ILogger<InviteController> _logger;

        public InviteController(
            IInvitationService                            invitationService,
            IUserRepository                               userRepository,
            Application.Interfaces.IAuthenticationService authenticationService,
            ILogger<InviteController>                     logger)
        {
            _invitationService = invitationService;
            _userRepository = userRepository;
            _authenticationService = authenticationService;
            _logger = logger;
        }

        // GET: /invite/{code}
        [HttpGet("{code}")]
        public async Task<IActionResult> Index(string code)
        {
            try
            {
                var invitation = await _invitationService.CheckInvitationAsync(code);
                if (!invitation.Valid)
                    return ErrorView(invitation.Error, code, invitation.Status);

                var model = BuildModel(code, invitation);

                // Уже вошёл в систему
                var currentUser = GetCurrentUser();
                if (currentUser != null)
                {
                    model.CurrentUserEmail = currentUser.DefaultEmail;
                    model.CurrentUserName = currentUser.DisplayName ?? currentUser.DefaultEmail;

                    if (string.IsNullOrEmpty(invitation.Email) ||
                        string.Equals(currentUser.DefaultEmail, invitation.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        return View("Join", model);
                    }

                    return View("Conflict", model);
                }

                // Зарегистрирован, но не вошёл
                if (!string.IsNullOrEmpty(invitation.Email))
                {
                    var existingUser = _userRepository.GetUser(invitation.Email).Data;
                    if (existingUser != null)
                    {
                        model.ExistingUserId = existingUser.Id;
                        return View("Login", model);
                    }
                }

                // Новый человек — регистрация
                model.DisplayName = model.GetSuggestedDisplayName();
                return View("Accept", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка открытия приглашения {Code}", code);
                return ErrorView("Произошла ошибка при обработке приглашения. Попробуйте позже.", code);
            }
        }

        // POST: /invite/{code} — регистрация нового пользователя
        [HttpPost("{code}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string code, AcceptInvitationRequest request)
        {
            try
            {
                var invitation = await _invitationService.CheckInvitationAsync(code);
                if (!invitation.Valid)
                    return ErrorView(invitation.Error, code, invitation.Status);

                // Email берём из приглашения, а не из формы
                var email = invitation.Email ?? request.Email;
                if (!string.IsNullOrEmpty(email) && _userRepository.GetUser(email).Data != null)
                    return RedirectToAction(nameof(Index), new { code });

                if (string.IsNullOrEmpty(invitation.Email) && string.IsNullOrWhiteSpace(request.Email))
                    ModelState.AddModelError(nameof(request.Email), "Укажите email");

                if (!ModelState.IsValid)
                    return AcceptWithErrors(code, invitation, request, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                var result = await _invitationService.AcceptInvitationAsync(code, request);
                if (!result.Success)
                    return AcceptWithErrors(code, invitation, request, new[] { result.Message });

                var newUser = _userRepository.GetUserById(result.UserId).Data;
                if (newUser != null)
                    await _authenticationService.AuthenticateWithCookiesAsync(newUser);

                _logger.LogInformation("Новый пользователь {UserId} зарегистрирован по приглашению {Code}", result.UserId, code);
                return Joined(invitation.CompanyName, isNewUser: true, alreadyMember: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка регистрации по приглашению {Code}", code);
                return ErrorView("Произошла ошибка при регистрации. Попробуйте позже.", code);
            }
        }

        // POST: /invite/{code}/login — вход существующего пользователя и принятие приглашения
        [HttpPost("{code}/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string code, string? email, string password)
        {
            var invitation = await _invitationService.CheckInvitationAsync(code);
            if (!invitation.Valid)
                return ErrorView(invitation.Error, code, invitation.Status);

            // Для приглашения по email войти можно только под этим email
            var loginEmail = invitation.Email ?? email;
            var model = BuildModel(code, invitation);
            model.Email = loginEmail;

            if (string.IsNullOrWhiteSpace(loginEmail) || string.IsNullOrEmpty(password))
            {
                model.Errors.Add("Введите пароль");
                return View("Login", model);
            }

            if (!await _userRepository.VerifyPasswordAsync(loginEmail, password))
            {
                model.Errors.Add("Неверный пароль. Если вы входили через Яндекс — нажмите «Войти через Яндекс».");
                return View("Login", model);
            }

            var user = _userRepository.GetUser(loginEmail).Data;
            if (user == null)
            {
                model.Errors.Add("Пользователь не найден");
                return View("Login", model);
            }

            await _authenticationService.AuthenticateWithCookiesAsync(user);

            var result = await _invitationService.AcceptInvitationForExistingUserAsync(code, user.Id);
            if (!result.Success)
                return ErrorView(result.Message, code);

            return Joined(result.CompanyName, isNewUser: false, result.AlreadyMember);
        }

        // POST: /invite/{code}/join — уже вошедший пользователь подтверждает вступление
        [HttpPost("{code}/join")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(string code)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return RedirectToAction(nameof(Index), new { code });

            var result = await _invitationService.AcceptInvitationForExistingUserAsync(code, userId.Value);
            if (!result.Success)
                return ErrorView(result.Message, code);

            return Joined(result.CompanyName, isNewUser: false, result.AlreadyMember);
        }

        // POST: /invite/{code}/switch — выйти из чужого аккаунта и вернуться к приглашению
        [HttpPost("{code}/switch")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchAccount(string code)
        {
            await _authenticationService.SignOutAsync();
            return RedirectToAction(nameof(Index), new { code });
        }

        // GET: /invite/joined — экран «Вы в команде»
        [HttpGet("joined")]
        public IActionResult JoinedPage()
        {
            var companyName = TempData[JoinedCompanyKey] as string;
            if (companyName == null)
                return RedirectToAction("Index", "Home");

            ViewBag.CompanyName = companyName;
            ViewBag.IsNewUser = TempData[JoinedNewUserKey] as bool? ?? false;
            ViewBag.AlreadyMember = TempData[JoinedAlreadyKey] as bool? ?? false;
            return View("Joined");
        }

        // GET: /invite/{code}/check — используется окном «Ввести код» на главной
        [HttpGet("{code}/check")]
        public async Task<IActionResult> Check(string code)
        {
            var result = await _invitationService.CheckInvitationAsync(code);
            if (!result.Valid)
                return BadRequest(new { valid = false, message = result.Error });

            return Ok(new
            {
                valid = true,
                email = result.Email,
                companyName = result.CompanyName,
                inviterName = result.InviterName,
                expiresAt = result.ExpiresAt,
                daysLeft = result.DaysLeft
            });
        }

        // POST: /invite/{code}/accept — принятие кода из окна «Ввести код» (пользователь уже вошёл)
        [HttpPost("{code}/accept")]
        public async Task<IActionResult> AcceptInvitation(string code)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "Пользователь не авторизован" });

            var result = await _invitationService.AcceptInvitationForExistingUserAsync(code, userId.Value);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new
            {
                success = true,
                message = result.Message,
                companyId = result.CompanyId,
                alreadyMember = result.AlreadyMember
            });
        }

        // ===================== вспомогательное =====================

        private IActionResult Joined(string? companyName, bool isNewUser, bool alreadyMember)
        {
            TempData[JoinedCompanyKey] = companyName ?? "компании";
            TempData[JoinedNewUserKey] = isNewUser;
            TempData[JoinedAlreadyKey] = alreadyMember;
            return RedirectToAction(nameof(JoinedPage));
        }

        private int? GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated != true)
                return null;
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
        }

        private Crm.Entity.ModelsCrm.User? GetCurrentUser()
        {
            var id = GetCurrentUserId();
            return id == null ? null : _userRepository.GetUserById(id.Value).Data;
        }

        private static AcceptInvitationViewModel BuildModel(string code, InvitationCheckResponse invitation) => new()
        {
            Code = code,
            Email = invitation.Email,
            EmailFromInvitation = !string.IsNullOrEmpty(invitation.Email),
            Phone = invitation.Phone,
            CompanyId = invitation.CompanyId,
            CompanyName = invitation.CompanyName,
            InviterName = invitation.InviterName,
            Message = invitation.Message,
            ExpiresAt = invitation.ExpiresAt,
            DaysLeft = invitation.DaysLeft
        };

        private IActionResult AcceptWithErrors(string code, InvitationCheckResponse invitation, AcceptInvitationRequest request, IEnumerable<string> errors)
        {
            var model = BuildModel(code, invitation);
            model.Email = invitation.Email ?? request.Email;
            model.DisplayName = request.DisplayName;
            model.Phone = request.Phone ?? invitation.Phone;
            model.Position = request.Position;
            model.Errors = errors.Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
            return View("Accept", model);
        }

        private IActionResult ErrorView(string? error, string code, string? status = null)
        {
            ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;
            return View("Error", new InvitationErrorViewModel
            {
                Error = error ?? "Приглашение недействительно",
                Code = code,
                Title = status switch
                {
                    "Accepted" => "Приглашение уже принято",
                    "Expired" => "Срок приглашения истёк",
                    "Revoked" => "Приглашение отозвано",
                    _ => "Приглашение недоступно"
                }
            });
        }
    }
}
