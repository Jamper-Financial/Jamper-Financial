using Jamper_Financial.Shared.Data;
using Jamper_Financial.Shared.Models;
using Microsoft.Data.Sqlite;

namespace Jamper_Financial.Shared.Services
{
    public class SessionRepository : ISessionRepository
    {
        public async Task<Session?> GetSessionByTokenAsync(string token)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    await connection.OpenAsync();

                    Console.WriteLine($"GetSessionByTokenAsync: Querying token {token}");

                    string query = "SELECT Id, UserID, Token, CreatedAt, ExpiresAt FROM Sessions WHERE Token = @Token;";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Token", token);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new Session
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    Token = reader.GetString(reader.GetOrdinal("Token")),
                                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                                    ExpiresAt = reader.GetDateTime(reader.GetOrdinal("ExpiresAt"))
                                };
                            }
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetSessionByTokenAsync: {ex.Message}");
                throw;
            }
        }

        public async Task CreateSessionAsync(Session session)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                await connection.OpenAsync();

                // Clear old sessions for the same user
                string deleteQuery = "DELETE FROM Sessions WHERE UserID = @UserId;";
                using (var deleteCommand = new SqliteCommand(deleteQuery, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@UserId", session.UserId);
                    await deleteCommand.ExecuteNonQueryAsync();
                }

                // Insert the new session
                string insertQuery = @"
            INSERT INTO Sessions (UserID, Token, CreatedAt, ExpiresAt)
            VALUES (@UserId, @Token, @CreatedAt, @ExpiresAt);
        ";
                using (var insertCommand = new SqliteCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@UserId", session.UserId);
                    insertCommand.Parameters.AddWithValue("@Token", session.Token);
                    insertCommand.Parameters.AddWithValue("@CreatedAt", session.CreatedAt);
                    insertCommand.Parameters.AddWithValue("@ExpiresAt", session.ExpiresAt);

                    Console.WriteLine($"Inserting session for UserID: {session.UserId}, Token: {session.Token}");
                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task DeleteSessionByTokenAsync(string token)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    await connection.OpenAsync();

                    string query = "DELETE FROM Sessions WHERE Token = @Token;";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Token", token);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteSessionByTokenAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidateSessionTokenAsync(string token)
        {
            try
            {
                var session = await GetSessionByTokenAsync(token);
                return session != null && session.ExpiresAt > DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ValidateSessionTokenAsync: {ex.Message}");
                throw;
            }
        }
    }
}
