using System;
using System.Collections.Generic;
using System.Linq;

namespace Jamper_Financial.Shared.Utilities
{
    public static class Filters
    {
        public static List<Transaction> ApplyCategoryFilter(List<Transaction> transactions, string filterCategory)
        {
            return transactions
                .Where(t => string.IsNullOrEmpty(filterCategory) || t.CategoryID.ToString().Equals(filterCategory, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static List<Transaction> ApplyAmountFilter(List<Transaction> transactions, decimal? minAmount, decimal? maxAmount)
        {
            return transactions
                .Where(t => (!minAmount.HasValue || t.Amount >= minAmount) &&
                            (!maxAmount.HasValue || t.Amount <= maxAmount))
                .ToList();
        }

        public static List<Transaction> ApplyDateFilter(List<Transaction> transactions, DateTime? firstDate, DateTime? secondDate)
        {
            return transactions
                .Where(t => (!firstDate.HasValue || t.Date >= firstDate) &&
                            (!secondDate.HasValue || t.Date <= secondDate))
                .ToList();
        }

        public static List<Transaction> ApplyReceiptFilter(List<Transaction> transactions, bool? hasReceipt)
        {
            return transactions
                .Where(t => !hasReceipt.HasValue || (hasReceipt.Value ? t.HasReceipt : !t.HasReceipt || t.HasReceipt == null))
                .ToList();
        }

        public static List<Transaction> ApplyFrequencyFilter(List<Transaction> transactions, string frequency)
        {
            return transactions
                .Where(t => string.IsNullOrEmpty(frequency) || t.Frequency == frequency)
                .ToList();
        }

        public static List<Transaction> ApplyEndDateFilter(List<Transaction> transactions, bool? hasEndDate)
        {
            return transactions
                .Where(t => !hasEndDate.HasValue || (hasEndDate.Value ? t.EndDate.HasValue : !t.EndDate.HasValue || t.EndDate == null))
                .ToList();
        }

        public static List<Transaction> ApplyIsPaidFilter(List<Transaction> transactions, bool? isPaid)
        {
            return transactions
                .Where(t => !isPaid.HasValue || t.IsPaid == isPaid)
                .ToList();
        }

        public static List<Transaction> ApplyAccountFilter(List<Transaction> transactions, int? accountId)
        {
            return transactions
                .Where(t => !accountId.HasValue || t.AccountID == accountId)
                .ToList();
        }

        public static List<Transaction> ApplyFilters(List<Transaction> transactions, string filterCategory, decimal? minAmount, decimal? maxAmount, DateTime? firstDate, DateTime? secondDate, bool? hasReceipt, string frequency, bool? hasEndDate, bool? isPaid, int? accountId)
        {
            var filteredTransactions = ApplyCategoryFilter(transactions, filterCategory);
            filteredTransactions = ApplyAmountFilter(filteredTransactions, minAmount, maxAmount);
            filteredTransactions = ApplyDateFilter(filteredTransactions, firstDate, secondDate);
            filteredTransactions = ApplyReceiptFilter(filteredTransactions, hasReceipt);
            filteredTransactions = ApplyFrequencyFilter(filteredTransactions, frequency);
            filteredTransactions = ApplyEndDateFilter(filteredTransactions, hasEndDate);
            filteredTransactions = ApplyIsPaidFilter(filteredTransactions, isPaid);
            filteredTransactions = ApplyAccountFilter(filteredTransactions, accountId);

            return filteredTransactions;
        }
    }
}
