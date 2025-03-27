using Jamper_Financial.Shared.Data;
using Jamper_Financial.Shared.Models;
using Microsoft.Data.Sqlite;
using Microsoft.JSInterop;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;
using static Jamper_Financial.Shared.Pages.LoginPage;
using Jamper_Financial.Shared.Services;

namespace Jamper_Financial.Shared.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly NavigationManager _navigationManager;
        private readonly UserStateService _userStateService;

        //public AuthenticationService(NavigationManager navigationManager, UserStateService userStateService)
        //{
        //    _navigationManager = navigationManager;
        //    _userStateService = userStateService;
        //}

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            string hashedInputPassword = HashPassword(password);
            return hashedInputPassword == hashedPassword;
        }


        public static bool ValidateUserCredentials(string identifier, string password)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT Password
                    FROM Users
                    WHERE Username = @Identifier OR Email = @Identifier;
                ";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Identifier", identifier);

                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        string storedPassword = result.ToString();
                        return VerifyPassword(password, storedPassword);
                    }
                    return false;
                }
            }
        }

        public void CreateUserAccount(User user, UserProfile userProfile)
        {
            string hashedPassword = HashPassword(userProfile.Password);
            user.Password = hashedPassword;
            DatabaseHelper.InsertUser(user);

            int userId = DatabaseHelper.GetUserIdByUsername(userProfile.Username);
            DatabaseHelper.InsertProfile(userId, userProfile.FirstName, userProfile.LastName);
            int adminRoleId = DatabaseHelper.GetRoleIdByName("Admin");
            DatabaseHelper.AssignRoleToUser(userId, adminRoleId);
        }

    }
}

