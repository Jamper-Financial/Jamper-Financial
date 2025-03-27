﻿using Jamper_Financial.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jamper_Financial.Shared.Services
{
    public interface IExpenseService
    {
        Task<List<Expense>> GetExpensesAsync(int userId);
        Task<List<Expense>> GetExpensesAsync(int userId, string period);
    }
}