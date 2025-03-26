using Jamper_Financial.Shared.Models;
using Jamper_Financial.Shared.Utilities;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Reflection.Metadata;

namespace Jamper_Financial.Shared.Data
{
    public static class TransactionHelper
    {
        private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AppDatabase.db");

        // This method retrieves all transactions from the database
        public static async Task<IEnumerable<Transaction>> GetTransactionsAsync(int userId)
        {
            var transactions = new List<Transaction>();

            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT t.TransactionID, t.Date, t.Description, t.Amount, t.Debit, t.Credit, t.CategoryID, c.Color, c.TransactionType, t.HasReceipt, t.Frequency, t.EndDate
                    FROM Transactions t
                    JOIN Categories c ON t.CategoryID = c.CategoryID
                    WHERE t.UserID = @UserID
                    ORDER BY t.Date DESC";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            transactions.Add(new Transaction
                            {
                                TransactionID = reader.GetInt32(0),
                                Date = DateTime.Parse(reader.GetString(1)),
                                Description = reader.GetString(2),
                                Amount = reader.GetDecimal(3), // Fetch Amount
                                Debit = reader.GetDecimal(4),
                                Credit = reader.GetDecimal(5),
                                CategoryID = reader.GetInt32(6),
                                TransactionType = reader.GetString(8),
                                HasReceipt = reader.GetInt32(9) == 1,
                                Frequency = reader.GetString(10),
                                EndDate = reader.IsDBNull(11) ? (DateTime?)null : DateTime.Parse(reader.GetString(11))
                            });
                        }
                    }
                }
            }
            Console.WriteLine(transactions.Count);
            return transactions;
        }



        // This method inserts a new transaction into the database
        public static async Task AddTransactionAsync(Transaction transaction)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();


                string categoryQuery = "SELECT TransactionType FROM Categories WHERE CategoryID = @CategoryID AND UserID = @UserID;";
                using (var categoryCommand = new SqliteCommand(categoryQuery, connection))
                {
                    categoryCommand.Parameters.AddWithValue("@CategoryID", transaction.CategoryID);
                    categoryCommand.Parameters.AddWithValue("@UserID", transaction.UserID);
                    var result = await categoryCommand.ExecuteScalarAsync();
                    transaction.TransactionType = result?.ToString() ?? "e"; // Default to expense
                }

                
                if (transaction.TransactionType == "e")
                {
                    transaction.Debit = transaction.Amount;
                    transaction.Credit = 0;
                }
                else
                {
                    transaction.Debit = 0;
                    transaction.Credit = transaction.Amount;
                }

                string query = @"
                    INSERT INTO Transactions (UserID, Date, Description, Amount, Debit, Credit, CategoryID, TransactionType, HasReceipt, Frequency, EndDate)
                    VALUES (@UserID, @Date, @Description, @Amount, @Debit, @Credit, @CategoryID, @TransactionType, @HasReceipt, @Frequency, @EndDate);
                    ";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", transaction.UserID);
                    command.Parameters.AddWithValue("@Date", transaction.Date.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@Description", transaction.Description);
                    command.Parameters.AddWithValue("@Amount", transaction.Amount);
                    command.Parameters.AddWithValue("@Debit", transaction.Debit);
                    command.Parameters.AddWithValue("@Credit", transaction.Credit);
                    command.Parameters.AddWithValue("@CategoryID", transaction.CategoryID);
                    command.Parameters.AddWithValue("@TransactionType", transaction.TransactionType);
                    command.Parameters.AddWithValue("@HasReceipt", transaction.HasReceipt ? 1 : 0);
                    command.Parameters.AddWithValue("@Frequency", transaction.Frequency);
                    command.Parameters.AddWithValue("@EndDate", transaction.EndDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
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

                string query = "UPDATE Transactions SET Date = @Date, Description = @Description, Amount = @Amount, Debit = @Debit, Credit = @Credit, CategoryID = @CategoryID, TransactionType = @TransactionType, Frequency = @Frequency, EndDate = @EndDate WHERE TransactionID = @TransactionID";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Date", transaction.Date.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@Description", transaction.Description ?? string.Empty);
                    command.Parameters.AddWithValue("@Amount", transaction.Amount);
                    command.Parameters.AddWithValue("@Debit", transaction.Debit);
                    command.Parameters.AddWithValue("@Credit", transaction.Credit);
                    command.Parameters.AddWithValue("@CategoryID", transaction.CategoryID);
                    command.Parameters.AddWithValue("@TransactionType", transaction.TransactionType);
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

        // This method adds a receipt for the specific transaction
        public static async Task AddOrUpdateReceiptAsync(ReceiptData receiptData)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            string query = "UPDATE Transactions SET HasReceipt = @HasReceipt WHERE TransactionID = @TransactionID";
            using (var command = new SqliteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@HasReceipt", 1);
                command.Parameters.AddWithValue("@TransactionID", receiptData.TransactionID);
                await command.ExecuteNonQueryAsync();
            };

            //insert the receipt file into the database
            string insertquery = @"
                    INSERT INTO Receipts (TransactionID, ReceiptData, Description)
                    VALUES (@TransactionID, @ReceiptData, @Description)
                ";

            using (var command = new SqliteCommand(insertquery, connection))
            {
                command.Parameters.AddWithValue("@TransactionID", receiptData.TransactionID);
                command.Parameters.Add("@ReceiptData", SqliteType.Blob).Value = receiptData.ReceiptFileData;
                command.Parameters.AddWithValue("@Description", receiptData.ReceiptDescription ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }

        }
        // This method is used to get the connection to the database

        public static async Task<ReceiptData> GetReceiptAsync(int transactionId)
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();
                string query = @"
            SELECT ReceiptID, Description, ReceiptData, TransactionID 
            FROM Receipts 
            WHERE TransactionID = @TransactionID
        ";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TransactionID", transactionId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int receiptIdOrdinal = reader.GetOrdinal("ReceiptID");
                            int descriptionOrdinal = reader.GetOrdinal("Description");
                            int receiptDataOrdinal = reader.GetOrdinal("ReceiptData");
                            int transactionIdOrdinal = reader.GetOrdinal("TransactionID");

                            return new ReceiptData
                            {
                                ReceiptID = reader.GetInt32(receiptIdOrdinal),
                                ReceiptDescription = reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal),
                                ReceiptFileData = (byte[])reader[receiptDataOrdinal],
                                TransactionID = reader.GetInt32(transactionIdOrdinal)
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            return null;
        }

        private static SqliteConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={DbPath}");
        }
    }
}