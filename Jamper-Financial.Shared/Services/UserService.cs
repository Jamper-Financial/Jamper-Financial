﻿using Jamper_Financial.Shared.Models;
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

        public async Task<User> GetUserByIdAsync(int userId)
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
                                Password = reader.GetString(3)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<UserProfile?> GetUserProfileByIdAsync(int userId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT p.ProfileId, p.UserId, p.FirstName, p.LastName, p.Birthday, p.Address, p.City, p.PostalCode, p.Country, u.Password, u.Email, p.PhoneNumber, p.EmailConfirmed, p.PhoneNumberConfirmed, u.Username
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
                                ProfileId = reader.GetInt32(0),
                                UserId = reader.GetInt32(1),
                                FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                LastName = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Birthday = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                                Address = reader.IsDBNull(5) ? null : reader.GetString(5),
                                City = reader.IsDBNull(6) ? null : reader.GetString(6),
                                PostalCode = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Country = reader.IsDBNull(8) ? null : reader.GetString(8),
                                Password = reader.GetString(9),
                                Email = reader.GetString(10),
                                PhoneNumber = reader.IsDBNull(11) ? null : reader.GetString(11),
                                EmailConfirmed = reader.IsDBNull(12) ? (bool?)null : reader.GetBoolean(12),
                                PhoneNumberConfirmed = reader.IsDBNull(13) ? (bool?)null : reader.GetBoolean(13),
                                Username = reader.IsDBNull(14) ? null : reader.GetString(14)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> UpdateUserProfileAsync(UserProfile userProfile)
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
                        LastName = @LastName,
                        Birthday = @Birthday,
                        Address = @Address,
                        City = @City,
                        PostalCode = @PostalCode,
                        Country = @Country
                    WHERE UserId = @UserId;
                ";

                        using (var updateProfileCommand = new SqliteCommand(updateProfileQuery, connection, transaction))
                        {
                            updateProfileCommand.Parameters.AddWithValue("@FirstName", userProfile.FirstName);
                            updateProfileCommand.Parameters.AddWithValue("@LastName", userProfile.LastName);
                            updateProfileCommand.Parameters.AddWithValue("@Birthday", (object)userProfile.Birthday ?? DBNull.Value);
                            updateProfileCommand.Parameters.AddWithValue("@Address", (object)userProfile.Address ?? DBNull.Value);
                            updateProfileCommand.Parameters.AddWithValue("@City", (object)userProfile.City ?? DBNull.Value);
                            updateProfileCommand.Parameters.AddWithValue("@PostalCode", (object)userProfile.PostalCode ?? DBNull.Value);
                            updateProfileCommand.Parameters.AddWithValue("@Country", (object)userProfile.Country ?? DBNull.Value);
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

    }
}