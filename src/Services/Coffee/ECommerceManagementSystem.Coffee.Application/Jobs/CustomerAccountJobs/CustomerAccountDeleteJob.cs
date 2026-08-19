using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Jobs.CustomerAccountJobs;

public class CustomerAccountDeleteJob
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;

    public CustomerAccountDeleteJob(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        try
        {
            var customerAccountsEmailVerifyPending = await _unitOfWork
                .GetRepository<CustomerAccounts>()
                .GetListAsync(
                    predicate: x =>
                        x.Account.Status == EAccountStatus.EmailVerifyPending
                        && x.Account.LastOtpSentAt.HasValue
                        && x.Account.LastOtpSentAt.Value.AddMinutes(15) <= now,
                    include: x => x.Include(x => x.Account)
                        .Include(x => x.Customer)
                );
            if (!customerAccountsEmailVerifyPending.Any()) return;
            foreach (var customerAccount in customerAccountsEmailVerifyPending)
            {
                _unitOfWork.GetRepository<CustomerAccounts>().DeleteAsync(customerAccount);

                if (customerAccount.Account != null)
                {
                    _unitOfWork.GetRepository<Accounts>().DeleteAsync(customerAccount.Account);
                }
                if (customerAccount.Customer != null)
                {
                    _unitOfWork.GetRepository<Customers>().DeleteAsync(customerAccount.Customer);
                }
            }
            await _unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}