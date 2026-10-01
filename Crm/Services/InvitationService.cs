using Crm.Models.Requests;
using Crm.Models.Responses;
using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Services.Email;
using Crm.Models.Compan;
using Crm.Entity;
using Crm.Models.Results;
using System.ComponentModel.DataAnnotations;

namespace Crm.Services
{
    public class InvitationService : IInvitationService
    {
        // Роль, которую получает сотрудник, пришедший по приглашению
        private const string EmployeeRole = "Employee";
        private const int DefaultExpiryDays = 7;
        private const int MaxExpiryDays = 30;

        private readonly IInvitationRepository _invitationRepository;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvitationService> _logger;

        public InvitationService(
            IInvitationRepository invitationRepository,
            IEmailService emailService,
            IUserRepository userRepository,
            ICompanyRepository companyRepository,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<InvitationService> logger)
        {
            _invitationRepository = invitationRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _companyRepository = companyRepository;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<InvitationLinkResponse> GenerateInvitationLinkAsync(int? departmentId)
        {
            var code = GenerateCode();
            var invitation = new Invitation
            {
                Code = code,
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(DefaultExpiryDays)
            };

            await _invitationRepository.Add(invitation);

            return new InvitationLinkResponse
            {
                Link = BuildLink(code),
                Code = code,
                ExpiresAt = invitation.ExpiresAt.Value
            };
        }

        public async Task<InvitationResponse> SendEmailInvitationsAsync(EmailInvitationRequest request, int userId)
        {
            var inviter = _userRepository.GetUserById(userId).Data;
            if (inviter == null)
                return Fail("Пользователь не найден");

            var company = await ResolveInviterCompanyAsync(inviter);
            if (company == null)
                return Fail("Приглашать сотрудников может владелец или администратор компании. Выберите компанию в профиле.");

            var expiryDays = Math.Clamp(request.ExpiryDays ?? DefaultExpiryDays, 1, MaxExpiryDays);
            var message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
            if (message != null && message.Length > 500)
                message = message[..500];

            // Нормализуем адреса: без пробелов, в нижнем регистре, без повторов
            var emailValidator = new EmailAddressAttribute();
            var emails = (request.Emails ?? Array.Empty<string>())
                .Select(e => (e ?? string.Empty).Trim().ToLowerInvariant())
                .Where(e => e.Length > 0)
                .Distinct()
                .ToList();

            var invalid = emails.Where(e => e.Length > 100 || !emailValidator.IsValid(e)).ToList();
            var alreadyMembers = new List<string>();
            var invitations = new List<Invitation>();

            foreach (var email in emails.Except(invalid))
            {
                var existingUser = _userRepository.GetUser(email).Data;
                if (existingUser != null)
                {
                    var membership = await _companyRepository.GetMembershipAsync(existingUser.Id, company.Id);
                    if (membership?.IsActive == true)
                    {
                        alreadyMembers.Add(email);
                        continue;
                    }
                }

                invitations.Add(new Invitation
                {
                    Code = GenerateCode(),
                    Email = email,
                    Status = InvitationStatus.Pending,
                    InvitationType = "Email",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                    CreatedBy = userId,
                    CompanyId = company.Id,
                    Message = message
                });
            }

            if (invitations.Count == 0)
                return Fail(BuildSendSummary(0, new List<string>(), alreadyMembers, invalid, nothingToSend: true));

            // Сначала сохраняем приглашения, потом шлём письма — иначе при ошибке БД
            // человек получил бы ссылку, которая не работает
            if (!await _invitationRepository.AddRange(invitations))
                return Fail("Не удалось сохранить приглашения. Попробуйте ещё раз.");

            var inviterName = !string.IsNullOrWhiteSpace(inviter.DisplayName)
                ? inviter.DisplayName
                : string.Join(" ", new[] { inviter.FirstName, inviter.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var sent = 0;
            var failed = new List<string>();
            foreach (var invitation in invitations)
            {
                var ok = await _emailService.SendInvitationEmailAsync(
                    invitation.Email!, BuildLink(invitation.Code), message, company.Name, expiryDays, inviterName);
                if (ok)
                    sent++;
                else
                    failed.Add(invitation.Email!);
            }

            return new InvitationResponse
            {
                Success = sent > 0,
                Count = sent,
                Message = BuildSendSummary(sent, failed, alreadyMembers, invalid, nothingToSend: false)
            };
        }

        /// <summary>
        /// Регистрация нового пользователя по приглашению: создаёт пользователя с паролем
        /// и добавляет его в компанию.
        /// </summary>
        public async Task<InvitationResponse> AcceptInvitationAsync(string code, AcceptInvitationRequest request)
        {
            try
            {
                var (invitation, company, error) = await LoadPendingInvitationAsync(code);
                if (invitation == null || company == null)
                    return new InvitationResponse { Success = false, Message = error };

                var email = (invitation.Email ?? request.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(email) || !new EmailAddressAttribute().IsValid(email))
                    return new InvitationResponse { Success = false, Message = "Укажите корректный email" };

                if (_userRepository.GetUser(email).Data != null)
                    return new InvitationResponse { Success = false, Message = "Пользователь с таким email уже зарегистрирован — войдите под своим паролем" };

                var displayName = request.DisplayName.Trim();
                var nameParts = displayName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var position = string.IsNullOrWhiteSpace(request.Position) ? null : request.Position.Trim();

                var user = Crm.Entity.Entities.User.Create(email);
                user.DisplayName = displayName;
                user.RealName = displayName;
                user.FirstName = nameParts.ElementAtOrDefault(0);
                user.LastName = nameParts.ElementAtOrDefault(1);
                user.DefaultPhone = string.IsNullOrWhiteSpace(request.Phone) ? invitation.Phone : request.Phone.Trim();
                user.Position = position;
                user.IsValidation = true; // email подтверждён тем, что человек пришёл по ссылке из письма
                user.CurrentCompanyId = company.Id;

                await _userRepository.AddAsync(user);

                if (!await _userRepository.SetPasswordAsync(email, request.Password))
                {
                    _logger.LogError("Пользователь {Email} создан, но пароль не сохранился", email);
                    return new InvitationResponse { Success = false, Message = "Не удалось сохранить пароль. Попробуйте ещё раз." };
                }

                var added = await _companyRepository.AddAsync(new CompanyUser
                {
                    CompanyId = company.Id,
                    UserId = user.Id,
                    Role = EmployeeRole,
                    Position = position,
                    JoinedDate = DateTime.UtcNow,
                    IsActive = true
                });
                if (!added.Success)
                {
                    _logger.LogError("Не удалось добавить пользователя {UserId} в компанию {CompanyId}", user.Id, company.Id);
                    return new InvitationResponse { Success = false, Message = "Не удалось добавить вас в компанию. Попробуйте ещё раз." };
                }

                await MarkAcceptedAsync(invitation);

                return new InvitationResponse
                {
                    Success = true,
                    Message = $"Добро пожаловать в {company.Name}!",
                    UserId = user.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка регистрации по приглашению {Code}", code);
                return new InvitationResponse
                {
                    Success = false,
                    Message = "Ошибка при регистрации. Попробуйте позже."
                };
            }
        }

        /// <summary>
        /// Существующий пользователь принимает приглашение: добавляем в компанию
        /// (или восстанавливаем прежнее членство) и делаем её текущей.
        /// </summary>
        public async Task<AcceptInvitationResult> AcceptInvitationForExistingUserAsync(string code, int userId)
        {
            try
            {
                var (invitation, company, error) = await LoadPendingInvitationAsync(code);
                if (invitation == null || company == null)
                    return AcceptInvitationResult.Error(error!);

                var user = _userRepository.GetUserById(userId).Data;
                if (user == null)
                    return AcceptInvitationResult.Error("Пользователь не найден");

                // Приглашение по email может принять только владелец этого email
                if (!string.IsNullOrEmpty(invitation.Email) &&
                    !string.Equals(user.DefaultEmail, invitation.Email, StringComparison.OrdinalIgnoreCase))
                {
                    return AcceptInvitationResult.Error($"Это приглашение отправлено на {invitation.Email}, а вы вошли как {user.DefaultEmail}.");
                }

                var membership = await _companyRepository.GetMembershipAsync(userId, company.Id);
                var alreadyMember = membership?.IsActive == true;

                if (membership == null)
                {
                    var added = await _companyRepository.AddAsync(new CompanyUser
                    {
                        CompanyId = company.Id,
                        UserId = userId,
                        Role = EmployeeRole,
                        Position = user.Position,
                        JoinedDate = DateTime.UtcNow,
                        IsActive = true
                    });
                    if (!added.Success)
                        return AcceptInvitationResult.Error("Не удалось добавить вас в компанию. Попробуйте ещё раз.");
                }
                else if (!membership.IsActive)
                {
                    // Сотрудника раньше удаляли из компании — возвращаем прежнюю запись
                    membership.IsActive = true;
                    membership.JoinedDate = DateTime.UtcNow;
                    await _companyRepository.AddOrUpdateAsync(membership);
                }

                // Человек пришёл по приглашению — сразу открываем ему эту компанию
                if (user.CurrentCompanyId != company.Id)
                {
                    user.CurrentCompanyId = company.Id;
                    user.ModifiedDate = DateTime.UtcNow;
                    await _userRepository.AddOrUpdateAsync(user);
                }

                await MarkAcceptedAsync(invitation);

                return new AcceptInvitationResult
                {
                    Success = true,
                    AlreadyMember = alreadyMember,
                    Message = alreadyMember
                        ? $"Вы уже состоите в компании {company.Name}"
                        : $"Вы присоединились к компании {company.Name}",
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    UserId = userId,
                    Email = user.DefaultEmail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка принятия приглашения {Code} пользователем {UserId}", code, userId);
                return AcceptInvitationResult.Error("Произошла ошибка при обработке приглашения. Попробуйте позже.");
            }
        }

        public async Task<InvitationCheckResponse> CheckInvitationAsync(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    return Invalid("Код приглашения не указан");

                var invitation = await _invitationRepository.GetByCodeAsync(code);
                if (invitation == null)
                    return Invalid("Приглашение не найдено. Проверьте ссылку или попросите прислать новое приглашение.");

                if (invitation.Status != InvitationStatus.Pending)
                    return Invalid(GetStatusErrorMessage(invitation.Status), invitation.Status);

                if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
                {
                    var expired = Invalid("Срок действия приглашения истёк. Попросите администратора компании прислать новое.", InvitationStatus.Expired);
                    expired.ExpiresAt = invitation.ExpiresAt;
                    return expired;
                }

                var company = invitation.CompanyId.HasValue
                    ? await _companyRepository.GetCompanyByIdAsync(invitation.CompanyId.Value)
                    : null;
                if (company == null || company.IsActive == false)
                    return Invalid("Компания, в которую вас пригласили, недоступна.");

                string? inviterName = null;
                if (invitation.CreatedBy.HasValue)
                {
                    var inviter = _userRepository.GetUserById(invitation.CreatedBy.Value).Data;
                    inviterName = inviter?.DisplayName
                        ?? string.Join(" ", new[] { inviter?.FirstName, inviter?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                }

                return new InvitationCheckResponse
                {
                    Valid = true,
                    Email = invitation.Email,
                    Phone = invitation.Phone,
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    InviterName = string.IsNullOrWhiteSpace(inviterName) ? null : inviterName,
                    Message = invitation.Message,
                    CreatedAt = invitation.CreatedAt,
                    ExpiresAt = invitation.ExpiresAt,
                    Status = invitation.Status,
                    InvitationType = invitation.InvitationType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки приглашения {Code}", code);
                return Invalid("Произошла техническая ошибка при проверке приглашения");
            }
        }

        public async Task<bool> UpdateStatusAsync(string code, string newStatus)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                    return false;

                var invitation = await _invitationRepository.GetInvitationToCode(code);
                if (invitation == null)
                    return false;

                invitation.Status = newStatus;
                SetStatusTimestamp(invitation, newStatus);

                return await _invitationRepository.Update(invitation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления статуса приглашения {Code}", code);
                return false;
            }
        }

        // ===================== вспомогательное =====================

        /// <summary>
        /// Компания, в которую пользователь может приглашать: текущая, если он в ней владелец
        /// или администратор, иначе — его собственная компания.
        /// </summary>
        private async Task<Company?> ResolveInviterCompanyAsync(User inviter)
        {
            if (inviter.CurrentCompanyId.HasValue)
            {
                var current = await _companyRepository.GetCompanyByIdAsync(inviter.CurrentCompanyId.Value);
                if (current != null && current.IsActive != false)
                {
                    if (current.OwnerId == inviter.Id)
                        return current;

                    var membership = await _companyRepository.GetMembershipAsync(inviter.Id, current.Id);
                    if (membership?.IsActive == true && string.Equals(membership.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                        return current;
                }
            }

            var owned = await _companyRepository.GetCompanyIdToUserId(inviter.Id);
            return owned.Success ? await _companyRepository.GetCompanyByIdAsync(owned.Data) : null;
        }

        private async Task<(Invitation? Invitation, Company? Company, string? Error)> LoadPendingInvitationAsync(string code)
        {
            var invitation = await _invitationRepository.GetInvitationToCode(code);
            if (invitation == null)
                return (null, null, "Приглашение не найдено или уже использовано");

            if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await _invitationRepository.Update(invitation);
                return (null, null, "Срок действия приглашения истёк");
            }

            var company = invitation.CompanyId.HasValue
                ? await _companyRepository.GetCompanyByIdAsync(invitation.CompanyId.Value)
                : null;
            if (company == null || company.IsActive == false)
                return (null, null, "Компания, в которую вас пригласили, недоступна");

            return (invitation, company, null);
        }

        private async Task MarkAcceptedAsync(Invitation invitation)
        {
            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;
            if (!await _invitationRepository.Update(invitation))
                _logger.LogWarning("Не удалось отметить приглашение {Code} принятым", invitation.Code);
        }

        /// <summary>
        /// Адрес сайта для ссылок в письмах. Берём из настройки BaseUrl (её же использует
        /// EmailService): за nginx запрос приходит по http на внутренний адрес, и ссылка,
        /// собранная из Request, вела бы не туда.
        /// </summary>
        private string BuildLink(string code)
        {
            var baseUrl = _configuration["BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var request = _httpContextAccessor.HttpContext!.Request;
                var scheme = request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? request.Scheme;
                baseUrl = $"{scheme}://{request.Host}";
            }
            return $"{baseUrl.TrimEnd('/')}/invite/{code}";
        }

        private static string BuildSendSummary(int sent, List<string> failed, List<string> alreadyMembers, List<string> invalid, bool nothingToSend)
        {
            var parts = new List<string>();
            if (nothingToSend)
                parts.Add("Приглашения не отправлены");
            else if (sent > 0)
                parts.Add($"Отправлено приглашений: {sent}");
            if (failed.Any())
                parts.Add($"не удалось отправить письмо: {string.Join(", ", failed)}");
            if (alreadyMembers.Any())
                parts.Add($"уже в компании: {string.Join(", ", alreadyMembers)}");
            if (invalid.Any())
                parts.Add($"некорректные адреса: {string.Join(", ", invalid)}");
            if (nothingToSend && !alreadyMembers.Any() && !invalid.Any())
                parts.Add("введите хотя бы один email");
            return string.Join("; ", parts);
        }

        private static InvitationResponse Fail(string message) => new() { Success = false, Message = message };

        private static InvitationCheckResponse Invalid(string error, string? status = null) => new()
        {
            Valid = false,
            Error = error,
            Status = status
        };

        private static string GetStatusErrorMessage(string status)
        {
            return status switch
            {
                InvitationStatus.Accepted => "Это приглашение уже принято. Войдите в CRM под своей учётной записью.",
                InvitationStatus.Expired => "Срок действия приглашения истёк. Попросите администратора компании прислать новое.",
                InvitationStatus.Declined => "Это приглашение было отклонено.",
                InvitationStatus.Revoked => "Приглашение отозвано администратором компании.",
                _ => "Приглашение недоступно. Проверьте ссылку или попросите прислать новое приглашение."
            };
        }

        private static void SetStatusTimestamp(Invitation invitation, string newStatus)
        {
            switch (newStatus)
            {
                case InvitationStatus.Accepted:
                    invitation.AcceptedAt = DateTime.UtcNow;
                    break;
                case InvitationStatus.Declined:
                    invitation.DeclinedAt = DateTime.UtcNow;
                    break;
            }
        }

        private static string GenerateCode()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "")
                .Substring(0, 16);
        }
    }
}
