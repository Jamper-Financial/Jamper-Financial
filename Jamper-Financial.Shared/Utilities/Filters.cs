using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jamper_Financial.Shared.Utilities
{
    public static class Filters
    {
        public static List<Transaction> ApplyFilters(List<Transaction> transactions, string filterCategory, double? minAmount, double? maxAmount)
        {
            return transactions
                .Where(t => (string.IsNullOrEmpty(filterCategory) || t.CategoryID.ToString().Equals(filterCategory, StringComparison.OrdinalIgnoreCase)) &&
                            (!minAmount.HasValue || (minAmount.Value >= 0 ? (t.Amount >= (decimal)minAmount || t.Amount >= (decimal)minAmount) : (t.Amount >= (decimal)minAmount || t.Amount >= (decimal)minAmount))) &&
                            (!maxAmount.HasValue || (maxAmount.Value >= 0 ? (t.Amount <= (decimal)maxAmount || t.Amount <= (decimal)maxAmount) : (t.Amount <= (decimal)maxAmount || t.Amount <= (decimal)maxAmount))))
                .ToList();
        }
    }
}