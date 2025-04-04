using Jamper_Financial.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jamper_Financial.Shared.Utilities
{
    public static class FilterUtilities
    {
        public static List<Transaction> ApplyFilters(List<Transaction> transactions, Filter filter)
        {
            var filteredTransactions = transactions.AsQueryable();

            if (!string.IsNullOrEmpty(filter.Category))
            {
                filteredTransactions = filteredTransactions.Where(t => t.CategoryID.ToString() == filter.Category);
            }

            if (filter.MinAmount.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.Amount >= filter.MinAmount.Value);
            }

            if (filter.MaxAmount.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.Amount <= filter.MaxAmount.Value);
            }

            if (filter.DateFrom.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.Date >= filter.DateFrom.Value);
            }

            if (filter.DateTo.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.Date <= filter.DateTo.Value);
            }

            if (filter.AccountId.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.AccountID == filter.AccountId.Value);
            }

            if (!string.IsNullOrEmpty(filter.Frequency))
            {
                filteredTransactions = filteredTransactions.Where(t => t.Frequency == filter.Frequency);
            }

            if (filter.EndDateFrom.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.EndDate >= filter.EndDateFrom.Value);
            }

            if (filter.EndDateTo.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.EndDate <= filter.EndDateTo.Value);
            }

            if (filter.HasReceipt.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.HasReceipt == filter.HasReceipt.Value);
            }

            if (filter.IsPaid.HasValue)
            {
                filteredTransactions = filteredTransactions.Where(t => t.IsPaid == filter.IsPaid.Value);
            }

            return filteredTransactions.ToList();
        }
    }
}
