using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jamper_Financial.Shared.Utilities
{
    public static class Sort
    {
        public static List<Transaction> ApplySort(List<Transaction> transactions, string sortBy, bool ascending)
        {
            return sortBy switch
            {
                "Date" => ascending ? transactions.OrderBy(t => t.Date).ToList() : transactions.OrderByDescending(t => t.Date).ToList(),
                "Category" => ascending ? transactions.OrderBy(t => t.Category).ToList() : transactions.OrderByDescending(t => t.Category).ToList(),
                "Amount" => ascending ? transactions.OrderBy(t => t.Debit > 0 ? -t.Debit : t.Credit).ToList() : transactions.OrderByDescending(t => t.Debit > 0 ? -t.Debit : t.Credit).ToList(),
                "Description" => ascending ? transactions.OrderBy(t => t.Description).ToList() : transactions.OrderByDescending(t => t.Description).ToList(),
                _ => transactions
            };
        }
    }
}