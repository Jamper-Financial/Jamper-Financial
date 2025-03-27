﻿using Jamper_Financial.Shared.Models;
using Microsoft.Data.Sqlite;

namespace Jamper_Financial.Shared.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly string _connectionString;
        public ExpenseService(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<List<Expense>> GetExpensesAsync(int userId)
        {
            // Default to "monthly" period if no period is specified
            return await GetExpensesAsync(userId, "monthly");
        }

        public async Task<List<Expense>> GetExpensesAsync(int userId, string period)
        {
            var expenses = new List<Expense>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                string query = @"
                    SELECT 
                        SUM(CASE WHEN e.TransactionType = 'e' THEN e.Amount ELSE 0 END) AS ExpenseAmount, 
                        SUM(CASE WHEN e.TransactionType = 'i' THEN e.Amount ELSE 0 END) AS SalaryAmount,
                        e.Date
                    FROM Transactions e
                    JOIN Categories c ON e.CategoryID = c.CategoryID
                    WHERE e.UserID = @UserID
                    AND e.Date >= @StartDate
                    AND e.Date <= @EndDate
                    GROUP BY e.Date
                    ORDER BY e.Date DESC;
                ";

                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@UserID", userId);

                DateTime startDate;
                DateTime endDate = DateTime.Now;

                if (period == "weekly")
                {
                    // Get the start of the week (Sunday)
                    startDate = endDate.AddDays(-(int)endDate.DayOfWeek);
                }
                else if (period == "monthly")
                {
                    // Get the start of the month
                    startDate = new DateTime(endDate.Year, endDate.Month, 1);
                }
                else
                {
                    throw new ArgumentException("Invalid period specified. Use 'weekly' or 'monthly'.");
                }

                // Ensure startDate and endDate are valid
                if (startDate > endDate)
                {
                    throw new ArgumentException("Start date cannot be after end date.");
                }

                command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));

                Console.WriteLine($"Query: {query}");
                Console.WriteLine($"Start of the week: {startDate.ToString("yyyy-MM-dd")}");
                Console.WriteLine($"End of the week: {endDate.ToString("yyyy-MM-dd")}");

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var expense = new Expense
                    {
                        ExpenseAmount = reader.GetDecimal(0),
                        SalaryAmount = reader.GetDecimal(1),
                        Date = DateOnly.FromDateTime(reader.GetDateTime(2)),
                    };

                    expenses.Add(expense);
                }
            }
            catch (Exception ex)
            {
                // Log the exception (if you have a logging mechanism)
                Console.WriteLine($"Error in GetExpensesAsync: {ex.Message}");
                return new List<Expense>();
            }
            return expenses;
        }
    }
}