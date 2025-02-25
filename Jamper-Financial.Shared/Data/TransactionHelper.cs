using Jamper_Financial.Shared.Utilities;
using Microsoft.Data.Sqlite;

namespace Jamper_Financial.Shared.Data
{
    public static class TransactionHelper
    {
        private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AppDatabase.db");

        // This method retrieves all transactions from the database
        public static async Task<IEnumerable<Transaction>> GetTransactionsAsync()
        {
            var transactions = new List<Transaction>();

            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string query = "SELECT * FROM Transactions";
                using (var command = new SqliteCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            transactions.Add(new Transaction
                            {
                                TransactionID = reader.GetInt32(0),
                                Date = DateTime.Parse(reader.GetString(1)),
                                Description = reader.GetString(2),
                                Debit = reader.GetDecimal(3),
                                Credit = reader.GetDecimal(4),
                                Category = reader.GetString(5),
                                Color = reader.GetString(6),
                                Frequency = reader.IsDBNull(7) ? null : reader.GetString(7),
                                EndDate = reader.IsDBNull(8) ? (DateTime?)null : DateTime.Parse(reader.GetString(8))
                            });
                        }
                    }
                }
            }

            return transactions;
        }

        // This method inserts a new transaction into the database
        public static async Task AddTransactionAsync(Transaction transaction)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string query = "INSERT INTO Transactions (Date, Description, Debit, Credit, Category, Color, Frequency, EndDate) VALUES (@Date, @Description, @Debit, @Credit, @Category, @Color, @Frequency, @EndDate)";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Date", transaction.Date.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@Description", transaction.Description ?? string.Empty);
                    command.Parameters.AddWithValue("@Debit", transaction.Debit);
                    command.Parameters.AddWithValue("@Credit", transaction.Credit);
                    command.Parameters.AddWithValue("@Category", transaction.Category ?? string.Empty);
                    command.Parameters.AddWithValue("@Color", transaction.Color ?? string.Empty);
                    command.Parameters.AddWithValue("@Frequency", transaction.Frequency ?? string.Empty);
                    command.Parameters.AddWithValue("@EndDate", transaction.EndDate.HasValue ? transaction.EndDate.Value.ToString("yyyy-MM-dd") : (object)DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // This method updates an existing transaction in the database
        public static async Task UpdateTransactionAsync(Transaction transaction)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string query = "UPDATE Transactions SET Date = @Date, Description = @Description, Debit = @Debit, Credit = @Credit, Category = @Category, Color = @Color, Frequency = @Frequency, EndDate = @EndDate WHERE TransactionID = @TransactionID";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Date", transaction.Date.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@Description", transaction.Description ?? string.Empty);
                    command.Parameters.AddWithValue("@Debit", transaction.Debit);
                    command.Parameters.AddWithValue("@Credit", transaction.Credit);
                    command.Parameters.AddWithValue("@Category", transaction.Category ?? string.Empty);
                    command.Parameters.AddWithValue("@Color", transaction.Color ?? string.Empty);
                    command.Parameters.AddWithValue("@Frequency", transaction.Frequency ?? string.Empty);
                    command.Parameters.AddWithValue("@EndDate", transaction.EndDate.HasValue ? transaction.EndDate.Value.ToString("yyyy-MM-dd") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@TransactionID", transaction.TransactionID);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // This method deletes a transaction from the database
        public static async Task DeleteTransactionAsync(int transactionId)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string query = "DELETE FROM Transactions WHERE TransactionID = @TransactionID";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TransactionID", transactionId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // This method is used to get the connection to the database
        private static SqliteConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={DbPath}");
        }
    }
}