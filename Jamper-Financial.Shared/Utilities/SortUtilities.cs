using Jamper_Financial.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jamper_Financial.Shared.Utilities
{
    public static class SortUtilities
    {
        public static List<Transaction> ApplySort(List<Transaction> transactions, string sortBy, bool ascending, List<Category> categories, List<BankAccount> accounts)
        {
            return sortBy switch
            {
                "Date" => ascending ? transactions.OrderBy(t => t.Date).ToList() : transactions.OrderByDescending(t => t.Date).ToList(),
                "CategoryName" => ascending ? transactions.OrderBy(t => GetCategoryName(t.CategoryID, categories)).ToList() : transactions.OrderByDescending(t => GetCategoryName(t.CategoryID, categories)).ToList(),
                "Amount" => ascending ? transactions.OrderBy(t => t.Amount > 0 ? -t.Amount : t.Amount).ToList() : transactions.OrderByDescending(t => t.Amount > 0 ? -t.Amount : t.Amount).ToList(),
                "Description" => ascending ? transactions.OrderBy(t => t.Description).ToList() : transactions.OrderByDescending(t => t.Description).ToList(),
                "AccountName" => ascending ? transactions.OrderBy(t => GetAccountName(t.AccountID, accounts)).ToList() : transactions.OrderByDescending(t => GetAccountName(t.AccountID, accounts)).ToList(),
                "AccountNumber" => ascending ? transactions.OrderBy(t => GetAccountNumber(t.AccountID, accounts)).ToList() : transactions.OrderByDescending(t => GetAccountNumber(t.AccountID, accounts)).ToList(),
                _ => transactions
            };
        }

        private static string GetCategoryName(int categoryId, List<Category> categories)
        {
            var category = categories.FirstOrDefault(c => c.CategoryID == categoryId);
            return category != null ? category.Name : "Unknown Category";
        }

        private static string GetAccountName(int accountId, List<BankAccount> accounts)
        {
            var account = accounts.FirstOrDefault(a => a.AccountId == accountId);
            return account != null ? account.AccountName : "Unknown Account";
        }

        private static string GetAccountNumber(int accountId, List<BankAccount> accounts)
        {
            var account = accounts.FirstOrDefault(a => a.AccountId == accountId);
            return account != null ? account.AccountNumber : "Unknown Account Number";
        }
    }
}
