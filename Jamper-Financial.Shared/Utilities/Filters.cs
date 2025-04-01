using System;
using System.Collections.Generic;
using System.Linq;

namespace Jamper_Financial.Shared.Utilities
{
    public static class Filters
    {
        public static List<Transaction> ApplyFilters(List<Transaction> transactions, string filterCategory, decimal? minAmount, decimal? maxAmount)
        {
            return transactions
                .Where(t => (string.IsNullOrEmpty(filterCategory) || t.CategoryID.ToString().Equals(filterCategory, StringComparison.OrdinalIgnoreCase)) &&
                            (!minAmount.HasValue || t.Amount >= minAmount) &&
                            (!maxAmount.HasValue || t.Amount <= maxAmount))
                .ToList();
        }
    }
}
