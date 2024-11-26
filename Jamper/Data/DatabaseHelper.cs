using Microsoft.Data.Sqlite;
using System.IO;

namespace Jamper.Data
{

    public static class DatabaseHelper
    {
        private static readonly string DbPath = Path.Combine(Directory.GetCurrentDirectory(), "AppDatabase.db");

        //This method is used to initialize the database
        public static void InitializeDatabase()
        {
            // Create the database file if it doesn't exist
            if (!File.Exists(DbPath))
            {
                using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                {
                    connection.Open();

                    string createTableQuery = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                            FirstName TEXT NOT NULL,
                            LastName TEXT NOT NULL,
                            Username TEXT NOT NULL UNIQUE,
                            Birthday TEXT NOT NULL,
                            Email TEXT NOT NULL UNIQUE,
                            Password TEXT NOT NULL
                        );
                    ";

                    using (var command = new SqliteCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        //This method is used to insert a new user into the database
        public static void InsertUser(string firstName, string lastName, string username, string birthday, string email, string password)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string insertQuery = @"
                INSERT INTO Users (FirstName, LastName, Username, Birthday, Email, Password)
                VALUES (@FirstName, @LastName, @Username, @Birthday, @Email, @Password);
            ";

                using (var command = new SqliteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@FirstName", firstName);
                    command.Parameters.AddWithValue("@LastName", lastName);
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Birthday", birthday);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);

                    command.ExecuteNonQuery();
                }
            }
        }

        //This method is used to check if a username is already taken
        public static bool IsUsernameTaken(string username)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username;";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }

        //This method is used to check if an email is already taken
        public static bool IsEmailTaken(string email)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM Users WHERE Email = @Email;";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }
        //This method is used to validate the user credentials with the database
        //It returns true if the user credentials are valid, otherwise false
        public static bool ValidateUserCredentials(string identifier, string password)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string query = @"
            SELECT COUNT(1)
            FROM Users
            WHERE (Username = @Identifier OR Email = @Identifier) AND Password = @Password;
        ";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Identifier", identifier);
                    command.Parameters.AddWithValue("@Password", password);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }

        //This method is used to get the connection to the database
        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection($"Data Source={DbPath}");
        }

        // This method checks if a user exists in the database
        public static bool IsUserExists(string username, string email)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = @"
            SELECT COUNT(1) 
            FROM Users 
            WHERE (Username = @Username OR Email = @Email);
        ";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Email", email);
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
        }


        // This method deletes a user from the database
        public static bool DeleteUser(string username, string email)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string deleteQuery = "DELETE FROM Users WHERE Username = @Username OR Email = @Email;";

                using (var command = new SqliteCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Email", email);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
        }

    }
}
