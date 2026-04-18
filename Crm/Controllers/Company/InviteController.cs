using Crm.Entity.ModelsCrm;
using Crm.Entity;
using Crm.Models.Company;
using Crm.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Crm.Entity.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Crm.Controllers.Company
{
    [AllowAnonymous]
    [Route("invite")]
    public class InviteController : Controller
    {
        private readonly IInvitationService _invitationService;
        private readonly IUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly ILogger<InviteController> _logger;

        public InviteController(
            IInvitationService invitationService,
            IUserRepository userRepository,
            ICompanyRepository companyRepository,
            ILogger<InviteController> logger)
        {
            _userRepository = userRepository;  
            _invitationService = invitationService;
            _companyRepository = companyRepository;
            _logger = logger;
        }

        // GET: /invite/{code}
        [HttpGet("{code}")]
        public async Task<IActionResult> Index(string code)
        {
            try
            {
                _logger.LogInformation("Checking invitation code: {Code}", code);

                if (string.IsNullOrEmpty(code))
                {
                    return View("Error", new InvitationErrorViewModel
                    {
                        Error = "Код приглашения не указан"
                    });
                }

                var invitation = await _invitationService.CheckInvitationAsync(code);

                if (!invitation.Valid)
                {
                    _logger.LogWarning("Invalid invitation code: {Code}, Error: {Error}", code, invitation.Error);

                    return View("Error", new InvitationErrorViewModel
                    {
                        Error = invitation.Error,
                        Code = code
                    });
                }

                // Возвращаем страницу регистрации
                var model = new AcceptInvitationViewModel
                {
                    Code = code,
                    Email = invitation.Email,
                    Phone = invitation.Phone,
                    DepartmentId = invitation.DepartmentId,
                    DepartmentName = invitation.DepartmentName
                };

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

                var result = await _invitationService.AcceptInvitationAsync(code, request);

                if (result.Success)
                {
                    _logger.LogInformation("Invitation accepted successfully for code: {Code}", code);

                    TempData["SuccessMessage"] = "Регистрация успешно завершена! Теперь вы можете войти в систему.";

                    // Перенаправляем на страницу входа
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    _logger.LogWarning("Failed to accept invitation: {Message}", result.Message);

                    var invitation = await _invitationService.CheckInvitationAsync(code);

                    return View("Accept", new AcceptInvitationViewModel
                    {
                        Code = code,
                        Email = invitation.Email,
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
                    throw new Exception(resultCompanyUser.LastError.Message);
                }
              
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting invitation with code: {Code}", code);
                return StatusCode(500, new { message = "Произошла ошибка при обработке приглашения" });
            }
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
