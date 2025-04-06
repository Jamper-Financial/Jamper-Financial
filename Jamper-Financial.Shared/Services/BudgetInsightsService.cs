using Jamper_Financial.Shared.Models;
using Microsoft.Data.Sqlite;

namespace Jamper_Financial.Shared.Services
{
    public class BudgetInsightsService(string connectionString) : IBudgetInsightsService
    {
        private readonly string _connectionString = connectionString;

        public async Task<List<BudgetInsight>> GetBudgetInsightsDescAsync()
        {
            try
            {
                return await Task.FromResult(new List<BudgetInsight>
                {
                    new() {
                        Id = 1,
                        Title = "Budget Insights",
                        Description = "Budget insights are a great way to see how your budget is doing. You can see how much you have spent, how much you have left, and how much you have saved. You can also see how much you have spent on different categories, like food, entertainment, and transportation. This can help you see where you are spending too much money, and where you can cut back"
                    }
                });
            }
            catch (Exception ex)
            {
                // Log the exception (if you have a logging mechanism)
                Console.WriteLine($"Error in GetBudgetInsightsDescAsync: {ex.Message}");
                return [];
            }
        }

        public async Task<List<BudgetItem>> GetBudgetItemsAsync(int userId)
        {
            var budgetItems = new List<BudgetItem>();

            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

            string query = @"
                SELECT c.Name AS Category, 
                       IFNULL(SUM(CASE WHEN t.TransactionType = 'i' THEN t.Amount ELSE 0 END) - 
                              SUM(CASE WHEN t.TransactionType = 'e' THEN t.Amount ELSE 0 END), 0) AS CurrentAmount, 
                       IFNULL(b.PlannedAmount, 0) AS PlannedAmount,
                       c.TransactionType
                FROM Categories c
                LEFT JOIN Transactions t ON c.CategoryID = t.CategoryID
                LEFT JOIN Budget b ON c.CategoryID = b.CategoryID AND b.UserID = @UserID
                WHERE c.UserID = @UserID
                GROUP BY c.CategoryID
                ORDER BY c.Name;
            ";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@UserID", userId);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    budgetItems.Add(new BudgetItem
                    {
                        Category = reader.GetString(0),
                        CurrentAmount = reader.GetDecimal(1),
                        PlannedAmount = reader.GetDecimal(2),
                        TransactionType = reader.GetInt32(3)
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception (if you have a logging mechanism)
                Console.WriteLine($"Error in GetBudgetItemsAsync: {ex.Message}");
            }

            return budgetItems;
        }


        public async Task UpdatePlannedAmountAsync(int userId, string category, decimal plannedAmount)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Get the CategoryID for the given category name and userId
                string getCategoryIDQuery = @"
                SELECT CategoryID FROM Categories WHERE Name = @Category AND UserID = @UserID;
            ";

                int categoryId;
                using (var getCategoryIDCommand = new SqliteCommand(getCategoryIDQuery, connection))
                {
                    getCategoryIDCommand.Parameters.AddWithValue("@Category", category);
                    getCategoryIDCommand.Parameters.AddWithValue("@UserID", userId);

                    var result = await getCategoryIDCommand.ExecuteScalarAsync() ?? throw new Exception("Category not found.");
                    categoryId = Convert.ToInt32(result);
                }

                // Check if a budget entry exists for the given CategoryID and UserID
                string checkBudgetExistsQuery = @"
                SELECT COUNT(*) FROM Budget WHERE UserID = @UserID AND CategoryID = @CategoryID;
            ";

                int budgetCount;
                using (var checkBudgetExistsCommand = new SqliteCommand(checkBudgetExistsQuery, connection))
                {
                    checkBudgetExistsCommand.Parameters.AddWithValue("@UserID", userId);
                    checkBudgetExistsCommand.Parameters.AddWithValue("@CategoryID", categoryId);

                    budgetCount = Convert.ToInt32(await checkBudgetExistsCommand.ExecuteScalarAsync());
                }

                if (budgetCount == 0)
                {
                    // Insert a new budget entry
                    string insertBudgetQuery = @"
                    INSERT INTO Budget (UserID, CategoryID, PlannedAmount)
                    VALUES (@UserID, @CategoryID, @PlannedAmount);
                ";

                    using var insertBudgetCommand = new SqliteCommand(insertBudgetQuery, connection);
                    insertBudgetCommand.Parameters.AddWithValue("@UserID", userId);
                    insertBudgetCommand.Parameters.AddWithValue("@CategoryID", categoryId);
                    insertBudgetCommand.Parameters.AddWithValue("@PlannedAmount", plannedAmount);

                    await insertBudgetCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // Update the existing budget entry
                    string updateBudgetQuery = @"
                    UPDATE Budget
                    SET PlannedAmount = @PlannedAmount
                    WHERE UserID = @UserID AND CategoryID = @CategoryID;
                ";

                    using var updateBudgetCommand = new SqliteCommand(updateBudgetQuery, connection);
                    updateBudgetCommand.Parameters.AddWithValue("@PlannedAmount", plannedAmount);
                    updateBudgetCommand.Parameters.AddWithValue("@UserID", userId);
                    updateBudgetCommand.Parameters.AddWithValue("@CategoryID", categoryId);

                    await updateBudgetCommand.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                // Log the exception (if you have a logging mechanism)
                Console.WriteLine($"Error in UpdatePlannedAmountAsync: {ex.Message}");
            }
        }

    }
}
