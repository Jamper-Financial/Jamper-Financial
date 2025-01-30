using Microsoft.Data.Sqlite;

namespace Jamper_Financial.Shared.Data
{
    public static class DatabaseHelper
    {
        private static readonly string DbPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AppDatabase.db");
        private static readonly string[] stringArray = ["LastName", "FirstName", "Birthday"];

        // This method is used to initialize the database
        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();

                // Check and create tables if they do not exist
                CreateTableIfNotExists(connection, "Users", @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        Email TEXT NOT NULL UNIQUE,
                        Password TEXT NOT NULL
                    );
                ");

                CreateTableIfNotExists(connection, "Profile", @"
                    CREATE TABLE IF NOT EXISTS Profile (
                        ProfileID INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserID INTEGER NOT NULL, 
                        FirstName TEXT NOT NULL,
                        LastName TEXT NOT NULL,
                        Birthday TEXT,
                        Address TEXT,
                        City TEXT,
                        postalCode TEXT,
                        Country TEXT,
                        PhoneNumber TEXT,
                        EmailConfirmed INTEGER,
                        PhoneNumberConfirmed INTEGER,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID)
                        );
                ");

                CreateTableIfNotExists(connection, "Roles", @"
                    CREATE TABLE IF NOT EXISTS Roles (
                        RoleId INTEGER PRIMARY KEY AUTOINCREMENT,
                        RoleName TEXT NOT NULL UNIQUE
                    );
                ");

                CreateTableIfNotExists(connection, "UserRoles", @"
                    CREATE TABLE IF NOT EXISTS UserRoles (
                        UserRoleId INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        RoleId INTEGER NOT NULL,
                        FOREIGN KEY (UserId) REFERENCES Users(UserId),
                        FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
                    );
                ");

                CreateTableIfNotExists(connection, "Transactions", @"
                    CREATE TABLE IF NOT EXISTS Transactions (
                        TransactionID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        Debit REAL NOT NULL,
                        Credit REAL NOT NULL,
                        Category TEXT NOT NULL,
                        Color TEXT NOT NULL,
                        Frequency TEXT,
                        EndDate TEXT
                    );
                ");

                // Insert initial roles
                InsertInitialRoles(connection);


                // Delete columns if exists
                DeleteColumnsIfExists(connection, "Users", stringArray);

            }
        }

        private static void AlterTableIfExists(SqliteConnection connection, string tableName, string alterTableQuery)
        {
            string checkTableQuery = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
            using var command = new SqliteCommand(checkTableQuery, connection);
            var result = command.ExecuteScalar();
            if (result != null)
            {
                using (var alterCommand = new SqliteCommand(alterTableQuery, connection))
                {
                    alterCommand.ExecuteNonQuery();
                }
            }
        }
        private static void DeleteColumnsIfExists(SqliteConnection connection, string tableName, string[] columnsToDelete)
        {
            // Get the column names of the original table
            string getColumnNamesQuery = $"PRAGMA table_info({tableName});";
            var columnNames = new List<string>();
            var columnsToInsert = new List<string>();
            using (var getColumnNamesCommand = new SqliteCommand(getColumnNamesQuery, connection))
            using (var reader = getColumnNamesCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (!columnsToDelete.Contains(columnName))
                    {
                        columnNames.Add(columnName);
                    }
                    else
                    {
                        columnsToInsert.Add(columnName);
                    }
                }
            }

            // Create a temporary table with the desired structure
            string columns = string.Join(", ", columnNames);
            string createTempTableQuery = $@"
                CREATE TABLE {tableName}_temp AS
                SELECT {columns} FROM {tableName} WHERE 0;
            ";
            using (var createTempTableCommand = new SqliteCommand(createTempTableQuery, connection))
            {
                createTempTableCommand.ExecuteNonQuery();
            }

            // Copy data from the original table to the temporary table
            string copyDataQuery = $@"
                INSERT INTO {tableName}_temp ({columns})
                SELECT {columns} FROM {tableName};
            ";
            using (var copyDataCommand = new SqliteCommand(copyDataQuery, connection))
            {
                copyDataCommand.ExecuteNonQuery();
            }

            // Temporarily disable foreign key checks
            using (var disableForeignKeyChecksCommand = new SqliteCommand("PRAGMA foreign_keys = OFF;", connection))
            {
                disableForeignKeyChecksCommand.ExecuteNonQuery();
            }

            // Insert deleted columns into Profile table from Users Table
            if (tableName == "Users" && columnsToInsert.Count > 0)
            {
                string columnsToInsertStr = string.Join(", ", columnsToInsert);
                string insertProfileQuery = $@"
                    INSERT INTO Profile (UserID, {columnsToInsertStr})
                    SELECT UserId, {columnsToInsertStr} FROM {tableName};
                ";
                using (var insertProfileCommand = new SqliteCommand(insertProfileQuery, connection))
                {
                    insertProfileCommand.ExecuteNonQuery();
                }
            }

            // Drop the original table
            string dropTableQuery = $"DROP TABLE {tableName};";
            using (var dropTableCommand = new SqliteCommand(dropTableQuery, connection))
            {
                dropTableCommand.ExecuteNonQuery();
            }

            // Rename the temporary table to the original table name
            string renameTableQuery = $"ALTER TABLE {tableName}_temp RENAME TO {tableName};";
            using (var renameTableCommand = new SqliteCommand(renameTableQuery, connection))
            {
                renameTableCommand.ExecuteNonQuery();
            }

            // Re-enable foreign key checks
            using (var enableForeignKeyChecksCommand = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
            {
                enableForeignKeyChecksCommand.ExecuteNonQuery();
            }
        }

        private static void CreateTableIfNotExists(SqliteConnection connection, string tableName, string createTableQuery)
        {
            string checkTableQuery = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
            using var command = new SqliteCommand(checkTableQuery, connection);
            var result = command.ExecuteScalar();
            if (result == null)
            {
                using var createCommand = new SqliteCommand(createTableQuery, connection);
                createCommand.ExecuteNonQuery();
            }
        }

        private static void InsertInitialRoles(SqliteConnection connection)
        {
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                string insertRoleQuery = @"
                    INSERT INTO Roles (RoleName)
                    SELECT @RoleName
                    WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = @RoleName);
                ";

                using (var command = new SqliteCommand(insertRoleQuery, connection))
                {
                    command.Parameters.AddWithValue("@RoleName", role);
                    command.ExecuteNonQuery();
                }
            }
        }

