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
                "CategoryID" => ascending ? transactions.OrderBy(t => t.CategoryID).ToList() : transactions.OrderByDescending(t => t.CategoryID).ToList(),
                "Amount" => ascending ? transactions.OrderBy(t => t.Amount > 0 ? -t.Amount : t.Amount).ToList() : transactions.OrderByDescending(t => t.Amount > 0 ? -t.Amount : t.Amount).ToList(),
                "Description" => ascending ? transactions.OrderBy(t => t.Description).ToList() : transactions.OrderByDescending(t => t.Description).ToList(),
                _ => transactions
            };
        }
    }
}