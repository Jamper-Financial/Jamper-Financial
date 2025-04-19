using Jamper_Financial.Shared.Data;
using Jamper_Financial.Shared.Models;
using Microsoft.Data.Sqlite;

namespace Jamper_Financial.Shared.Services
{
    public class UserRepository : IUserRepository
    {
        public async Task<User?> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT UserId, Username, Email, Password, IsGoogleSignin
                    FROM Users
                    WHERE Username = @UsernameOrEmail OR Email = @UsernameOrEmail;
                ";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UsernameOrEmail", usernameOrEmail);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                Username = reader.GetString(reader.GetOrdinal("Username")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Password = reader["Password"] as string ?? string.Empty,
                                IsGoogleSignIn = reader.GetInt32(reader.GetOrdinal("IsGoogleSignin"))
                            };
                        }
                    }
                }
            }

            return null;
        }

        public async Task CreateSessionAsync(Session session)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO Sessions (UserID, Token, CreatedAt, ExpiresAt)
                    VALUES (@UserID, @Token, @CreatedAt, @ExpiresAt);
                ";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", session.UserId);
                    command.Parameters.AddWithValue("@Token", session.Token);
                    command.Parameters.AddWithValue("@CreatedAt", session.CreatedAt);
                    command.Parameters.AddWithValue("@ExpiresAt", session.ExpiresAt);

                    Console.WriteLine($"Inserting session for UserID: {session.UserId}, Token: {session.Token}");
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
