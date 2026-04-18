using Crm.Entity.ModelsCrm;
using Crm.Entity.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Crm.Application.Features.Accounts.Commands.CreateCompany
{
    public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, CreateCompanyResult>
    {
        private ICompanyRepository _companyRepository;
        private IUserRepository _userRepository;
        private readonly ILogger<CreateCompanyCommandHandler> _logger;

        public CreateCompanyCommandHandler(
            ICompanyRepository companyRepository,
            IUserRepository userRepository,
            ILogger<CreateCompanyCommandHandler> logger)
        {            
            _companyRepository = companyRepository;            
            _userRepository = userRepository;
            _logger = logger;
        }        

        public async Task<CreateCompanyResult> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 3. Создаем доменную сущность
                var company = Entity.Entities.Company.Create(
                    request.CompanyName,
                    request.OwnerId
                    );

                // 4. Сохраняем в базу
                await _companyRepository.AddAsync(company);

                _logger.LogInformation("company created successfully with ID: {CompanyId}", company.Id);


                await _companyRepository.AddAsync(new CompanyUser
                {
                    CompanyId = company.Id,
                    UserId = request.OwnerId,
                    Role = "Admin",
                    Position = "Владелец",
                    JoinedDate = DateTime.UtcNow,
                    IsActive = true
                });

                var user = _userRepository.GetUserById(request.OwnerId);
                user.Data.CurrentCompanyId = company.Id;
                user.Data.ModifiedDate = DateTime.UtcNow;
                await _userRepository.AddOrUpdateAsync(user.Data);

                // 5. Возвращаем результат
                return new CreateCompanyResult
                {
                    Succeeded = true,
                    CompanyBase = company,
                    CompanyId = company.Id.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return new CreateCompanyResult
                {
                    Succeeded = false,
                    Errors = new List<string> { "An error occurred while creating company" }
                };
            }
        }
    }
}
