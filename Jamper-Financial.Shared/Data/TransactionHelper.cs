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
        public static async Task<IEnumerable<Transaction>> GetTransactionsAsync()
        {
            var transactions = new List<Transaction>();

            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string query = "SELECT TransactionID, Date, Description, Debit, Credit, Category, Color, Frequency, EndDate, CategoryID, HasReceipt FROM Transactions";
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
                                EndDate = reader.IsDBNull(8) ? (DateTime?)null : DateTime.Parse(reader.GetString(8)),
                                CategoryID = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                                HasReceipt = reader.IsDBNull(10) ? 0 : reader.GetInt32(10)
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