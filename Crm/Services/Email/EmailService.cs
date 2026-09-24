using Crm.Core.Features.Email.Models;
using Crm.Services.Notifications;
using System.Net.Mail;
using System.Net;

namespace Crm.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly string _baseUrl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _baseUrl = configuration["BaseUrl"] ?? "https://crm.biglv.ru";
        }

        public async Task SendTaskNotificationEmailAsync(TaskNotificationContext context)
        {
            if (string.IsNullOrWhiteSpace(context.AssigneeEmail))
                return;

            var isNew = context.Kind == TaskNotificationKind.Created;
            var currentYear = DateTime.Now.Year;
            var tasksUrl = $"{_baseUrl}/Tasks";

            var headingText = isNew ? "Новая задача" : "Задача изменена";
            var introText = isNew
                ? $"Вам назначена новая задача от {context.AuthorName ?? "коллеги"}."
                : $"Задача, которую вам поручили, была изменена{(context.AuthorName != null ? $" ({context.AuthorName})" : "")}.";

            var priorityColor = context.Priority switch
            {
                "Высокий" => "#c62828",
                "Средний" => "#b7791f",
                "Низкий" => "#2e7d32",
                _ => "#4a5568"
            };

            var detailsRows = "";
            if (!string.IsNullOrWhiteSpace(context.ProjectName))
                detailsRows += $@"<tr><td class='detail-label'>Проект</td><td class='detail-value'>{System.Net.WebUtility.HtmlEncode(context.ProjectName)}</td></tr>";
            if (context.Deadline.HasValue)
                detailsRows += $@"<tr><td class='detail-label'>Срок</td><td class='detail-value'>{context.Deadline.Value:dd.MM.yyyy HH:mm}</td></tr>";
            if (!string.IsNullOrWhiteSpace(context.Priority))
                detailsRows += $@"<tr><td class='detail-label'>Приоритет</td><td class='detail-value'><span style='color:{priorityColor}; font-weight:600;'>{System.Net.WebUtility.HtmlEncode(context.Priority)}</span></td></tr>";
            if (!string.IsNullOrWhiteSpace(context.AuthorName))
                detailsRows += $@"<tr><td class='detail-label'>Постановщик</td><td class='detail-value'>{System.Net.WebUtility.HtmlEncode(context.AuthorName)}</td></tr>";

            // Description — это уже санитайзенный HTML из HtmlEditor (см. TaskDataController),
            // поэтому вставляем как есть (не HtmlEncode), иначе письмо покажет сырые теги
            // вместо форматирования. Относительные ссылки на картинки (/uploads/tasks/...)
            // превращаем в абсолютные — почтовый клиент не знает, с какого домена их брать.
            var descriptionHtml = context.Description;
            if (!string.IsNullOrWhiteSpace(descriptionHtml))
                descriptionHtml = descriptionHtml.Replace("src=\"/uploads/", $"src=\"{_baseUrl}/uploads/");

            var descriptionBlock = string.IsNullOrWhiteSpace(descriptionHtml)
                ? ""
                : $@"<div class='description-box'><div class='description-label'>Описание</div>{descriptionHtml}</div>";

            var changedFieldsBlock = "";
            if (!isNew && context.ChangedFields.Count > 0)
            {
                var items = string.Join("", context.ChangedFields.Select(f => $"<li>{System.Net.WebUtility.HtmlEncode(f)}</li>"));
                changedFieldsBlock = $@"<div class='changed-box'><div class='changed-label'>Что изменилось</div><ul>{items}</ul></div>";
            }

            var emailBody = $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}

        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            line-height: 1.6;
            color: #1a1a2e;
            background-color: #f5f7fb;
        }}

        .email-wrapper {{
            max-width: 600px;
            margin: 0 auto;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px 20px;
        }}

        .email-container {{
            background: #ffffff;
            border-radius: 24px;
            box-shadow: 0 20px 40px rgba(0, 0, 0, 0.15), 0 5px 15px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}

        .email-header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px;
            text-align: center;
            color: white;
        }}

        .logo-icon {{
            width: 72px;
            height: 72px;
            background: rgba(255, 255, 255, 0.2);
            border-radius: 18px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            margin-bottom: 20px;
            border: 1px solid rgba(255, 255, 255, 0.3);
        }}

        .logo-icon svg {{ width: 40px; height: 40px; color: white; }}

        .email-header h1 {{ font-size: 28px; font-weight: 700; margin: 0 0 8px; }}
        .email-header p {{ font-size: 16px; opacity: 0.95; margin: 0; }}

        .email-body {{ padding: 40px; background: #ffffff; }}

        .message-box {{
            background: linear-gradient(135deg, #f8f9ff 0%, #f0f2ff 100%);
            border-left: 4px solid #667eea;
            padding: 20px 24px;
            border-radius: 16px;
            margin-bottom: 28px;
        }}

        .message-box p {{ margin: 0; font-size: 16px; color: #2d3748; }}

        .task-title {{
            font-size: 22px;
            font-weight: 700;
            color: #1a1a2e;
            margin-bottom: 20px;
        }}

        .details-table {{ width: 100%; border-collapse: collapse; margin-bottom: 24px; }}
        .detail-label {{
            padding: 10px 0;
            color: #718096;
            font-size: 14px;
            width: 130px;
            vertical-align: top;
            border-bottom: 1px solid #edf2f7;
        }}
        .detail-value {{
            padding: 10px 0;
            color: #2d3748;
            font-size: 15px;
            font-weight: 500;
            border-bottom: 1px solid #edf2f7;
        }}

        .description-box {{
            background: #f7fafc;
            border-radius: 12px;
            padding: 16px 20px;
            margin-bottom: 24px;
            border: 1px solid #e2e8f0;
        }}
        .description-label, .changed-label {{
            font-size: 12px;
            color: #718096;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
        }}
        .description-box p {{ color: #2d3748; font-size: 15px; white-space: pre-wrap; }}

        .changed-box {{
            background: #fef5e7;
            border-radius: 12px;
            padding: 16px 20px;
            margin-bottom: 24px;
        }}
        .changed-box ul {{ margin: 0; padding-left: 20px; color: #7c5e10; font-size: 14px; }}

        .cta-button {{
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white !important;
            text-decoration: none;
            padding: 14px 36px;
            border-radius: 50px;
            font-weight: 600;
            font-size: 16px;
            box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
        }}

        .cta-wrap {{ text-align: center; margin-top: 8px; }}

        .email-footer {{
            background: #f7fafc;
            padding: 28px 40px;
            text-align: center;
            border-top: 1px solid #e2e8f0;
        }}
        .copyright {{ color: #a0aec0; font-size: 13px; }}

        @@media (max-width: 600px) {{
            .email-wrapper {{ padding: 20px 10px; }}
            .email-header {{ padding: 30px 24px; }}
            .email-body {{ padding: 28px 24px; }}
        }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-container'>
            <div class='email-header'>
                <div class='logo-icon'>
                    <svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
                        <path d='M9 11l3 3L22 4'/>
                        <path d='M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11'/>
                    </svg>
                </div>
                <h1>{headingText}</h1>
                <p>{context.AssigneeName}, у вас есть обновление по задаче</p>
            </div>

            <div class='email-body'>
                <div class='message-box'>
                    <p>{introText}</p>
                </div>

                <div class='task-title'>{System.Net.WebUtility.HtmlEncode(context.TaskName)}</div>

                <table class='details-table'>{detailsRows}</table>

                {descriptionBlock}
                {changedFieldsBlock}

                <div class='cta-wrap'>
                    <a href='{tasksUrl}' class='cta-button'>Открыть задачи</a>
                </div>
            </div>

            <div class='email-footer'>
                <div class='copyright'>
                    © {currentYear} Crm.System.<br>
                    Это автоматическое письмо, пожалуйста, не отвечайте на него.
                </div>
            </div>
        </div>
    </div>
</body>
</html>";

            var mail = new Mail
            {
                EmailFrom = "ashurkovav@yandex.ru",
                EmailTo = context.AssigneeEmail,
                EmailSubject = isNew
                    ? $"📋 Новая задача: {context.TaskName}"
                    : $"✏️ Задача изменена: {context.TaskName}",
                EmailBody = emailBody
            };

            await SendEmailAsync(mail);
        }

        public async Task SendInvitationEmailAsync(string email, string link, string customMessage = null, string companyName = "CRM System")
        {
            var message = string.IsNullOrEmpty(customMessage)
                ? "Вас приглашают присоединиться к команде в корпоративной CRM системе."
                : customMessage;

            var currentYear = DateTime.Now.Year;

            var emailBody = $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
        
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            line-height: 1.6;
            color: #1a1a2e;
            background-color: #f5f7fb;
            margin: 0;
            padding: 0;
        }}
        
        .email-wrapper {{
            max-width: 600px;
            margin: 0 auto;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 40px 20px;
        }}
        
        .email-container {{
            background: #ffffff;
            border-radius: 24px;
            box-shadow: 0 20px 40px rgba(0, 0, 0, 0.15), 0 5px 15px rgba(0, 0, 0, 0.1);
            overflow: hidden;
        }}
        
        .email-header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 48px 40px 40px;
            text-align: center;
            color: white;
            position: relative;
            overflow: hidden;
        }}
        
        .email-header::before {{
            content: '';
            position: absolute;
            top: -50%;
            right: -50%;
            width: 200%;
            height: 200%;
            background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 70%);
            animation: pulse 4s ease-in-out infinite;
        }}
        
        @keyframes pulse {{
            0%, 100% {{ transform: scale(1); opacity: 0.5; }}
            50% {{ transform: scale(1.1); opacity: 0.3; }}
        }}
        
        .logo-icon {{
            width: 80px;
            height: 80px;
            background: rgba(255, 255, 255, 0.2);
            border-radius: 20px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            margin-bottom: 24px;
            position: relative;
            z-index: 1;
            backdrop-filter: blur(10px);
            border: 1px solid rgba(255, 255, 255, 0.3);
        }}
        
        .logo-icon svg {{
            width: 48px;
            height: 48px;
            color: white;
        }}
        
        .email-header h1 {{
            font-size: 32px;
            font-weight: 700;
            margin: 0 0 12px;
            position: relative;
            z-index: 1;
            text-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }}
        
        .email-header p {{
            font-size: 18px;
            opacity: 0.95;
            margin: 0;
            position: relative;
            z-index: 1;
        }}
        
        .email-body {{
            padding: 48px 40px;
            background: #ffffff;
        }}
        
        .message-box {{
            background: linear-gradient(135deg, #f8f9ff 0%, #f0f2ff 100%);
            border-left: 4px solid #667eea;
            padding: 24px;
            border-radius: 16px;
            margin-bottom: 32px;
        }}
        
        .message-box p {{
            margin: 0;
            font-size: 18px;
            color: #2d3748;
            line-height: 1.6;
        }}
        
        .invitation-details {{
            text-align: center;
            margin-bottom: 32px;
        }}
        
        .invitation-icon {{
            width: 64px;
            height: 64px;
            background: linear-gradient(135deg, #f8f9ff 0%, #f0f2ff 100%);
            border-radius: 16px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            margin-bottom: 20px;
        }}
        
        .invitation-icon svg {{
            width: 32px;
            height: 32px;
            color: #667eea;
        }}
        
        .invitation-details h2 {{
            font-size: 24px;
            font-weight: 600;
            color: #1a1a2e;
            margin: 0 0 8px;
        }}
        
        .invitation-details .subtitle {{
            color: #718096;
            font-size: 16px;
            margin: 0 0 24px;
        }}
        
        .cta-button {{
            display: inline-block;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white !important;
            text-decoration: none;
            padding: 16px 40px;
            border-radius: 50px;
            font-weight: 600;
            font-size: 18px;
            text-align: center;
            box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
            transition: all 0.3s ease;
            border: none;
            margin: 16px 0;
            position: relative;
            overflow: hidden;
        }}
        
        .cta-button:hover {{
            transform: translateY(-2px);
            box-shadow: 0 12px 30px rgba(102, 126, 234, 0.5);
        }}
        
        .cta-button::before {{
            content: '';
            position: absolute;
            top: 50%;
            left: 50%;
            width: 0;
            height: 0;
            border-radius: 50%;
            background: rgba(255, 255, 255, 0.3);
            transform: translate(-50%, -50%);
            transition: width 0.6s, height 0.6s;
        }}
        
        .cta-button:hover::before {{
            width: 300px;
            height: 300px;
        }}
        
        .link-section {{
            background: #f7fafc;
            border-radius: 12px;
            padding: 20px;
            margin-top: 24px;
            border: 1px solid #e2e8f0;
        }}
        
        .link-label {{
            font-size: 14px;
            color: #718096;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
        }}
        
        .link-url {{
            font-family: 'Courier New', monospace;
            font-size: 14px;
            color: #4a5568;
            word-break: break-all;
            background: white;
            padding: 12px;
            border-radius: 8px;
            border: 1px solid #e2e8f0;
        }}
        
        .expiry-info {{
            display: inline-block;
            background: #fef5e7;
            color: #b7791f;
            padding: 8px 16px;
            border-radius: 50px;
            font-size: 14px;
            margin-top: 24px;
        }}
        
        .expiry-info i {{
            margin-right: 6px;
        }}
        
        .features-grid {{
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 16px;
            margin: 40px 0 24px;
        }}
        
        .feature-item {{
            text-align: center;
        }}
        
        .feature-icon {{
            width: 48px;
            height: 48px;
            background: linear-gradient(135deg, #f8f9ff 0%, #f0f2ff 100%);
            border-radius: 12px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            margin-bottom: 12px;
        }}
        
        .feature-icon svg {{
            width: 24px;
            height: 24px;
            color: #667eea;
        }}
        
        .feature-item h4 {{
            font-size: 16px;
            font-weight: 600;
            color: #1a1a2e;
            margin: 0 0 4px;
        }}
        
        .feature-item p {{
            font-size: 13px;
            color: #718096;
            margin: 0;
        }}
        
        .email-footer {{
            background: #f7fafc;
            padding: 32px 40px;
            text-align: center;
            border-top: 1px solid #e2e8f0;
        }}
        
        .company-info {{
            margin-bottom: 20px;
        }}
        
        .company-name {{
            font-weight: 600;
            color: #1a1a2e;
            font-size: 18px;
            margin-bottom: 8px;
        }}
        
        .social-links {{
            display: flex;
            justify-content: center;
            gap: 16px;
            margin: 20px 0;
        }}
        
        .social-link {{
            width: 40px;
            height: 40px;
            background: white;
            border-radius: 50%;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            color: #718096;
            text-decoration: none;
            transition: all 0.3s ease;
            border: 1px solid #e2e8f0;
        }}
        
        .social-link:hover {{
            background: #667eea;
            color: white;
            border-color: #667eea;
            transform: translateY(-2px);
        }}
        
        .copyright {{
            color: #a0aec0;
            font-size: 13px;
            margin-top: 20px;
        }}
        
        .help-text {{
            color: #718096;
            font-size: 14px;
            margin-top: 16px;
        }}
        
        .help-text a {{
            color: #667eea;
            text-decoration: none;
            font-weight: 500;
        }}
        
        .help-text a:hover {{
            text-decoration: underline;
        }}
        
        @@media (max-width: 600px) {{
            .email-wrapper {{
                padding: 20px 10px;
            }}
            
            .email-header {{
                padding: 36px 24px 30px;
            }}
            
            .email-header h1 {{
                font-size: 26px;
            }}
            
            .email-body {{
                padding: 32px 24px;
            }}
            
            .features-grid {{
                grid-template-columns: 1fr;
                gap: 20px;
            }}
            
            .logo-icon {{
                width: 60px;
                height: 60px;
            }}
            
            .logo-icon svg {{
                width: 36px;
                height: 36px;
            }}
            
            .cta-button {{
                padding: 14px 28px;
                font-size: 16px;
            }}
            
            .message-box {{
                padding: 18px;
            }}
            
            .message-box p {{
                font-size: 16px;
            }}
        }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='email-container'>
            <div class='email-header'>
                <div class='logo-icon'>
                    <svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
                        <path d='M12 2L2 7v10l10 5 10-5V7l-10-5z'/>
                        <path d='M2 7l10 5 10-5'/>
                        <path d='M12 22V12'/>
                    </svg>
                </div>
                <h1>{companyName}</h1>
                <p>Вас пригласили присоединиться к команде</p>
            </div>
            
            <div class='email-body'>
                <div class='message-box'>
                    <p>{message}</p>
                </div>
                
                <div class='invitation-details'>
                    <div class='invitation-icon'>
                        <svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'>
                            <path d='M22 12h-4l-3 9L9 3l-3 9H2'/>
                        </svg>
                    </div>
                    <h2>Примите приглашение</h2>
                    <p class='subtitle'>Начните работу с командой уже сегодня</p>
                    
                    <a href='{link}' class='cta-button'>
                        Принять приглашение
                    </a>
                </div>
                
                <div class='features-grid'>
                    <div class='feature-item'>
                        <div class='feature-icon'>
                            <svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'>
                                <path d='M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2'/>
                                <circle cx='9' cy='7' r='4'/>
                                <path d='M23 21v-2a4 4 0 0 0-3-3.87'/>
                                <path d='M16 3.13a4 4 0 0 1 0 7.75'/>
                            </svg>
                        </div>
                        <h4>Команда</h4>
                        <p>Работайте вместе</p>
                    </div>
                    
                    <div class='feature-item'>
                        <div class='feature-icon'>
                            <svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'>
                                <path d='M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.127.96.362 1.903.7 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.907.339 1.85.574 2.81.7A2 2 0 0 1 22 16.92z'/>
                            </svg>
                        </div>
                        <h4>Коммуникация</h4>
                        <p>Общайтесь в чатах</p>
                    </div>
                    
                    <div class='feature-item'>
                        <div class='feature-icon'>
                            <svg viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2'>
                                <rect x='2' y='3' width='20' height='14' rx='2' ry='2'/>
                                <line x1='8' y1='21' x2='16' y2='21'/>
                                <line x1='12' y1='17' x2='12' y2='21'/>
                            </svg>
                        </div>
                        <h4>Проекты</h4>
                        <p>Управляйте задачами</p>
                    </div>
                </div>
                
                <div class='link-section'>
                    <div class='link-label'>Или скопируйте ссылку:</div>
                    <div class='link-url'>{link}</div>
                </div>
                
                <div class='expiry-info'>
                    <svg viewBox='0 0 24 24' width='14' height='14' fill='currentColor' style='margin-right: 4px;'>
                        <circle cx='12' cy='12' r='10'/>
                        <polyline points='12 6 12 12 16 14'/>
                    </svg>
                    Ссылка действительна 7 дней
                </div>
            </div>
            
            <div class='email-footer'>
                <div class='company-info'>
                    <div class='company-name'>{companyName}</div>
                    <p style='color: #718096; margin: 8px 0;'>Корпоративная CRM система</p>
                </div>
                
                <div class='social-links'>
                    <a href='#' class='social-link'>
                        <svg width='18' height='18' viewBox='0 0 24 24' fill='currentColor'>
                            <path d='M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z'/>
                        </svg>
                    </a>
                    <a href='#' class='social-link'>
                        <svg width='18' height='18' viewBox='0 0 24 24' fill='currentColor'>
                            <path d='M23 3a10.9 10.9 0 0 1-3.14 1.53 4.48 4.48 0 0 0-7.86 3v1A10.66 10.66 0 0 1 3 4s-4 9 5 13a11.64 11.64 0 0 1-7 2c9 5 20 0 20-11.5a4.5 4.5 0 0 0-.08-.83A7.72 7.72 0 0 0 23 3z'/>
                        </svg>
                    </a>
                    <a href='#' class='social-link'>
                        <svg width='18' height='18' viewBox='0 0 24 24' fill='currentColor'>
                            <path d='M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z'/>
                            <rect x='2' y='9' width='4' height='12'/>
                            <circle cx='4' cy='4' r='2'/>
                        </svg>
                    </a>
                </div>
                
                <div class='help-text'>
                    <p>Есть вопросы? <a href='mailto:support@{companyName.ToLower().Replace(" ", "")}.ru'>Напишите в поддержку</a></p>
                </div>
                
                <div class='copyright'>
                    © {currentYear} {companyName}. Все права защищены.<br>
                    Это автоматическое письмо, пожалуйста, не отвечайте на него.
                </div>
            </div>
        </div>
    </div>
</body>
</html>";

            var mail = new Mail
            {
                EmailFrom = "ashurkovav@yandex.ru",
                EmailTo = email,
                EmailSubject = $"🎉 Приглашение в {companyName} - присоединяйтесь к команде!",
                EmailBody = emailBody
            };

            await SendEmailAsync(mail);
        }



        //private async Task SendEmailAsync(string to, string subject, string body)
        //{
        //    // Реализация отправки email (MailKit, SMTP и т.д.)
        //    _logger.LogInformation($"Email sent to {to}: {subject}");
        //    await Task.CompletedTask;
        //}

        private async Task<bool> SendEmailAsync(Mail mail)
        {
            try
            {
                var from = new MailAddress(mail.EmailFrom, "Crm.System");
                var to = new MailAddress(mail.EmailTo, "client");

                using var message = new MailMessage(from, to)
                {
                    Subject = mail.EmailSubject,
                    Body = mail.EmailBody,
                    IsBodyHtml = true
                };

                using var smtpClient = new SmtpClient("smtp.yandex.ru", 587)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(from.Address, "kfoavuebmrejiwzr")
                };

                await smtpClient.SendMailAsync(message);

                _logger.LogInformation("Email отправлен успешно на {EmailTo}", mail.EmailTo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке email на {EmailTo}", mail.EmailTo);
                return false;
            }
        }
    }
}
