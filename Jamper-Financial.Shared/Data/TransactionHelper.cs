using Jamper_Financial.Shared.Models;
using Jamper_Financial.Shared.Utilities;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Linq.Expressions;
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

                // Note: we need to filter out subscriptions that are not yet actual expenses
                string query = @"
                    SELECT t.TransactionID, t.[Date], t.Description, t.Amount, t.CategoryID, c.Color, c.TransactionType, t.HasReceipt, t.Frequency, t.EndDate, a.AccountID, t.IsPaid
                    FROM Transactions t
                    JOIN Categories c ON t.CategoryID = c.CategoryID
                    JOIN Accounts a ON t.AccountID = a.AccountID
                    WHERE t.UserID = @UserID
                    ORDER BY t.[Date] DESC";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Get ordinals
                        int transactionIdOrdinal = reader.GetOrdinal("TransactionID");
                        int dateOrdinal = reader.GetOrdinal("Date");
                        int descriptionOrdinal = reader.GetOrdinal("Description");
                        int amountOrdinal = reader.GetOrdinal("Amount");
                        int categoryIdOrdinal = reader.GetOrdinal("CategoryID");
                        int transactionTypeOrdinal = reader.GetOrdinal("TransactionType");
                        int hasReceiptOrdinal = reader.GetOrdinal("HasReceipt");
                        int frequencyOrdinal = reader.GetOrdinal("Frequency");
                        int endDateOrdinal = reader.GetOrdinal("EndDate");
                        int accountIdOrdinal = reader.GetOrdinal("AccountID");
                        int isPaid = reader.GetOrdinal("IsPaid");

                        while (await reader.ReadAsync())
                        {
                            transactions.Add(new Transaction
                            {
                                TransactionID = reader.GetInt32(transactionIdOrdinal),
                                Date = DateTime.Parse(reader.GetString(dateOrdinal)),
                                Description = reader.GetString(descriptionOrdinal),
                                Amount = reader.GetDecimal(amountOrdinal),
                                CategoryID = reader.GetInt32(categoryIdOrdinal),
                                TransactionType = reader.GetString(transactionTypeOrdinal),
                                HasReceipt = reader.GetInt32(hasReceiptOrdinal) == 1,
                                Frequency = reader.IsDBNull(frequencyOrdinal) ? null : reader.GetString(frequencyOrdinal),
                                EndDate = reader.IsDBNull(endDateOrdinal) ? (DateTime?)null : DateTime.Parse(reader.GetString(endDateOrdinal)),
                                AccountID = reader.GetInt32(accountIdOrdinal),
                                IsPaid = reader.GetInt32(isPaid) == 1,
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
            try
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

                    string query = @"
                    INSERT INTO Transactions (UserID, Date, Description, Amount, CategoryID, TransactionType, HasReceipt, Frequency, EndDate, AccountID, IsPaid)
                    VALUES (@UserID, @Date, @Description, @Amount, @CategoryID, @TransactionType, @HasReceipt, @Frequency, @EndDate, @AccountID, @IsPaid);
                    ";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", transaction.UserID);
                        command.Parameters.AddWithValue("@Date", transaction.Date.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@Description", transaction.Description);
                        command.Parameters.AddWithValue("@Amount", transaction.Amount);
                        command.Parameters.AddWithValue("@CategoryID", transaction.CategoryID);
                        command.Parameters.AddWithValue("@TransactionType", transaction.TransactionType);
                        command.Parameters.AddWithValue("@HasReceipt", transaction.HasReceipt ? 1 : 0);
                        command.Parameters.AddWithValue("@Frequency", transaction.Frequency ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@EndDate", transaction.EndDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@AccountID", transaction.AccountID);
                        command.Parameters.AddWithValue("@IsPaid", transaction.IsPaid ? 1 : 0);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write("Error: " + e.Message);
            }

        }

        // This method updates an existing transaction in the database
        public static async Task UpdateTransactionAsync(Transaction transaction)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string query = "UPDATE Transactions SET Date = @Date, Description = @Description, Amount = @Amount, CategoryID = @CategoryID, TransactionType = @TransactionType, Frequency = @Frequency, EndDate = @EndDate, AccountId = @AccountId, IsPaid = @IsPaid WHERE TransactionID = @TransactionID";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Date", transaction.Date.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@Description", transaction.Description ?? string.Empty);
                    command.Parameters.AddWithValue("@Amount", transaction.Amount);
                    command.Parameters.AddWithValue("@CategoryID", transaction.CategoryID);
                    command.Parameters.AddWithValue("@TransactionType", transaction.TransactionType);
                    command.Parameters.AddWithValue("@Frequency", transaction.Frequency ?? string.Empty);
                    command.Parameters.AddWithValue("@EndDate", transaction.EndDate.HasValue ? transaction.EndDate.Value.ToString("yyyy-MM-dd") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AccountId", transaction.AccountID);
                    command.Parameters.AddWithValue("@TransactionID", transaction.TransactionID);
                    command.Parameters.AddWithValue("@IsPaid", transaction.IsPaid);

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

                await DeleteReceiptAsync(transactionId);

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
            }
            ;

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

        // This method deletes a receipt for the specific transaction
        public static async Task DeleteReceiptAsync(int transactionId)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            string deleteReceiptQuery = "DELETE FROM Receipts WHERE TransactionID = @TransactionID";
            using (var deleteReceiptCommand = new SqliteCommand(deleteReceiptQuery, connection))
            {
                deleteReceiptCommand.Parameters.AddWithValue("@TransactionID", transactionId);
                await deleteReceiptCommand.ExecuteNonQueryAsync();
            }

            string updateTransactionQuery = "UPDATE Transactions SET HasReceipt = 0 WHERE TransactionID = @TransactionID";
            using (var updateTransactionCommand = new SqliteCommand(updateTransactionQuery, connection))
            {
                updateTransactionCommand.Parameters.AddWithValue("@TransactionID", transactionId);
                await updateTransactionCommand.ExecuteNonQueryAsync();
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