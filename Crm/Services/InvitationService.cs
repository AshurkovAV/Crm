using Crm.Models.Requests;
using Crm.Models.Responses;
using Crm.Models;
using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using Crm.Services.Email;
using Crm.Models.Compan;
using Microsoft.EntityFrameworkCore;
using Crm.Entity;
using Crm.Entity.DTO;
using Microsoft.EntityFrameworkCore.Metadata;
using Crm.Models.Results;

namespace Crm.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IInvitationRepository _invitationRepository;
        private readonly IEmailService _emailService;        
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository; 

        public InvitationService(
            IInvitationRepository invitationRepository,
            IEmailService emailService,
            IUserRepository userRepository,
            ICompanyRepository companyRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _invitationRepository = invitationRepository;               
            _userRepository = userRepository;
            _emailService = emailService;        
            _companyRepository = companyRepository; 
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<InvitationLinkResponse> GenerateInvitationLinkAsync(int? departmentId)
        {
            var code = GenerateCode();
            var invitation = new Invitation
            {
                Code = code,                
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _invitationRepository.Add(invitation);            

            var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";
            var link = $"{baseUrl}/invite/{code}";

            return new InvitationLinkResponse
            {
                Link = link,
                Code = code,
                ExpiresAt = invitation.ExpiresAt.Value
            };
        }
        public async Task<InvitationResponse> SendEmailInvitationsAsync(EmailInvitationRequest request, int userId)
        {
            var invitations = new List<Invitation>();
            var baseUrl = $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host}";

            var userResult = _companyRepository.GetCompanyIdToUserId(userId);
            if (userResult.Result.Success) {
                foreach (var email in request.Emails)
                {
                    var code = GenerateCode();
                    var link = $"{baseUrl}/invite/{code}";

                    var invitation = new Invitation
                    {
                        Code = code,
                        Email = email,
                        Status = InvitationStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        CompanyId = userResult.Result.Data
                    };

                    invitations.Add(invitation);
                    await _emailService.SendInvitationEmailAsync(email, link, request.Message);
                }

                await _invitationRepository.AddRange(invitations);

                return new InvitationResponse
                {
                    Success = true,
                    Count = invitations.Count,
                    Message = $"Отправлено {invitations.Count} приглашений"
                };

            }
            return new InvitationResponse
            {
                Success = false,
                Count = invitations.Count,
                Message = $"Ошибка добавления"
            };



        }
        public async Task<InvitationResponse> AcceptInvitationAsync(string code, AcceptInvitationRequest request)
        {
            try
            {
                var invitation = await _invitationRepository.GetInvitationToCode(code);

                if (invitation == null)
                {
                    return new InvitationResponse
                    {
                        Success = false,
                        Message = "Приглашение не найдено"
                    };
                }

                if (invitation.ExpiresAt < DateTime.UtcNow)
                {
                    invitation.Status = InvitationStatus.Expired;
                    await _invitationRepository.Update(invitation);

                    return new InvitationResponse
                    {
                        Success = false,
                        Message = "Срок действия приглашения истек"
                    };
                }

                // Создаем пользователя
                var user = new CompanyUserDto
                {
                    DisplayName = request.DisplayName,
                    Email = invitation.Email ?? request.Email,
                    Phone = invitation.Phone ?? request.Phone,                    
                    IsActive = true
                };

                //_context.CompanyUsers.Add(user);

                //invitation.Status = InvitationStatus.Accepted;
                //invitation.AcceptedAt = DateTime.UtcNow;

                //await _context.SaveChangesAsync();

                return new InvitationResponse
                {
                    Success = true,
                    Message = "Регистрация успешно завершена",
                    UserId = user.UserId
                };
            }
            catch (Exception ex)
            {                
                return new InvitationResponse
                {
                    Success = false,
                    Message = "Ошибка при регистрации: " + ex.Message
                };
            }
        }

        public async Task<AcceptInvitationResult> AcceptInvitationForExistingUserAsync(string code, int userId)
        {
            var invitation = _invitationRepository.GetInvitationToCode(code).Result;

            if (invitation == null)
            {
                return new AcceptInvitationResult
                {
                    Success = false,
                    Message = "Приглашение не найдено или уже использовано"
                };
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                return new AcceptInvitationResult
                {
                    Success = false,
                    Message = "Срок действия приглашения истёк"
                };
            }

            // Проверяем, не состоит ли уже пользователь в компании
            var existingCompanyUser = await _companyRepository.GetCompanyUserToUserId(userId, invitation.CompanyId);
                

            if (existingCompanyUser.Success)
            {
                return new AcceptInvitationResult
                {
                    Success = false,
                    Message = "Вы уже состоите в этой компании"
                };
            }

            // Добавляем пользователя в компанию
            var companyUser = new CompanyUser
            {
                CompanyId = (int)invitation.CompanyId,
                UserId = userId,
                Role = "Member",                
                JoinedDate = DateTime.UtcNow,
                IsActive = true
            };
            _companyRepository.AddAsync(companyUser);

            // Обновляем статус приглашения
            invitation.Status = "Accepted";
            invitation.AcceptedAt = DateTime.UtcNow;

            // Если у пользователя нет текущей компании - устанавливаем
            var user = _userRepository.GetUserById(userId).Data;
            if (user != null && user.CurrentCompanyId == null)
            {
                user.CurrentCompanyId = invitation.CompanyId;
                await _userRepository.AddOrUpdateAsync(user);
            }
          

            var company = _companyRepository.GetCompanyUsersAsync((int)invitation.CompanyId).Result;

            return new AcceptInvitationResult
            {
                Success = true,
                CompanyName = company?.FirstOrDefault().CompanyName,
                CompanyId = invitation.CompanyId
            };
        }
        public async Task<InvitationCheckResponse> CheckInvitationAsync(string code)
        {
            try
            {                

                if (string.IsNullOrEmpty(code))
                {
                    return new InvitationCheckResponse
                    {
                        Valid = false,
                        Error = "Код приглашения не указан"
                    };
                }

                // Ищем приглашение по коду
                var invitation = await _invitationRepository.GetInvitationToCode(code);

                if (invitation == null)
                {
                    return new InvitationCheckResponse
                    {
                        Valid = false,
                        Error = "Приглашение не найдено"
                    };
                }

                // Проверка: не просрочено ли приглашение (НЕ МЕНЯЕМ СТАТУС!)
                if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return new InvitationCheckResponse
                    {
                        Valid = false,
                        Error = "Срок действия приглашения истек",
                        ExpiresAt = invitation.ExpiresAt,                        
                    };
                }

                // Проверка статуса приглашения
                if (invitation.Status != InvitationStatus.Pending)
                {
                    return new InvitationCheckResponse
                    {
                        Valid = false,
                        Error = GetStatusErrorMessage(invitation.Status),
                        Status = invitation.Status
                    };
                }

                // Проверяем, существует ли уже пользователь с таким email (только для информации)
                User existingUser = null;
                if (!string.IsNullOrEmpty(invitation.Email))
                {
                    var userResult = _userRepository.GetUser(invitation.Email);
                    existingUser = userResult?.Data;
                }

                // Всё валидно, НЕ МЕНЯЕМ СТАТУС
                return new InvitationCheckResponse
                {
                    Valid = true,                    
                    Email = invitation.Email,
                    Phone = invitation.Phone,
                    CompanyId = invitation.CompanyId,                    
                    CreatedAt = invitation.CreatedAt,
                    ExpiresAt = invitation.ExpiresAt,                   
                    Status = invitation.Status,
                   
                };
            }
            catch (Exception ex)
            {
                
                return new InvitationCheckResponse
                {
                    Valid = false,
                    Error = "Произошла техническая ошибка при проверке приглашения"
                };
            }
        }
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
        //public async Task<InvitationCheckResponse> CheckInvitationAsync(string code)
        //{
        //    try
        //    {

        //        if (string.IsNullOrEmpty(code))
        //        {
        //            return new InvitationCheckResponse
        //            {
        //                Valid = false,
        //                Error = "Код приглашения не указан"
        //            };
        //        }

        //        // Ищем приглашение по коду
        //        var invitation = await _invitationRepository.GetInvitationToCode(code);

        //        // Проверка: существует ли приглашение
        //        if (invitation == null)
        //        {                    
        //            return new InvitationCheckResponse
        //            {
        //                Valid = false,
        //                Error = "Приглашение не найдено. Возможно, ссылка недействительна или была удалена."
        //            };
        //        }

        //        // Проверка: не просрочено ли приглашение
        //        if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
        //        {                   

        //            // Автоматически меняем статус на Expired
        //            invitation.Status = InvitationStatus.Expired;
        //            await _invitationRepository.Update(invitation);

        //            return new InvitationCheckResponse
        //            {
        //                Valid = false,
        //                Error = "Срок действия приглашения истек. Пожалуйста, запросите новое приглашение.",
        //                ExpiresAt = invitation.ExpiresAt
        //            };
        //        }

        //        // Проверка: статус приглашения
        //        switch (invitation.Status)
        //        {
        //            case InvitationStatus.Accepted:                        
        //                return new InvitationCheckResponse
        //                {
        //                    Valid = false,
        //                    Error = "Это приглашение уже было принято.",
        //                    Status = invitation.Status,
        //                    AcceptedAt = invitation.AcceptedAt
        //                };               



        //            case InvitationStatus.Expired:                       
        //                return new InvitationCheckResponse
        //                {
        //                    Valid = false,
        //                    Error = "Срок действия приглашения истек.",                          
        //                    ExpiresAt = invitation.ExpiresAt
        //                };
        //        }

        //        // Проверка: если email уже зарегистрирован в системе
        //        if (!string.IsNullOrEmpty(invitation.Email))
        //        {
        //            var existingUser = _userRepository.GetUser(invitation.Email);

        //            if (existingUser.Data != null)
        //            {

        //                // Меняем статус приглашения                        
        //                invitation.Status = InvitationStatus.Accepted;
        //                invitation.AcceptedAt = DateTime.UtcNow;
        //                await _invitationRepository.Update(invitation);

        //                return new InvitationCheckResponse
        //                {
        //                    Valid = false,
        //                    Error = "Пользователь с таким email уже зарегистрирован в системе.",
        //                    Email = invitation.Email
        //                };
        //            }
        //            else
        //            {
        //                return new InvitationCheckResponse
        //                {
        //                    Valid = true, // Разрешаем продолжить регистрацию                            
        //                    Email = invitation.Email,
        //                    Phone = invitation.Phone,
        //                    CreatedAt = invitation.CreatedAt,
        //                    ExpiresAt = invitation.ExpiresAt,
        //                    Status = invitation.Status,                           

        //                };
        //            }
        //        }

        //        // Приглашение валидно              

        //        return new InvitationCheckResponse
        //        {
        //            Valid = true,                   
        //            Email = invitation.Email,
        //            Phone = invitation.Phone, 
        //            CreatedAt = invitation.CreatedAt,
        //            ExpiresAt = invitation.ExpiresAt,                   
        //            Status = invitation.Status,                   
        //        };
        //    }
        //    catch (Exception ex)
        //    {                
        //        return new InvitationCheckResponse
        //        {
        //            Valid = false,
        //            Error = "Произошла техническая ошибка при проверке приглашения. Пожалуйста, попробуйте позже."
        //        };
        //    }
        //}

        public async Task<bool> UpdateStatusAsync(string code, string newStatus)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {                 
                    return false;
                }

                var invitation = await _invitationRepository.GetInvitationToCode(code);                    

                if (invitation == null)
                {                    
                    return false;
                }

                var oldStatus = invitation.Status;
                invitation.Status = newStatus;

                // Устанавливаем временные метки в зависимости от статуса
                SetStatusTimestamp(invitation, newStatus);

                await _invitationRepository.Update(invitation);

                return true;
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine("Database error updating invitation status: {Code}", code);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating invitation status: {Code}", code);
                return false;
            }
        }
        private void SetStatusTimestamp(Invitation invitation, string newStatus)
        {
            switch (newStatus)
            {
                case InvitationStatus.Accepted:
                    invitation.AcceptedAt = DateTime.UtcNow;
                    break;
                case InvitationStatus.Declined:
                    invitation.DeclinedAt = DateTime.UtcNow;
                    break;
                case InvitationStatus.Expired:
                    // Для Expired может не быть специального поля, 
                    // или используем ExpiresAt для проверки
                    break;
                case InvitationStatus.Revoked:
                    // Можно добавить поле RevokedAt при необходимости
                    break;
            }
        }
        //public async Task<InvitationStatsResponse> GetStatsAsync()
        //{
        //    var total = await _context.Invitations.CountAsync();
        //    var pending = await _context.Invitations.CountAsync(i => i.Status == InvitationStatus.Pending);
        //    var accepted = await _context.Invitations.CountAsync(i => i.Status == InvitationStatus.Accepted);
        //    var expired = await _context.Invitations.CountAsync(i => i.Status == InvitationStatus.Expired);

        //    return new InvitationStatsResponse
        //    {
        //        TotalSent = total,
        //        Pending = pending,
        //        Accepted = accepted,
        //        Expired = expired,
        //        AcceptanceRate = total > 0 ? (double)accepted / total * 100 : 0
        //    };
        //}

        private string GenerateCode()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Replace("=", "")
                .Substring(0, 16);
        }
    }
}