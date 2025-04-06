using Jamper_Financial.Shared.Models;

namespace Jamper_Financial.Shared.Services
{
    public interface IAccountService
    {
        Task<bool> CreateAccount(BankAccount bankAccount);
        Task<bool> UpdateAccount(BankAccount bankAccount);
        Task<bool> DeleteAccount(int accountId);
        Task<List<BankAccount>> GetBankAccounts(int userId);
        Task<List<BankAccountType>> GetAccountTypes();
    }
}
