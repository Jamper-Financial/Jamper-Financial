using Jamper_Financial.Shared.Models;

namespace Jamper_Financial.Shared.Services
{
    public class BudgetInsightsService : IBudgetInsightsService
    {
        private readonly string _connectionString;

        public BudgetInsightsService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<BudgetInsight>> GetBudgetInsightsDescAsync()
        {
            return await Task.FromResult(new List<BudgetInsight>
            {
                new BudgetInsight
                {
                    Id = 1,
                    Title = "Budget Insights",
                    Description = "Budget insights are a great way to see how your budget is doing. You can see how much you have spent, how much you have left, and how much you have saved. You can also see how much you have spent on different categories, like food, entertainment, and transportation. This can help you see where you are spending too much money, and where you can cut back"
                }
            });
        }

        public async Task<List<BudgetItem>> GetBudgetItemsAsync()
        {
            return await Task.FromResult(new List<BudgetItem>
            {
                new BudgetItem
                {
                    Category = "Savings",
                    PlannedAmount = 350.00M,
                    CurrentAmount = 31255.55M,
                },
                new BudgetItem
                {
                    Category = "Subscriptions",
                    PlannedAmount = 150.00M,
                    CurrentAmount = 4342.67M,
                },
                new BudgetItem
                {
                    Category = "Loans",
                    PlannedAmount = 500.00M,
                    CurrentAmount = 1305.91M,
                },
                new BudgetItem
                {
                    Category = "Entertainment",
                    PlannedAmount = 1100.00M,
                    CurrentAmount = 1242.51M,
                }
            });
        }
    }
}