        // This method is used to insert a new user into the database
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

        public static void InsertProfile()
        {
            using var connection = GetConnection();
            connection.Open();
            string insertQuery = @"
                    INSERT INTO Profile (FirstName, LastName, Username, Birthday, Email, Password)
                    VALUES ('John', 'Doe', 'johndoe', '1990-01-01', '};";
        }

        // This method is used to insert a new role into the database
        public static void InsertRole(string roleName)
        {
            using var connection = GetConnection();
            connection.Open();

            string insertQuery = @"
                    INSERT INTO Roles (RoleName)
                    VALUES (@RoleName);
                ";

            using var command = new SqliteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@RoleName", roleName);
            command.ExecuteNonQuery();
        }

        // This method is used to assign a role to a user
        public static void AssignRoleToUser(int userId, int roleId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string insertQuery = @"
                    INSERT INTO UserRoles (UserId, RoleId)
                    VALUES (@UserId, @RoleId);
                ";

                using (var command = new SqliteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@RoleId", roleId);
                    command.ExecuteNonQuery();
                }
            }
        }

        // This method is used to check if a username is already taken
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

        // This method is used to check if an email is already taken
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

        // This method is used to validate the user credentials with the database
        // It returns true if the user credentials are valid, otherwise false
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

        // This method is used to get the connection to the database
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

        // This method gets the user ID by username
        public static int GetUserIdByUsername(string username)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string query = "SELECT UserId FROM Users WHERE Username = @Username;";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }

        // This method gets the role ID by role name
        public static int GetRoleIdByName(string roleName)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string query = "SELECT RoleId FROM Roles WHERE RoleName = @RoleName;";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RoleName", roleName);
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
        }
    }
}

