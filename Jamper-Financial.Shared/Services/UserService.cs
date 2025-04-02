using Jamper_Financial.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.Sqlite;

namespace Jamper_Financial.Shared.Services
{
    public class UserService : IUserService
    {
        private readonly string _connectionString;

        public UserService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> DeleteUserProfileAsync(int userId)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Start a transaction to ensure atomicity
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Delete the user's profile from the Profile table
                            string deleteProfileQuery = "DELETE FROM Profile WHERE UserId = @UserId;";
                            using (var deleteProfileCommand = new SqliteCommand(deleteProfileQuery, connection, transaction))
                            {
                                deleteProfileCommand.Parameters.AddWithValue("@UserId", userId);
                                int profileRowsAffected = await deleteProfileCommand.ExecuteNonQueryAsync();

                                // If no rows were affected, the profile didn't exist
                                if (profileRowsAffected == 0)
                                {
                                    return false;
                                }
                            }

                            // Optionally, delete the user from the Users table
                            string deleteUserQuery = "DELETE FROM Users WHERE UserId = @UserId;";
                            using (var deleteUserCommand = new SqliteCommand(deleteUserQuery, connection, transaction))
                            {
                                deleteUserCommand.Parameters.AddWithValue("@UserId", userId);
                                await deleteUserCommand.ExecuteNonQueryAsync();
                            }

                            // Commit the transaction if both deletions succeed
                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // Rollback the transaction in case of an error
                            transaction.Rollback();
                            Console.WriteLine($"Error deleting user profile: {ex.Message}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteUserProfileAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT UserId, Username, Email, Password FROM Users WHERE UserId = @UserId;";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new User
                                {
                                    UserId = reader.GetInt32(0),
                                    Username = reader.GetString(1),
                                    Email = reader.GetString(2),
                                    Password = reader.IsDBNull(3) ? null : reader.GetString(3)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserByIdAsync: {ex.Message}");
            }
            return null;
        }

        public async Task<UserProfile?> GetUserProfileByIdAsync(int userId)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT p.ProfileId, p.UserId, p.FirstName, p.LastName, u.Password, u.Email, p.PhoneNumber, p.EmailConfirmed, p.PhoneNumberConfirmed, u.Username, p.Avatar
                        FROM Profile p
                        JOIN Users u ON p.UserId = u.UserId
                        WHERE p.UserId = @UserId;
                    ";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new UserProfile
                                {
                                    ProfileId = reader.GetInt32(reader.GetOrdinal("ProfileId")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? null : reader.GetString(reader.GetOrdinal("FirstName")),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? null : reader.GetString(reader.GetOrdinal("LastName")),
                                    Password = reader.IsDBNull(reader.GetOrdinal("Password")) ? null : reader.GetString(reader.GetOrdinal("Password")),
                                    Email = reader.GetString(reader.GetOrdinal("Email")),
                                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                    EmailConfirmed = reader.IsDBNull(reader.GetOrdinal("EmailConfirmed")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("EmailConfirmed")),
                                    PhoneNumberConfirmed = reader.IsDBNull(reader.GetOrdinal("PhoneNumberConfirmed")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("PhoneNumberConfirmed")),
                                    Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? null : reader.GetString(reader.GetOrdinal("Username")),
                                    ProfilePicture = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader.GetFieldValue<byte[]>(reader.GetOrdinal("Avatar"))
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserProfileByIdAsync: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> UpdateUserProfileAsync(UserProfile userProfile)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Start a transaction to ensure atomicity
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Update the Profile table
                            string updateProfileQuery = @"
                                UPDATE Profile
                                SET FirstName = @FirstName,
                                    LastName = @LastName
                                WHERE UserId = @UserId;
                            ";

                            using (var updateProfileCommand = new SqliteCommand(updateProfileQuery, connection, transaction))
                            {
                                updateProfileCommand.Parameters.AddWithValue("@FirstName", userProfile.FirstName);
                                updateProfileCommand.Parameters.AddWithValue("@LastName", userProfile.LastName);
                                updateProfileCommand.Parameters.AddWithValue("@UserId", userProfile.UserId);

                                await updateProfileCommand.ExecuteNonQueryAsync();
                            }

                            // Update the Users table
                            string updateUserQuery = @"
                                UPDATE Users
                                SET Email = @Email
                                WHERE UserId = @UserId;
                            ";

                            using (var updateUserCommand = new SqliteCommand(updateUserQuery, connection, transaction))
                            {
                                updateUserCommand.Parameters.AddWithValue("@Email", userProfile.Email);
                                updateUserCommand.Parameters.AddWithValue("@UserId", userProfile.UserId);

                                await updateUserCommand.ExecuteNonQueryAsync();
                            }

                            // Commit the transaction if both updates succeed
                            transaction.Commit();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            // Rollback the transaction in case of an error
                            transaction.Rollback();
                            Console.WriteLine($"Error updating user profile: {ex.Message}");
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateUserProfileAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAvatarAsync(UserProfile userProfile, byte[] avatar)
        {
            try
            {
                userProfile.ProfilePicture = avatar;

                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                        UPDATE Profile
                        SET Avatar = @Avatar
                        WHERE UserId = @UserId;
                    ";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Avatar", userProfile.ProfilePicture);
                        command.Parameters.AddWithValue("@UserId", userProfile.UserId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateUserAvatarAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<UserSettings?> GetUserSettingsByIdAsync(int userId)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT UserId, Currency, Timezone, EnableSubscriptionNotifications, EnableGoalsNotifications,EnableBudgetNotifications FROM UserSettings WHERE UserId = @UserId;";
                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new UserSettings
                                {
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    Currency = reader.IsDBNull(reader.GetOrdinal("Currency")) ? null : reader.GetString(reader.GetOrdinal("Currency")),
                                    TimeZone = reader.IsDBNull(reader.GetOrdinal("Timezone")) ? null : reader.GetString(reader.GetOrdinal("Timezone")),
                                    EnableSubscriptionNotification = reader.GetInt32(reader.GetOrdinal("EnableSubscriptionNotifications")) == 1,
                                    EnableBudgetNotification = reader.GetInt32(reader.GetOrdinal("EnableBudgetNotifications")) == 1,
                                    EnableGoalsNotification = reader.GetInt32(reader.GetOrdinal("EnableGoalsNotifications")) == 1
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserSettingsByIdAsync: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> UpdateUserSettingsAsync(UserSettings userSettings)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                    UPDATE UserSettings
                    SET Currency = @Currency,
                        Timezone = @Timezone,
                        EnableSubscriptionNotifications = @EnableSubscriptionNotifications,
                        EnableGoalsNotifications = @EnableGoalsNotifications,
                        EnableBudgetNotifications = @EnableBudgetNotifications
                    WHERE UserId = @UserId;
                ";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Currency", userSettings.Currency ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Timezone", userSettings.TimeZone ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@EnableSubscriptionNotifications", userSettings.EnableSubscriptionNotification);
                        command.Parameters.AddWithValue("@EnableGoalsNotifications", userSettings.EnableGoalsNotification);
                        command.Parameters.AddWithValue("@EnableBudgetNotifications", userSettings.EnableBudgetNotification);
                        command.Parameters.AddWithValue("@UserId", userSettings.UserId);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateUserSettingsAsync: {ex.Message}");
                return false;
            }
        }
    }
}