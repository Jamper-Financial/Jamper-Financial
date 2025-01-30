using Jamper_Financial.Shared.Data;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace Jamper_Financial.Shared.Services
{
    public static class AuthenticationService
    {
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

        public static void CreateUserAccount(string firstName, string lastName, string username, DateTime birthDate, string email, string password)
        {
            string hashedPassword = HashPassword(password);
            DatabaseHelper.InsertUser(firstName, lastName, username, birthDate.ToString("yyyy-MM-dd"), email, hashedPassword);

            int userId = DatabaseHelper.GetUserIdByUsername(username);
            int adminRoleId = DatabaseHelper.GetRoleIdByName("Admin");
            DatabaseHelper.AssignRoleToUser(userId, adminRoleId);
        }
    }
}
