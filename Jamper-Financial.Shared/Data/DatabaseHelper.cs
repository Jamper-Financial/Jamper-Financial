using System.Linq.Expressions;
using Jamper_Financial.Shared.Models;
using Microsoft.Data.Sqlite;
using Microsoft.VisualBasic;

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

                // Create Goals table
                CreateTableIfNotExists(connection, "Goals", @"
               CREATE TABLE IF NOT EXISTS Goals (
                   GoalId INTEGER PRIMARY KEY AUTOINCREMENT,
                    Type TEXT,
                    Name TEXT,
                    Amount REAL,
                    Date TEXT,
                    GoalType TEXT,
                    IsQuickGoal INTEGER,
                    IsRetirementGoal INTEGER,
                    IsEmergencyFundGoal INTEGER,
                    IsTravelGoal INTEGER,
                    IsHomeGoal INTEGER,
                    Category TEXT,
                    StartDate TEXT,
                    EndDate TEXT,
                    Description TEXT,
                    ShowDescription INTEGER,
                    Frequency TEXT,
                    IsFadingOut INTEGER
                );
        ");

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
                        UserID INTEGER NOT NULL,
                        Date TEXT NOT NULL, 
                        Description TEXT NOT NULL,
                        Amount DECIMAL(10, 2) NOT NULL,  -- Re-added Amount
                        Debit DECIMAL(10, 2) DEFAULT 0,  
                        Credit DECIMAL(10, 2) DEFAULT 0, 
                        CategoryID INTEGER NOT NULL,  
                        TransactionType TEXT CHECK(TransactionType IN ('e', 'i')) NOT NULL,
                        HasReceipt INTEGER DEFAULT 0,
                        Frequency TEXT DEFAULT 'None',
                        EndDate TEXT,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
                        FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID) ON DELETE CASCADE
                    );
                ");

                CreateTableIfNotExists(connection, "Categories", @"
                    CREATE TABLE IF NOT EXISTS Categories (
                        CategoryID INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserID INTEGER NOT NULL,
                        Name TEXT NOT NULL,
                        Color TEXT NOT NULL,
                        TransactionType TEXT CHECK(TransactionType IN ('e', 'i')) NOT NULL,
                        ParentCategoryID INTEGER,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE,
                        FOREIGN KEY (ParentCategoryID) REFERENCES Categories(CategoryID) ON DELETE SET NULL
                        
                    );

                ");

                CreateTableIfNotExists(connection, "Budget", @"
                    CREATE TABLE IF NOT EXISTS Budget (
                        BudgetID INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserID INTEGER NOT NULL,
                        CategoryID INTEGER NOT NULL,
                        PlannedAmount REAL NOT NULL,
                        FOREIGN KEY (UserID) REFERENCES Users(UserID),
                        FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
                    );
                ");

                CreateTableIfNotExists(connection, "Receipts", @"
                    CREATE TABLE IF NOT EXISTS Receipts (
                        ReceiptID INTEGER PRIMARY KEY AUTOINCREMENT,
                        TransactionID INTEGER NOT NULL,
                        ReceiptData BLOB NOT NULL,
                        Description TEXT NULL,
                        FOREIGN KEY (TransactionID) REFERENCES Transactions(TransactionID)
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
        public static List<Goal> GetGoals()
        {
            var goals = new List<Goal>();
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM Goals;";
                using (var command = new SqliteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DateTime date, startDate, endDate;
                        DateTime.TryParseExact(reader["Date"].ToString(), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out date);
                        DateTime.TryParseExact(reader["StartDate"].ToString(), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out startDate);
                        DateTime.TryParseExact(reader["EndDate"].ToString(), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out endDate);

                        goals.Add(new Goal
                        {
                            GoalId = reader.GetInt32(reader.GetOrdinal("GoalId")),
                            Type = reader["Type"].ToString(),
                            Name = reader["Name"].ToString(),
                            Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
                            Date = date,
                            GoalType = reader["GoalType"].ToString(),
                            IsQuickGoal = reader.GetBoolean(reader.GetOrdinal("IsQuickGoal")),
                            IsRetirementGoal = reader.GetBoolean(reader.GetOrdinal("IsRetirementGoal")),
                            IsEmergencyFundGoal = reader.GetBoolean(reader.GetOrdinal("IsEmergencyFundGoal")),
                            IsTravelGoal = reader.GetBoolean(reader.GetOrdinal("IsTravelGoal")),
                            IsHomeGoal = reader.GetBoolean(reader.GetOrdinal("IsHomeGoal")),
                            Category = reader["Category"].ToString(),
                            StartDate = startDate,
                            EndDate = endDate,
                            Description = reader["Description"].ToString(),
                            ShowDescription = reader.GetBoolean(reader.GetOrdinal("ShowDescription")),
                            Frequency = reader["Frequency"].ToString(),
                            IsFadingOut = reader.GetBoolean(reader.GetOrdinal("IsFadingOut"))
                        });
                    }
                }
            }
            return goals;
        }
        public static void InsertGoal(Goal goal)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();
                    string insertQuery = @"
                    INSERT INTO Goals (Type, Name, Amount, Date, GoalType, IsQuickGoal, IsRetirementGoal, IsEmergencyFundGoal, IsTravelGoal, IsHomeGoal, Category, StartDate, EndDate, Description, ShowDescription, Frequency, IsFadingOut)
                    VALUES (@Type, @Name, @Amount, @Date, @GoalType, @IsQuickGoal, @IsRetirementGoal, @IsEmergencyFundGoal, @IsTravelGoal, @IsHomeGoal, @Category, @StartDate, @EndDate, @Description, @ShowDescription, @Frequency, @IsFadingOut);
                ";
                    using (var command = new SqliteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Type", goal.Type ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Name", goal.Name ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Amount", goal.Amount);
                        command.Parameters.AddWithValue("@Date", goal.Date != default ? goal.Date.ToString("yyyy-MM-dd") : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GoalType", goal.GoalType ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsQuickGoal", goal.IsQuickGoal);
                        command.Parameters.AddWithValue("@IsRetirementGoal", goal.IsRetirementGoal);
                        command.Parameters.AddWithValue("@IsEmergencyFundGoal", goal.IsEmergencyFundGoal);
                        command.Parameters.AddWithValue("@IsTravelGoal", goal.IsTravelGoal);
                        command.Parameters.AddWithValue("@IsHomeGoal", goal.IsHomeGoal);
                        command.Parameters.AddWithValue("@Category", goal.Category ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@StartDate", goal.StartDate != default ? goal.StartDate.ToString("yyyy-MM-dd") : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@EndDate", goal.EndDate != default ? goal.EndDate.ToString("yyyy-MM-dd") : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Description", goal.Description ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ShowDescription", goal.ShowDescription);
                        command.Parameters.AddWithValue("@Frequency", goal.Frequency ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IsFadingOut", goal.IsFadingOut);
                        command.ExecuteNonQuery();

                        command.CommandText = "SELECT last_insert_rowid();"; // ADDED
                        long newId = (long)command.ExecuteScalar();         // ADDED
                        goal.GoalId = (int)newId; 
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static void UpdateGoal(Goal goal) // ADDED
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string updateQuery = @"
                    UPDATE Goals SET 
                        Type = @Type,
                        Name = @Name,
                        Amount = @Amount,
                        Date = @Date,
                        GoalType = @GoalType,
                        IsQuickGoal = @IsQuickGoal,
                        IsRetirementGoal = @IsRetirementGoal,
                        IsEmergencyFundGoal = @IsEmergencyFundGoal,
                        IsTravelGoal = @IsTravelGoal,
                        IsHomeGoal = @IsHomeGoal,
                        Category = @Category,
                        StartDate = @StartDate,
                        EndDate = @EndDate,
                        Description = @Description,
                        ShowDescription = @ShowDescription,
                        Frequency = @Frequency,
                        IsFadingOut = @IsFadingOut
                    WHERE GoalId = @GoalId";
                using (var command = new SqliteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@Type", goal.Type ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Name", goal.Name ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Amount", goal.Amount);
                    command.Parameters.AddWithValue("@Date", goal.Date != default ? goal.Date.ToString("yyyy-MM-dd") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@GoalType", goal.GoalType ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IsQuickGoal", goal.IsQuickGoal ? 1 : 0);
                    command.Parameters.AddWithValue("@IsRetirementGoal", goal.IsRetirementGoal ? 1 : 0);
                    command.Parameters.AddWithValue("@IsEmergencyFundGoal", goal.IsEmergencyFundGoal ? 1 : 0);
                    command.Parameters.AddWithValue("@IsTravelGoal", goal.IsTravelGoal ? 1 : 0);
                    command.Parameters.AddWithValue("@IsHomeGoal", goal.IsHomeGoal ? 1 : 0);
                    command.Parameters.AddWithValue("@Category", goal.Category ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@StartDate", goal.StartDate != default ? goal.StartDate.ToString("yyyy-MM-dd") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@EndDate", goal.EndDate != default ? goal.EndDate.ToString("yyyy-MM-dd") : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Description", goal.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ShowDescription", goal.ShowDescription ? 1 : 0);
                    command.Parameters.AddWithValue("@Frequency", goal.Frequency ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IsFadingOut", goal.IsFadingOut ? 1 : 0);
                    command.Parameters.AddWithValue("@GoalId", goal.GoalId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteGoal(int goalId)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        string deleteQuery = "DELETE FROM Goals WHERE GoalId = @GoalId;";
                        using (var command = new SqliteCommand(deleteQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@GoalId", goalId);
                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                transaction.Commit();
                            }
                            else
                            {
                                transaction.Rollback();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            
            }
        }




        private static void DeleteColumnsIfExists(SqliteConnection connection, string tableName, string[] columnsToDelete)
        {
            // Get the column names of the original table
            string getColumnNamesQuery = $"PRAGMA table_info({tableName});";
            var columnNames = new List<string>();
            var columnsToInsert = new List<string>();
            var columnsExist = false;

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
                        columnsExist = true;
                    }
                }
            }

            // If none of the columns to delete exist, return early
            if (!columnsExist)
            {
                return;
            }

            // Create a temporary table with the desired structure
            string columns = string.Join(", ", columnNames);
            string createTempTableQuery = $@"
                CREATE TABLE {tableName}_temp (
                        UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        Email TEXT NOT NULL UNIQUE,
                        Password TEXT NOT NULL
                );
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

        // Add categories 
        public static void InsertCategory(int userId, string name, string color, string transactionType, int? parentCategoryId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string insertQuery = @"
            INSERT INTO Categories (UserID, Name, Color, TransactionType, ParentCategoryID)
            VALUES (@UserID, @Name, @Color, @TransactionType, @ParentCategoryID);
        ";

                using (var command = new SqliteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Color", color);
                    command.Parameters.AddWithValue("@TransactionType", transactionType);
                    command.Parameters.AddWithValue("@ParentCategoryID", parentCategoryId ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }


        // Update Categories
        public static void UpdateCategory(int userId, int categoryId, string newName, string newColor, string newTransactionType)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string updateQuery = "UPDATE Categories SET Name = @NewName, Color = @NewColor, TransactionType = @TransactionType WHERE CategoryID = @CategoryID AND UserID = @UserID;";
                using (var command = new SqliteCommand(updateQuery, connection))
                {
                    command.Parameters.AddWithValue("@NewName", newName);
                    command.Parameters.AddWithValue("@NewColor", newColor);
                    command.Parameters.AddWithValue("@TransactionType", newTransactionType);
                    command.Parameters.AddWithValue("@CategoryID", categoryId);
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.ExecuteNonQuery();
                }
            }
        }


        // Delete Categories
        public static void DeleteCategory(int userId, int categoryId)
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                string deleteQuery = "DELETE FROM Categories WHERE CategoryID = @CategoryID AND UserID = @UserID;";
                using (var command = new SqliteCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@CategoryID", categoryId);
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Get Categories
        public static List<Category> GetCategories(int userId)
        {
            var categories = new List<Category>();
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                string query = "SELECT CategoryID, Name FROM Categories WHERE UserID = @UserID;";
                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                CategoryID = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return categories;
        }

        public static async Task<List<Category>> LoadUserCategoriesAsync(int userId)
        {
            var categories = new List<Category>();

            using (var connection = GetConnection())
            {
                await connection.OpenAsync();

                string query = @"
            SELECT CategoryID, UserID, Name, Color, TransactionType
            FROM Categories
            WHERE UserID = @UserID";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            categories.Add(new Category
                            {
                                CategoryID = reader.GetInt32(0),
                                UserID = reader.GetInt32(1),
                                Name = reader.GetString(2),
                                Color = reader.GetString(3),
                                TransactionType = reader.GetString(4)
                            });
                        }
                    }
                }
            }

            return categories;
        }



        // Insert Set Categories
        private static void InsertDefaultCategories(int userId, SqliteConnection connection)
        {
            string[] defaultCategories = {
        "Rent", "Mortgage", "Utilities", "Insurance", "Condo fees",
        "Car maintenance", "Car payment", "Gas", "Public transportation",
        "Groceries", "Restaurant", "Take Out",
        "Gym", "Medical",
        "Entertainment", "Going out", "Subscriptions",
        "Clothing", "Gifts", "Electronics", "Home maintenance",
        "Debt", "Work (Job)", "Side project", "Tax refund", "Expense reimbursement"
    };

            foreach (var category in defaultCategories)
            {
                string insertCategoryQuery = "INSERT INTO Categories (UserID, Name) VALUES (@UserID, @Name);";
                using (var command = new SqliteCommand(insertCategoryQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);
                    command.Parameters.AddWithValue("@Name", category);
                    command.ExecuteNonQuery();
                }
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
        public static void InsertUser(string username, string email, string password)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string insertQuery = @"
                    INSERT INTO Users (Username, Email, Password)
                    VALUES (@Username, @Email, @Password);
                ";

                using (var command = new SqliteCommand(insertQuery, connection))
                {

                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);

                    command.ExecuteNonQuery();
                }
            }
        }

        public static void InsertProfile(int UserId, string FirstName, string LastName, string Birthday)
        {
            using var connection = GetConnection();
            connection.Open();
            string insertQuery = @"
                    INSERT INTO Profile (UserID, FirstName, LastName, Birthday)
                    VALUES (@UserId, @FirstName, @LastName, @Birthday);";

            using (var command = new SqliteCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@UserId", UserId);
                command.Parameters.AddWithValue("@FirstName", FirstName);
                command.Parameters.AddWithValue("@LastName", LastName);
                command.Parameters.AddWithValue("@Birthday", Birthday);

                command.ExecuteNonQuery();
            }
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

        // public static (int UserId, int ProfileId, string Role)? GetUserDetailsByEmail(string email)
        // {
        //     try
        //     {
        //         using (var connection = GetConnection())
        //         {
        //             connection.Open();

        //             // Corrected query to join Users, Profile, UserRoles, and Roles tables
        //             string query = @"
        //                 SELECT u.UserId, p.ProfileId, r.RoleName 
        //                 FROM Users u
        //                 LEFT JOIN Profile p ON u.UserId = p.UserId
        //                 LEFT JOIN UserRoles ur ON u.UserId = ur.UserId
        //                 LEFT JOIN Roles r ON ur.RoleId = r.RoleId
        //                 WHERE u.Email = @Email;";

        //             using (var command = new SqliteCommand(query, connection))
        //             {
        //                 command.Parameters.AddWithValue("@Email", email);

        //                 using (var reader = command.ExecuteReader())
        //                 {
        //                     if (reader.Read())
        //                     {
        //                         int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
        //                         int profileId = reader.GetInt32(reader.GetOrdinal("ProfileId"));
        //                         string role = reader["RoleName"].ToString();
        //                         return (userId, profileId, role);
        //                     }
        //                 }
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         // Log the error (if you have a logging mechanism)
        //         Console.WriteLine($"Error in GetUserDetailsByEmail: {ex.Message}");
        //     }

        //     return null;
        // }

        // public static (int UserId, int ProfileId, string Role)? GetUserDetailsByEmail(string email)
        // {
        //     try
        //     {
        //         using (var connection = GetConnection())
        //         {
        //             connection.Open();

        //             // Normalize the email (trim and convert to lowercase)
        //             string normalizedEmail = email.Trim().ToLower();

        //             // Debugging: Log the normalized email
        //             Console.WriteLine($"Querying database for email: {normalizedEmail}");

        //             string query = @"
        //                 SELECT u.UserId, p.ProfileId, r.RoleName 
        //                 FROM Users u
        //                 LEFT JOIN Profile p ON u.UserId = p.UserId
        //                 LEFT JOIN UserRoles ur ON u.UserId = ur.UserId
        //                 LEFT JOIN Roles r ON ur.RoleId = r.RoleId
        //                 WHERE LOWER(TRIM(u.Email)) = @Email;";

        //             using (var command = new SqliteCommand(query, connection))
        //             {
        //                 command.Parameters.AddWithValue("@Email", normalizedEmail);

        //                 using (var reader = command.ExecuteReader())
        //                 {
        //                     if (reader.Read())
        //                     {
        //                         int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
        //                         int profileId = reader.GetInt32(reader.GetOrdinal("ProfileId"));
        //                         string role = reader["RoleName"].ToString();

        //                         // Debugging: Log the retrieved user details
        //                         Console.WriteLine($"Retrieved UserId: {userId}, ProfileId: {profileId}, Role: {role}");

        //                         return (userId, profileId, role);
        //                     }
        //                     else
        //                     {
        //                         // Debugging: Log if no rows are returned
        //                         Console.WriteLine("No user found with the specified email.");
        //                     }
        //                 }
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         // Log the error (if you have a logging mechanism)
        //         Console.WriteLine($"Error in GetUserDetailsByEmail: {ex.Message}");
        //     }

        //     return null;
        // }

        public static (int UserId, int ProfileId, string Role)? GetUserDetailsByUsernameOrEmail(string usernameOrEmail)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();

                    // Normalize the username or email (trim and convert to lowercase)
                    string normalizedUsernameOrEmail = usernameOrEmail.Trim().ToLower();

                    // Debugging: Log the normalized username or email
                    Console.WriteLine($"Querying database for username or email: {normalizedUsernameOrEmail}");

                    string query = @"
                        SELECT u.UserId, p.ProfileId, r.RoleName 
                        FROM Users u
                        LEFT JOIN Profile p ON u.UserId = p.UserId
                        LEFT JOIN UserRoles ur ON u.UserId = ur.UserId
                        LEFT JOIN Roles r ON ur.RoleId = r.RoleId
                        WHERE LOWER(TRIM(u.Username)) = @UsernameOrEmail OR LOWER(TRIM(u.Email)) = @UsernameOrEmail;";

                    using (var command = new SqliteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UsernameOrEmail", normalizedUsernameOrEmail);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(reader.GetOrdinal("UserId"));
                                int profileId = reader.GetInt32(reader.GetOrdinal("ProfileId"));
                                string role = reader["RoleName"].ToString();

                                // Debugging: Log the retrieved user details
                                Console.WriteLine($"Retrieved UserId: {userId}, ProfileId: {profileId}, Role: {role}");

                                return (userId, profileId, role);
                            }
                            else
                            {
                                // Debugging: Log if no rows are returned
                                Console.WriteLine("No user found with the specified username or email.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error (if you have a logging mechanism)
                Console.WriteLine($"Error in GetUserDetailsByUsernameOrEmail: {ex.Message}");
            }

            return null;
        }

        // This method deletes a user and profile from the database
        public static bool DeleteUserAndProfile(int userId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Delete from UserRoles table
                        string deleteUserRolesQuery = "DELETE FROM UserRoles WHERE UserId = @UserId;";
                        using (var deleteUserRolesCommand = new SqliteCommand(deleteUserRolesQuery, connection, transaction))
                        {
                            deleteUserRolesCommand.Parameters.AddWithValue("@UserId", userId);
                            deleteUserRolesCommand.ExecuteNonQuery();
                        }

                        // Delete from Profile table
                        string deleteProfileQuery = "DELETE FROM Profile WHERE UserId = @UserId;";
                        using (var deleteProfileCommand = new SqliteCommand(deleteProfileQuery, connection, transaction))
                        {
                            deleteProfileCommand.Parameters.AddWithValue("@UserId", userId);
                            deleteProfileCommand.ExecuteNonQuery();
                        }

                        // Delete from Users table
                        string deleteUserQuery = "DELETE FROM Users WHERE UserId = @UserId;";
                        using (var deleteUserCommand = new SqliteCommand(deleteUserQuery, connection, transaction))
                        {
                            deleteUserCommand.Parameters.AddWithValue("@UserId", userId);
                            deleteUserCommand.ExecuteNonQuery();
                        }

                        // Commit the transaction if all operations succeed
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction if any operation fails
                        transaction.Rollback();
                        Console.WriteLine($"Error in DeleteUserAndProfile: {ex.Message}");
                        return false;
                    }
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

        public static (int Userid, string Username, string Email) GetUserDetails(string identifier)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT UserId, Username, Email
                    FROM Users
                    WHERE Username = @Identifier OR Email = @Identifier;
                ";

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Identifier", identifier);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userid = reader.GetInt32(reader.GetOrdinal("UserId"));
                            string username = reader["Username"].ToString();
                            string email = reader["Email"].ToString();
                            return (userid, username, email);
                        }
                    }
                }
                return (0, null, null); // Return null if no user found
            }
        }

    }
}
