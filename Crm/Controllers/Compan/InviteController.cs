using Crm.Entity.ModelsCrm;
using Crm.Entity;
using Crm.Models.Compan;
using Crm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Crm.Entity.Services;
using Crm.Models;
using Microsoft.AspNetCore.Identity;
using Crm.Application.Interfaces;

namespace Crm.Controllers.Compan
{
    [AllowAnonymous]
    [Route("invite")]
    public class InviteController : Controller
    {
        private readonly IInvitationService _invitationService;
        private readonly IUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;
        private IUserContextService _userContextService;
        private readonly Application.Interfaces.IAuthenticationService _authenticationService;
        private readonly ILogger<InviteController> _logger;

        public InviteController(
            IInvitationService                            invitationService,
            IUserRepository                               userRepository,
            ICompanyRepository                            companyRepository,
            Application.Interfaces.IAuthenticationService authenticationService,
            IUserContextService                           userContextService,
            ILogger<InviteController>                     logger)
        {
            _userRepository = userRepository;  
            _invitationService = invitationService;
            _companyRepository = companyRepository;
            _userContextService = userContextService;   
            _authenticationService = authenticationService;
            _logger = logger;
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> Index(string code)
        {
            try
            {
                _logger.LogInformation("Processing invitation code: {Code}", code);

                if (string.IsNullOrEmpty(code))
                {
                    return View("Error", new InvitationErrorViewModel
                    {
                        Error = "Код приглашения не указан",
                        Code = code
                    });
                }

                // Проверяем приглашение
                var invitation = await _invitationService.CheckInvitationAsync(code);
                if (!invitation.Valid)
                {
                    return View("Error", new InvitationErrorViewModel
                    {
                        Error = invitation.Error,
                        Code = code
                    });
                }
                var userId = _userContextService.GetCurrentUserId();
                // Если пользователь уже авторизован
                if (userId != null)
                {
                    // Получаем текущего пользователя
                    var currentUserResult = _userRepository.GetUserById(userId);
                    if (currentUserResult.Success && currentUserResult.Data != null)
                    {
                        // Проверяем, совпадает ли email с приглашением
                        if (currentUserResult.Data.DefaultEmail == invitation.Email)
                        {
                            // Сразу принимаем приглашение
                            var acceptResult = await _invitationService.AcceptInvitationForExistingUserAsync(code, currentUserResult.Data.Id);

                            if (acceptResult.Success)
                            {
                                TempData["SuccessMessage"] = $"Вы успешно присоединились к компании {acceptResult.CompanyName}!";
                                return RedirectToAction("Index", "Home");
                            }
                            else
                            {
                                return View("Error", new InvitationErrorViewModel
                                {
                                    Error = acceptResult.Message,
                                    Code = code
                                });
                            }
                        }
                        else
                        {
                            // Другой пользователь - конфликт
                            var model1 = new AcceptInvitationViewModel
                            {
                                Code = code,
                                Email = invitation.Email,
                                CompanyName = invitation.CompanyName,
                                Message = $"Вы авторизованы как {currentUserResult.Data.DefaultEmail}, но приглашение отправлено на {invitation.Email}. Выйдите и войдите под правильным аккаунтом."
                            };
                            return View("Conflict", model1);
                        }
                    }
                }

                // Проверяем, зарегистрирован ли пользователь (но не авторизован)
                var existingUserResult = _userRepository.GetUser(invitation.Email);
                var existingUser = existingUserResult.Success ? existingUserResult.Data : null;

                var model = new AcceptInvitationViewModel
                {
                    Code = code,
                    Email = invitation.Email,
                    Phone = invitation.Phone,
                    DepartmentId = invitation.DepartmentId,
                    DepartmentName = invitation.DepartmentName,
                    CompanyName = invitation.CompanyName,
                    Position = invitation.Position,
                    CompanyId = invitation.CompanyId,
                    ExpiresAt = invitation.ExpiresAt,
                    DaysLeft = invitation.ExpiresAt.HasValue
                        ? (int?)Math.Max(0, (invitation.ExpiresAt.Value - DateTime.Now).Days)
                        : null,
                    ExistingUserId = existingUser?.Id
                };

                // Если пользователь зарегистрирован, показываем форму входа
                if (existingUser != null)
                {
                    ViewBag.ReturnUrl = Url.Action("Index", "Invite", new { code = code });
                    return View("Login", model);
                }

                // Иначе - форма регистрации
                return View("Accept", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing invitation code: {Code}", code);
                return View("Error", new InvitationErrorViewModel
                {
                    Error = "Произошла ошибка при обработке приглашения. Пожалуйста, попробуйте позже.",
                    Code = code
                });
            }
        }


        /// <summary>
        /// Определяет состояние пользователя
        /// </summary>
        private UserState GetUserState(User existingUser)
        {
            // 1. Пользователь НЕ зарегистрирован
            if (existingUser == null)
            {
                return UserState.NotRegistered;
            }

            // 2. Пользователь зарегистрирован, но НЕ авторизован
            if (!User.Identity.IsAuthenticated)
            {
                return UserState.RegisteredNotAuthenticated;
            }

            // 3. Пользователь зарегистрирован И авторизован
            var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == existingUser.Id)
            {
                return UserState.RegisteredAndAuthenticated;
            }

            // 4. Авторизован, но под другим email
            return UserState.AuthenticatedMismatch;
        }


        // POST: /invite/{code}
        [HttpPost("{code}")]
        public async Task<IActionResult> Index(string code, AcceptInvitationRequest request)
        {
            try
            {
                _logger.LogInformation("Processing invitation acceptance for code: {Code}", code);

                if (!ModelState.IsValid)
                {
                    var invitation = await _invitationService.CheckInvitationAsync(code);

                    var model = new AcceptInvitationViewModel
                    {
                        Code = code,
                        Email = invitation.Email,
                        Phone = invitation.Phone,
                        DepartmentId = invitation.DepartmentId,
                        DepartmentName = invitation.DepartmentName,
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    };

                    return View("Accept", model);
                }

                // Проверяем приглашение
                var invitationCheck = await _invitationService.CheckInvitationAsync(code);
                if (!invitationCheck.Valid)
                {
                    return View("Error", new InvitationErrorViewModel
                    {
                        Error = invitationCheck.Error ?? "Приглашение недействительно",
                        Code = code
                    });
                }

                // Ищем существующего пользователя
                var existingUser = _userRepository.GetUser(invitationCheck.Email).Data;

                // Если пользователь существует - проверяем пароль
                if (existingUser != null)
                {
                    // Проверяем пароль
                    var isPasswordValid = await _userRepository.VerifyPasswordAsync(invitationCheck.Email, request.Password);

                    if (!isPasswordValid)
                    {
                        _logger.LogWarning("Invalid password for existing user: {Email}", invitationCheck.Email);

                        ModelState.AddModelError("Password", "Неверный пароль");

                        return View("Accept", new AcceptInvitationViewModel
                        {
                            Code = code,
                            Email = invitationCheck.Email,
                            Phone = invitationCheck.Phone,
                            DepartmentId = invitationCheck.DepartmentId,
                            DepartmentName = invitationCheck.DepartmentName,
                            CompanyName = invitationCheck.CompanyName,
                            Position = invitationCheck.Position,
                            Errors = new List<string> { "Неверный пароль. Пожалуйста, попробуйте снова." }
                        });
                    }
                }
                else
                {
                    // Новый пользователь - создаём с проверкой пароля
                    if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6)
                    {
                        ModelState.AddModelError("Password", "Пароль должен быть не менее 6 символов");

                        return View("Accept", new AcceptInvitationViewModel
                        {
                            Code = code,
                            Email = invitationCheck.Email,
                            Phone = invitationCheck.Phone,
                            DepartmentId = invitationCheck.DepartmentId,
                            DepartmentName = invitationCheck.DepartmentName,
                            CompanyName = invitationCheck.CompanyName,
                            Position = invitationCheck.Position,
                            Errors = new List<string> { "Пароль должен быть не менее 6 символов" }
                        });
                    }

                    if (request.Password != request.ConfirmPassword)
                    {
                        ModelState.AddModelError("ConfirmPassword", "Пароли не совпадают");

                        return View("Accept", new AcceptInvitationViewModel
                        {
                            Code = code,
                            Email = invitationCheck.Email,
                            Phone = invitationCheck.Phone,
                            DepartmentId = invitationCheck.DepartmentId,
                            DepartmentName = invitationCheck.DepartmentName,
                            CompanyName = invitationCheck.CompanyName,
                            Position = invitationCheck.Position,
                            Errors = new List<string> { "Пароли не совпадают" }
                        });
                    }
                }

                // Принимаем приглашение (создаём пользователя или добавляем в компанию)
                var result = await _invitationService.AcceptInvitationAsync(code, request);

                if (result.Success)
                {
                    _logger.LogInformation("Invitation accepted successfully for code: {Code}", code);

                    // Если пользователь новый - показываем успешную регистрацию
                    if (existingUser == null)
                    {
                        // Новый пользователь - получаем созданного пользователя и авторизуем
                        var newUserResult = _userRepository.GetUser(invitationCheck.Email);
                        if (newUserResult.Success && newUserResult.Data != null)
                        {
                            // Авторизуем нового пользователя через ваш сервис
                            await _authenticationService.AuthenticateWithCookiesAsync(newUserResult.Data);

                            TempData["SuccessMessage"] = $"Добро пожаловать! Вы успешно присоединились к компании {invitationCheck.CompanyName}.";
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "Регистрация успешно завершена! Теперь вы можете войти в систему.";
                            return RedirectToAction("Login", "Account");
                        }
                    }
                    else
                    {
                        // Существующий пользователь - авторизуем через ваш сервис
                        await _authenticationService.AuthenticateWithCookiesAsync(existingUser);

                        TempData["SuccessMessage"] = $"С возвращением! Вы успешно присоединились к компании {invitationCheck.CompanyName}.";
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to accept invitation: {Message}", result.Message);

                    return View("Accept", new AcceptInvitationViewModel
                    {
                        Code = code,
                        Email = invitationCheck.Email,
                        Phone = invitationCheck.Phone,
                        DepartmentId = invitationCheck.DepartmentId,
                        DepartmentName = invitationCheck.DepartmentName,
                        CompanyName = invitationCheck.CompanyName,
                        Position = invitationCheck.Position,
                        Errors = new List<string> { result.Message }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing invitation acceptance for code: {Code}", code);
                return View("Error", new InvitationErrorViewModel
                {
                    Error = "Произошла ошибка при регистрации. Пожалуйста, попробуйте позже.",
                    Code = code
                });
            }
        }

        [HttpGet("{code}/check")]
        public async Task<IActionResult> Check(string code)
        {
            try
            {
                // CheckInvitationAsync НЕ меняет статус
                var result = await _invitationService.CheckInvitationAsync(code);

                return Ok(new
                {
                    valid = result.Valid,
                    error = result.Error,
                    email = result.Email,
                    departmentName = result.DepartmentName,                    
                    expiresAt = result.ExpiresAt,
                    daysLeft = result.DaysLeft,
                    
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking invitation: {Code}", code);
                return StatusCode(500, new { valid = false, error = "Ошибка при проверке" });
            }
        }
        [HttpPost("{code}/accept")]
        public async Task<IActionResult> AcceptInvitation(string code)
        {
            try
            {
                // Получаем текущего пользователя
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Пользователь не авторизован" });
                }

                _logger.LogInformation("User {UserId} accepting invitation with code: {Code}", userId, code);

                // Проверяем существование приглашения
                var invitation = await _invitationService.CheckInvitationAsync(code);

                if (invitation == null)
                {
                    return BadRequest(new { message = "Приглашение не найдено" });
                }

                // Проверяем статус приглашения
                if (invitation.Status != InvitationStatus.Pending)
                {
                    return BadRequest(new { message = GetStatusErrorMessage(invitation.Status) });
                }

                // Проверяем срок действия
                if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
                {
                    await _invitationService.UpdateStatusAsync(code, InvitationStatus.Expired);
                    return BadRequest(new { message = "Срок действия приглашения истек" });
                }

                // Получаем информацию о текущем пользователе
                var currentUser = _userRepository.GetUserById(int.Parse(userId));

                // Проверяем, совпадает ли email пользователя с email в приглашении
                if (!string.IsNullOrEmpty(invitation.Email) &&
                    !string.Equals(currentUser.Data.DefaultEmail, invitation.Email, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Email mismatch. User email: {UserEmail}, Invitation email: {InvitationEmail}",
                        currentUser.Data.DefaultEmail, invitation.Email);

                    return BadRequest(new
                    {
                        message = $"Это приглашение предназначено для {invitation.Email}. " +
                                  $"Вы вошли как {currentUser.Data.DefaultEmail}. Пожалуйста, войдите под правильной учетной записью."
                    });
                }

                //// Проверяем, не состоит ли уже пользователь в этой компании
                //var existingMembership = await _userService.GetCompanyMembershipAsync(
                //    int.Parse(userId),
                //    invitation.CompanyId ?? 1);

                //if (existingMembership != null && existingMembership.IsActive)
                //{
                //    // Меняем статус приглашения на Accepted
                //    await _invitationService.UpdateStatusAsync(code, InvitationStatus.Accepted);

                //    return Ok(new
                //    {
                //        success = true,
                //        message = "Вы уже являетесь сотрудником этой компании",
                //        alreadyMember = true
                //    });
                //}

                //// Если пользователь уже был в компании, но неактивен - активируем
                //if (existingMembership != null && !existingMembership.IsActive)
                //{
                //    await _userService.ReactivateCompanyMembershipAsync(existingMembership.Id);

                //    // Обновляем статус приглашения
                //    await _invitationService.UpdateStatusAsync(code, InvitationStatus.Accepted);

                //    return Ok(new
                //    {
                //        success = true,
                //        message = "Ваше участие в компании восстановлено!",
                //        reactivated = true
                //    });
                //}

                // Добавляем пользователя в компанию
                var companyUser = new CompanyUser
                {
                    UserId = int.Parse(userId),
                    CompanyId = invitation.CompanyId ?? 1,
                    Role = "Employee",
                    Position = "Сотрудник",
                    JoinedDate = DateTime.UtcNow,
                    IsActive = true
                };


                var resultCompanyUser = await _companyRepository.AddAsync(companyUser);
                if (resultCompanyUser.Success)
                {
                    // Обновляем статус приглашения
                    await _invitationService.UpdateStatusAsync(code, InvitationStatus.Accepted);

                    currentUser.Data.CurrentCompanyId = invitation.CompanyId;
                    await _userRepository.AddOrUpdateAsync(currentUser.Data);

                    _logger.LogInformation("User {UserId} successfully joined company via invitation {Code}", userId, code);
                    return Ok(new
                    {
                        success = true,
                        message = "Вы успешно присоединились к компании!",
                        companyId = invitation.CompanyId,
                        departmentId = invitation.DepartmentId
                    });
                }
                else
                {
                    throw new Exception(resultCompanyUser.Errors[0].ToString());
                }
              
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting invitation with code: {Code}", code);
                return StatusCode(500, new { message = "Произошла ошибка при обработке приглашения" });
            }
        }

        [HttpPost("login")]        
        public async Task<IActionResult> Login(string email, string password, string returnUrl, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Введите email и пароль");

                var invitation = await _invitationService.CheckInvitationAsync(code);
                var model = new AcceptInvitationViewModel { Email = email, Code = code };
                return View("Login", model);
            }

            // Проверяем пароль через ваш репозиторий
            var isPasswordValid = await _userRepository.VerifyPasswordAsync(email, password);

            if (isPasswordValid)
            {
                // Получаем пользователя
                var userResult = _userRepository.GetUser(email);
                if (userResult.Success && userResult.Data != null)
                {
                    // Авторизуем пользователя
                    await _authenticationService.AuthenticateWithCookiesAsync(userResult.Data);

                    // ВАЖНО: После успешного входа сразу принимаем приглашение
                    // А не просто перенаправляем обратно
                    var acceptResult = await _invitationService.AcceptInvitationForExistingUserAsync(code, userResult.Data.Id);

                    if (acceptResult.Success)
                    {
                        TempData["SuccessMessage"] = $"Вы успешно присоединились к компании {acceptResult.CompanyName}!";
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = acceptResult.Message ?? "Не удалось принять приглашение";
                        return RedirectToAction("Index", "Home");
                    }
                }
            }

            ModelState.AddModelError(string.Empty, "Неверный email или пароль");

            var viewModel = new AcceptInvitationViewModel { Email = email, Code = code };
            return View("Login", viewModel);
        }

        /// <summary>
        /// Получить понятное сообщение об ошибке для каждого статуса приглашения
        /// </summary>
        private string GetStatusErrorMessage(string status)
        {
            return status switch
            {
                InvitationStatus.Accepted => "Это приглашение уже было принято. Если вы считаете, что произошла ошибка, обратитесь к администратору.",

                InvitationStatus.Expired => "Срок действия приглашения истек. Пожалуйста, запросите новое приглашение у администратора компании.",

                InvitationStatus.Declined => "Это приглашение было отклонено. Вы не можете использовать его повторно.",

                InvitationStatus.Revoked => "Это приглашение было отозвано администратором. Пожалуйста, запросите новое приглашение.",

                InvitationStatus.Pending => null, // Для Pending нет ошибки

                _ => "Приглашение недоступно. Пожалуйста, проверьте код или запросите новое приглашение."
            };
        }
    }
}
