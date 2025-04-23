using Jamper_Financial.Shared.Models;
using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace Jamper_Financial.Shared.Utilities
{
    public static class CsvParser
    {
        // Supported date formats
        private static readonly string[] SupportedDateFormats = new[]
        {
            "yyyy-MM-dd", "MM/dd/yyyy", "dd-MM-yyyy", "dd/MM/yyyy"
        };

        public static List<Transaction> ParseCsv(
            string csvContent,
            int accountId,
            int userId,
            Dictionary<string, int> columnMappings,
            string delimiter = ",",
            string[]? additionalDateFormats = null)
        {
            var transactions = new List<Transaction>();

            // Merge supported and additional date formats
            var dateFormats = SupportedDateFormats.Concat(additionalDateFormats ?? Array.Empty<string>()).ToArray();

            using (var reader = new StringReader(csvContent))
            using (var parser = new TextFieldParser(reader))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(delimiter);

                int lineNumber = 0;

                // Read the header row if present
                if (columnMappings.Values.All(v => v >= 0))
                {
                    parser.ReadLine(); // Skip the header row
                    lineNumber++;
                }

                while (!parser.EndOfData)
                {
                    lineNumber++;
                    try
                    {
                        var columns = parser.ReadFields(); // Reads a line and splits it into fields
                        if (columns == null || columns.All(string.IsNullOrWhiteSpace)) continue; // Skip empty rows

                        // Ensure required columns are mapped
                        if (!columnMappings.ContainsKey("Date") || columnMappings["Date"] < 0 ||
                            !columnMappings.ContainsKey("Description") || columnMappings["Description"] < 0 ||
                            (!columnMappings.ContainsKey("Amount") &&
                             (!columnMappings.ContainsKey("Debit") || !columnMappings.ContainsKey("Credit"))))
                        {
                            throw new InvalidOperationException("Required columns (Date, Description, Amount) are not mapped.");
                        }

                        // Parse each field
                        var amount = ParseAmount(
                            columns.ElementAtOrDefault(columnMappings.GetValueOrDefault("Amount")),
                            columns.ElementAtOrDefault(columnMappings.GetValueOrDefault("Debit")),
                            columns.ElementAtOrDefault(columnMappings.GetValueOrDefault("Credit"))
                        );

                        var transaction = new Transaction
                        {
                            Date = ParseDate(columns.ElementAtOrDefault(columnMappings["Date"]), dateFormats),
                            Description = ParseDescription(columns.ElementAtOrDefault(columnMappings["Description"])),
                            Amount = amount,

                            // Assign CategoryID and TransactionType based on Amount
                            TransactionType = amount > 0 ? "i" : "e", // "i" for income, "e" for expense
                            CategoryID = amount > 0 ? 8 : 6, // 8 for income, 6 for expense

                            // Default values for other fields
                            AccountID = accountId,
                            UserID = userId,
                            IsPaid = true,
                            HasReceipt = false,
                            Frequency = "None", // Default to "None"
                            EndDate = null, // Default to null
                            TemporaryReceiptFilePath = null,
                            addItem = true
                        };

                        // Validate transaction
                        ValidateTransaction(transaction);

                        transactions.Add(transaction);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing line {lineNumber}: {ex.Message}");
                    }
                }
            }

            return transactions;
        }

        // Parse Date with support for multiple formats
        private static DateTime ParseDate(string? dateValue, string[] dateFormats)
        {
            if (string.IsNullOrWhiteSpace(dateValue))
            {
                throw new FormatException("Date value is required but was empty or null.");
            }

            // Trim quotes and whitespace
            dateValue = dateValue.Trim('"').Trim();

            // Try parsing the date with supported formats
            if (DateTime.TryParseExact(dateValue, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate;
            }

            throw new FormatException($"Invalid date format: {dateValue}. Supported formats are: {string.Join(", ", dateFormats)}");
        }

        // Parse Description
        private static string ParseDescription(string? descriptionValue)
        {
            return string.IsNullOrWhiteSpace(descriptionValue) ? "No Description" : descriptionValue.Trim();
        }

        // Parse Amount with support for different formats
        private static decimal ParseAmount(string? amountValue, string? debitValue = null, string? creditValue = null, bool isAmountInverted = false)
        {
            // Case 1: Single Amount Column
            if (!string.IsNullOrWhiteSpace(amountValue))
            {
                amountValue = amountValue.Trim().ToUpperInvariant();

                // Handle DR suffix (debit)
                if (amountValue.EndsWith("DR"))
                {
                    amountValue = amountValue.Replace("DR", "").Trim();
                    if (decimal.TryParse(amountValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDebitAmount))
                    {
                        return isAmountInverted ? parsedDebitAmount : -parsedDebitAmount; // Invert if needed
                    }
                }
                // Handle CR suffix (credit)
                else if (amountValue.EndsWith("CR"))
                {
                    amountValue = amountValue.Replace("CR", "").Trim();
                    if (decimal.TryParse(amountValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedCreditAmount))
                    {
                        return isAmountInverted ? -parsedCreditAmount : parsedCreditAmount; // Invert if needed
                    }
                }
                // Handle regular amount
                else if (decimal.TryParse(amountValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedAmount))
                {
                    return isAmountInverted ? -parsedAmount : parsedAmount; // Invert if needed
                }

                throw new FormatException($"Invalid amount format: {amountValue}");
            }

            // Case 2: Separate Debit and Credit Columns
            if (!string.IsNullOrWhiteSpace(debitValue) &&
                decimal.TryParse(debitValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDebit))
            {
                return isAmountInverted ? parsedDebit : -parsedDebit; // Debit is negative, invert if needed
            }

            if (!string.IsNullOrWhiteSpace(creditValue) &&
                decimal.TryParse(creditValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedCredit))
            {
                return isAmountInverted ? -parsedCredit : parsedCredit; // Credit is positive, invert if needed
            }

            // Default to 0 if no valid amount is found
            return 0;
        }


        // Validate Transaction
        private static void ValidateTransaction(Transaction transaction)
        {
            if (transaction.Amount == 0)
            {
                throw new InvalidOperationException("Transaction amount cannot be zero.");
            }

            if (transaction.Date == default)
            {
                throw new InvalidOperationException("Transaction date is invalid.");
            }
        }
    }
}

