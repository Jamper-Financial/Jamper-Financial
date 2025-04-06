using Jamper_Financial.Shared.Models;
using Jamper_Financial.Shared.Data;
using Microsoft.Data.Sqlite;
using System.Text;

namespace Jamper_Financial.Shared.Services
{
    public class AccountService(string connectionString) : IAccountService
    {
        public async Task<bool> CreateAccount(BankAccount bankAccount)
        {
            Console.Write("Start insert to DB");
            Console.WriteLine(bankAccount.ToString());
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    await connection.OpenAsync();
                    string insertQuery = @"
                        INSERT INTO Accounts (AccountTypeID, AccountName, Balance, AccountNumber, UserID)
                        VALUES (@AccountTypeID, @AccountName, @Balance, @AccountNumber, @UserID);
                    ";
                    using (var command = new SqliteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AccountTypeID", bankAccount.AccountTypeID);
                        command.Parameters.AddWithValue("@AccountName", bankAccount.AccountName);
                        command.Parameters.AddWithValue("@Balance", bankAccount.Balance);
                        command.Parameters.AddWithValue("@AccountNumber", bankAccount.AccountNumber);
                        command.Parameters.AddWithValue("@UserID", bankAccount.UserId);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Console.Write("Error: " + e.Message);
                // Handle any errors if needed
                return false;
            }
        }

        public async Task<bool> DeleteAccount(int accountId)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    await connection.OpenAsync();
                    string deleteQuery = "DELETE FROM Accounts WHERE AccountID = @AccountID;";
                    using (var command = new SqliteCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AccountID", accountId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write("Error: " + e.Message);
                return false;
            }
        }

        public async Task<List<BankAccount>> GetBankAccounts(int userId)
        {
            try
            {
                var bankAccounts = new List<BankAccount>();
                using (var connection = DatabaseHelper.GetConnection())
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Accounts WHERE UserID = @UserID;";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bankAccounts.Add(new BankAccount
                                {
                                    AccountId = reader.GetInt32(reader.GetOrdinal("AccountID")),
                                    AccountTypeID = reader.GetInt32(reader.GetOrdinal("AccountTypeID")),
                                    AccountName = reader.GetString(reader.GetOrdinal("AccountName")),
                                    Balance = reader.GetInt32(reader.GetOrdinal("Balance")),
                                    AccountNumber = reader.GetString(reader.GetOrdinal("AccountNumber")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserID"))
                                });
                            }
                        }
                    }
                }
                return bankAccounts;
            }
            catch (Exception e)
            {
                Console.Write("Error: " + e.Message);
                return null;
            }
        }

        public async Task<bool> UpdateAccount(BankAccount bankAccount)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    await connection.OpenAsync();
                    string updateQuery = @"
                        UPDATE Accounts SET 
                            AccountTypeID = @AccountTypeID,
                            AccountName = @AccountName,
                            Balance = @Balance,
                            AccountNumber = @AccountNumber
                        WHERE AccountID = @AccountID;
                    ";
                    using (var command = new SqliteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AccountTypeID", bankAccount.AccountTypeID);
                        command.Parameters.AddWithValue("@AccountName", bankAccount.AccountName);
                        command.Parameters.AddWithValue("@Balance", bankAccount.Balance);
                        command.Parameters.AddWithValue("@AccountNumber", bankAccount.AccountNumber);
                        command.Parameters.AddWithValue("@AccountID", bankAccount.AccountId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write("Error: " + e.Message);
                return false;
            }
        }

        public async Task<List<BankAccountType>> GetAccountTypes()
        {
            try
            {
                var accountTypes = new List<BankAccountType>();
                using (var connection = DatabaseHelper.GetConnection())
                {
                    await connection.OpenAsync();
                    string query = "SELECT AccountTypeId, Description FROM AccountType;";
                    using (var command = new SqliteCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            accountTypes.Add(new BankAccountType
                            {
                                AccountTypeId = reader.GetInt32(0),
                                AccountTypeName = reader.GetString(1)
                            });
                        }
                    }
                }
                return accountTypes;
            }
            catch (Exception e)
            {
                Console.Write("Error: " + e.Message);
                throw;
            }
        }
    }
}
