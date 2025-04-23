using Jamper_Financial.Shared.Data;
using Jamper_Financial.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jamper_Financial.Shared.Services
{
    public class UpcomingTransactionService
    {
        private readonly UserStateService _userStateService;

        public UpcomingTransactionService(UserStateService userStateService)
        {
            _userStateService = userStateService;
        }

        public async Task<List<Transaction>> GetUpcomingTransactionsAsync(DateTime startDate, DateTime endDate)
        {
            var allTransactions = await TransactionHelper.GetTransactionsAsync(_userStateService.UserId);
            var upcomingTransactions = new List<Transaction>();

            foreach (var transaction in allTransactions)
            {
                if (transaction.Date >= startDate && transaction.Date <= endDate)
                {
                    upcomingTransactions.Add(transaction);
                }
            }

            return upcomingTransactions;
        }

        public async Task<int> GetUnpaidTransactionCountAsync(DateTime startDate, DateTime endDate)
        {
            var transactions = await GetUpcomingTransactionsAsync(startDate, endDate);
            return transactions.Count(t => !t.IsPaid);
        }

        public async Task UpdateTransactionAsync(Transaction transaction)
        {
            await TransactionHelper.UpdateTransactionAsync(transaction);
        }
    }
}