using Jamper_Financial.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jamper_Financial.Shared.Services
{
    public interface IBudgetInsightsService
    {
        Task<List<BudgetInsight>> GetBudgetInsightsDescAsync();
        Task<List<BudgetItem>> GetBudgetItemsAsync();
    }
}