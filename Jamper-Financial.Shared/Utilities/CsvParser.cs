using Jamper_Financial.Shared.Models;
using System.Globalization;

namespace Jamper_Financial.Shared.Utilities
{
    public static class CsvParser
    {
        public static List<Transaction> ParseCsv(string csvContent, int accountId, int userId, string bankName)
        {
            bool hasHeaders = DetectHeaders(csvContent);
            return bankName switch
            {
                "RBC" => ParseRbcCsv(csvContent, accountId, userId, hasHeaders),
                "TD" => ParseTdCsv(csvContent, accountId, userId, hasHeaders),
                "Scotiabank" => ParseScotiabankCsv(csvContent, accountId, userId, hasHeaders),
                "BMO" => ParseBmoCsv(csvContent, accountId, userId, hasHeaders),
                "CIBC" => ParseCibcCsv(csvContent, accountId, userId, hasHeaders),
                _ => throw new ArgumentException("Unsupported bank")
            };
        }

        private static bool DetectHeaders(string csvContent)
        {
            var firstLine = csvContent.Split('\n').FirstOrDefault();
            if (firstLine == null) return false;

            var columns = firstLine.Split(',');
            // Check if the first line contains non-numeric values which are likely headers
            return columns.Any(column => !decimal.TryParse(column, out _));
        }

        private static List<Transaction> ParseRbcCsv(string csvContent, int accountId, int userId, bool hasHeaders)
        {
            var transactions = new List<Transaction>();
            var lines = csvContent.Split('\n');
            if (hasHeaders) lines = lines.Skip(1).ToArray(); // Skip header if present

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var columns = line.Split(',');
                transactions.Add(new Transaction
                {
                    Date = DateTime.ParseExact(columns[0], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Description = columns[2].Trim('"'),
                    Amount = decimal.Parse(columns[3], CultureInfo.InvariantCulture),
                    AccountID = accountId,
                    UserID = userId,
                    IsPaid = true,
                    TransactionType = columns[3].StartsWith("-") ? "e" : "i", // Expense if negative
                    CategoryID = 0, // Default value, update as needed
                    HasReceipt = false,
                    Frequency = "None",
                    EndDate = null,
                    TemporaryReceiptFilePath = null,
                    addItem = true
                });
            }
            return transactions;
        }

        private static List<Transaction> ParseTdCsv(string csvContent, int accountId, int userId, bool hasHeaders)
        {
            var transactions = new List<Transaction>();
            var lines = csvContent.Split('\n');
            if (hasHeaders) lines = lines.Skip(1).ToArray(); // Skip header if present

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var columns = line.Split(',');
                transactions.Add(new Transaction
                {
                    Date = DateTime.ParseExact(columns[0], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                    Description = columns[1].Trim('"'),
                    Amount = decimal.Parse(columns[2].Contains("(")
                        ? $"-{columns[2].Replace("(", "").Replace(")", "")}"
                        : columns[2],
                        CultureInfo.InvariantCulture),
                    AccountID = accountId,
                    UserID = userId,
                    IsPaid = true,
                    TransactionType = columns[2].Contains("(") ? "e" : "i",
                    CategoryID = 0, // Default value, update as needed
                    HasReceipt = false,
                    Frequency = "None",
                    EndDate = null,
                    TemporaryReceiptFilePath = null,
                    addItem = true
                });
            }
            return transactions;
        }

        private static List<Transaction> ParseScotiabankCsv(string csvContent, int accountId, int userId, bool hasHeaders)
        {
            var transactions = new List<Transaction>();
            var lines = csvContent.Split('\n');
            if (hasHeaders) lines = lines.Skip(1).ToArray(); // Skip header if present

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var columns = line.Split(',');
                transactions.Add(new Transaction
                {
                    Date = DateTime.ParseExact(columns[0], "yyyy/MM/dd", CultureInfo.InvariantCulture),
                    Description = columns[1].Trim('"'),
                    Amount = decimal.Parse(columns[2], CultureInfo.InvariantCulture),
                    AccountID = accountId,
                    UserID = userId,
                    IsPaid = true,
                    TransactionType = columns[2].StartsWith("-") ? "e" : "i",
                    CategoryID = 0, // Default value, update as needed
                    HasReceipt = false,
                    Frequency = "None",
                    EndDate = null,
                    TemporaryReceiptFilePath = null,
                    addItem = true
                });
            }
            return transactions;
        }

        private static List<Transaction> ParseBmoCsv(string csvContent, int accountId, int userId, bool hasHeaders)
        {
            var transactions = new List<Transaction>();
            var lines = csvContent.Split('\n');
            if (hasHeaders) lines = lines.Skip(1).ToArray(); // Skip header if present

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var columns = line.Split(',');
                transactions.Add(new Transaction
                {
                    Date = DateTime.ParseExact(columns[0], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                    Description = columns[1].Trim('"'),
                    Amount = decimal.Parse(columns[2], CultureInfo.InvariantCulture),
                    AccountID = accountId,
                    UserID = userId,
                    IsPaid = true,
                    TransactionType = columns[2].StartsWith("-") ? "e" : "i",
                    CategoryID = 0, // Default value, update as needed
                    HasReceipt = false,
                    Frequency = "None",
                    EndDate = null,
                    TemporaryReceiptFilePath = null,
                    addItem = true
                });
            }
            return transactions;
        }

        private static List<Transaction> ParseCibcCsv(string csvContent, int accountId, int userId, bool hasHeaders)
        {
            var transactions = new List<Transaction>();
            var lines = csvContent.Split('\n');
            if (hasHeaders) lines = lines.Skip(1).ToArray(); // Skip header if present

            foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                var columns = line.Split(',');
                transactions.Add(new Transaction
                {
                    Date = DateTime.ParseExact(columns[0], "MM/dd/yyyy", CultureInfo.InvariantCulture),
                    Description = columns[1].Trim('"'),
                    Amount = decimal.Parse(columns[2].Contains("DR")
                        ? $"-{columns[2].Replace("DR", "")}"
                        : columns[2].Replace("CR", ""),
                        CultureInfo.InvariantCulture),
                    AccountID = accountId,
                    UserID = userId,
                    IsPaid = true,
                    TransactionType = columns[2].Contains("DR") ? "e" : "i",
                    CategoryID = 0, // Default value, update as needed
                    HasReceipt = false,
                    Frequency = "None",
                    EndDate = null,
                    TemporaryReceiptFilePath = null,
                    addItem = true
                });
            }
            return transactions;
        }
    }
}
