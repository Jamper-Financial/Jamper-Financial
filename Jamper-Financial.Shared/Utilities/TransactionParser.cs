using System.Globalization;
using System.Text.RegularExpressions;
using Tesseract;
using System.IO; // Add this using directive

namespace Jamper_Financial.Shared.Utilities
{
    public class TransactionParser
    {
        public static string ExtractTextFromImage(string imagePath)
        {
            try
            {
                string tessdataPath = "../Jamper-Financial.Shared/tessdata";

                // Simulate extracted text for testing purposes
                //return @"
                //    Some Store Name
                //    123 Main St
                //    Calgary, AB T2T 2T2
                //    --------------------
                //    Item 1          $10.00
                //    Subtotal        $10.00
                //    GST 5%            $0.50
                //    Total Due       $10.50
                //    Payment: Debit    $10.50
                //    2025-03-04 18:30:00
                //    Thank you!
                //    ";
                if (!Directory.Exists(tessdataPath))
                {
                    Console.WriteLine($"Tessdata directory not found: {tessdataPath}");
                    return string.Empty;
                }

                using (var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default)) // Replace with your tessdata path and language
                {
                    using (var img = Pix.LoadFromFile(imagePath))
                    {
                        using (var page = engine.Process(img))
                        {
                            string text = page.GetText();
                            return text;
                        }
                    }
                }
            }
            catch (TesseractException e)
            {
                Console.WriteLine($"Tesseract Error: {e.Message}");
                return string.Empty;
            }
            catch (Exception e)
            {
                Console.WriteLine($"General Error: {e.Message}");
                return string.Empty;
            }
        }

        public static Transaction ParseTransactionsSingleLoop(string imagePath)
        {
            try
            {
                Console.WriteLine($"Parsing image: {imagePath}");
                string extractedText = ExtractTextFromImage(imagePath);
                if (string.IsNullOrEmpty(extractedText))
                {
                    return null;
                }

                string[] lines = extractedText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                DateTime transactionDate = DateTime.MinValue;
                decimal grandTotalAmount = 0;
                string grandTotalLine = null;
                string description = null;
                string firstLine = null;
                if (lines.Length > 0)
                {
                    firstLine = lines[0];
                    Console.WriteLine($"First line: {firstLine}");
                }


                // --- First loop: Find the most likely Grand Total line and the latest date ---
                foreach (string line in lines)
                {
                    try
                    {
                        Console.WriteLine(line);
                        // --- Attempt to identify the description (store name) --- (optional)
                        if (description == null && !string.IsNullOrWhiteSpace(firstLine) && !IsLikelyFinancialLine(firstLine))
                        {
                            description = firstLine.Trim();
                            Console.WriteLine($"Potential Description: {description}");
                        }

                        string trimmedLine = line.Trim().ToLower();

                        // --- Attempt to parse the date (keep finding the latest) ---
                        Match dateMatch = Regex.Match(line, @"(\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}[/-]\d{1,2}[/-]\d{1,2})");
                        if (dateMatch.Success)
                        {
                            string dateString = dateMatch.Value;
                            string[] dateFormats = { "MM/dd/yyyy", "MM/dd/yy", "dd/MM/yyyy", "dd/MM/yy",
                                                        "yyyy/MM/dd", "yy/MM/dd", "MM-dd-yyyy", "MM-dd-yy",
                                                        "dd-MM-yyyy", "dd-MM-yy", "yyyy-MM-dd", "yy-MM-dd" };

                            if (DateTime.TryParseExact(dateString, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                            {
                                if (parsedDate > transactionDate) // Update to the latest date found
                                {
                                    transactionDate = parsedDate;
                                }
                            }
                        }

                        // --- Identify the Grand Total line ---
                        if ((trimmedLine.Contains("grand total") ||
                             trimmedLine.Contains("total due") ||
                             trimmedLine.Contains("amount due") ||
                             trimmedLine.Contains("balance due") ||
                             (trimmedLine.Contains("total") && IsLikelyFinalTotal(lines, Array.IndexOf(lines, line)))) &&
                            (line.Contains("$") || line.Contains("CAD")))
                        {
                            grandTotalLine = line;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error processing line for Grand Total/Date '{line}': {e.Message}");
                    }
                }

                // --- Second step: Extract the amount from the identified Grand Total line ---
                if (!string.IsNullOrEmpty(grandTotalLine))
                {
                    Match amountMatch = Regex.Match(grandTotalLine, @"(?:[$]|CAD)?\s*(\d{1,}(?:,\d{3})*(?:\.\d{2})?)");
                    if (amountMatch.Success)
                    {
                        string amountString = amountMatch.Groups[1].Value.Replace(",", "");
                        if (decimal.TryParse(amountString, out decimal parsedAmount))
                        {
                            grandTotalAmount = parsedAmount;
                        }
                    }
                }

                // --- Create and return the Transaction with the Grand Total ---
                if (grandTotalAmount > 0)
                {
                    return new Transaction
                    {
                        Amount = grandTotalAmount,
                        Description = description,
                        CategoryID = 0,
                        TransactionType = "e",
                        Date = transactionDate != DateTime.MinValue ? transactionDate : DateTime.Now,
                        Frequency = null,
                        EndDate = DateTime.Now,
                        AccountID = 0,
                        IsPaid = true,
                        HasReceipt = true,
                    };
                }

                return null; // Return null if no Grand Total is found
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error parsing transactions: {e.Message}");
                return null;
            }
        }

        private static bool IsLikelyFinancialLine(string line)
        {
            return line.Contains("$") || line.Contains("CAD") || Regex.IsMatch(line, @"\d+[/.-]\d+[/.-]\d+") || Regex.IsMatch(line, @"\b(subtotal|total|gst|hst|tax|amount due|balance)\b", RegexOptions.IgnoreCase);
        }

        private static bool IsLikelyFinalTotal(string[] allLines, int currentIndex)
        {
            // Check if it's near the end of the lines that might contain financial info
            // Adjust the buffer based on typical receipt structure
            if (currentIndex > allLines.Length - 5)
            {
                return true;
            }

            // You could add more sophisticated checks here, like looking for payment information below.

            return false;
        }
    }
}