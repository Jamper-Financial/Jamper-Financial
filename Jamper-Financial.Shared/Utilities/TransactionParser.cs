using System.Globalization;
using System.Text.RegularExpressions;
using Tesseract;
using System.IO;

namespace Jamper_Financial.Shared.Utilities
{
    public class TransactionParser
    {
        public static string ExtractTextFromImage(string imagePath)
        {
            try
            {
                string tessdataPath = "../Jamper-Financial.Shared/tessdata";

                if (!Directory.Exists(tessdataPath))
                {
                    Console.WriteLine($"Tessdata directory not found: {tessdataPath}");
                    return string.Empty;
                }

                using (var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(imagePath))
                    {
                        using (var page = engine.Process(img, PageSegMode.Auto))
                        {
                            string text = page.GetText();
                            text = PostProcessText(text);
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

        public static Transaction? ParseTransactionsSingleLoop(string imagePath)
        {
            try
            {
                Console.WriteLine($"Parsing image: {imagePath}");
                string extractedText = ExtractTextFromImage(imagePath);
                Console.WriteLine($"Raw extracted text:\n{extractedText}"); // Add this line
                if (string.IsNullOrEmpty(extractedText))
                {
                    return null;
                }

                string[] lines = extractedText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                DateTime transactionDate = DateTime.MinValue;
                decimal grandTotalAmount = 0;
                string? grandTotalLine = null;
                string? description = null;
                string? firstLine = null;

                if (lines.Length > 0)
                {
                    firstLine = lines[0];
                    Console.WriteLine($"First line: \n {firstLine}");

                    // Handle the case where the receipt is mostly on a single line
                    if (lines.Length <= 2 && !string.IsNullOrWhiteSpace(firstLine)) // Adjust '2' based on typical minimal lines
                    {
                        description = firstLine.Length > 25 ? firstLine.Substring(0, 25).Trim() : firstLine.Trim();
                        Console.WriteLine($"Single-line potential description: {description}");
                    }
                }

                foreach (string line in lines)
                {
                    try
                    {
                        Console.WriteLine("Line Texts " + line.Trim() + " Description " + description);

                        if (description == null && !string.IsNullOrWhiteSpace(firstLine) && !IsLikelyFinancialLine(firstLine))
                        {
                            description = firstLine.Trim();
                            Console.WriteLine($"Potential Description: {description}");
                        }

                        string trimmedLine = line.Trim().ToLower();

                        Match dateMatch = Regex.Match(line, @"(\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}[/-]\d{1,2}[/-]\d{1,2})");
                        if (dateMatch.Success)
                        {
                            string dateString = dateMatch.Value;
                            string[] dateFormats = { "MM/dd/yyyy", "MM/dd/yy", "dd/MM/yyyy", "dd/MM/yy",
                                                     "yyyy/MM/dd", "yy/MM/dd", "MM-dd-yyyy", "MM-dd-yy",
                                                     "dd-MM-yyyy", "dd-MM-yy", "yyyy-MM-dd", "yy-MM-dd" };

                            if (DateTime.TryParseExact(dateString, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                            {
                                if (parsedDate > transactionDate)
                                {
                                    transactionDate = parsedDate;
                                }
                            }
                        }

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
                if (description == null && lines.Length > 0 && !IsLikelyFinancialLine(firstLine))
                {
                    description = firstLine.Trim();
                    Console.WriteLine($"Potential Description (after loop): {description}");
                }

                if (string.IsNullOrEmpty(description) && lines.Length > 0)
                {
                    description = lines[0].Length > 25 ? lines[0].Substring(0, 25).Trim() : lines[0].Trim();
                    Console.WriteLine($"Fallback Description (first 25 chars): {description}");
                }


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

                return new Transaction
                {
                    Amount = grandTotalAmount,
                    Description = description ?? string.Empty,
                    CategoryID = 0,
                    TransactionType = "e",
                    Date = transactionDate != DateTime.MinValue ? transactionDate : DateTime.Now,
                    Frequency = null,
                    EndDate = transactionDate != DateTime.MinValue ? transactionDate : DateTime.Now,
                    AccountID = 0,
                };
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
            if (currentIndex > allLines.Length - 5)
            {
                return true;
            }
            return false;
        }

        private static string PostProcessText(string text)
        {
            string processedText = Regex.Replace(text, @"(\d{1,2}[/-]\d{1,2}[/-]\d{2,4}|\d{4}[/-]\d{1,2}[/-]\d{1,2})", "\n$1");
            processedText = Regex.Replace(processedText, @"(\$?\d{1,}(?:,\d{3})*(?:\.\d{2})?)", "\n$1");

            if (!processedText.Contains(Environment.NewLine))
            {
                processedText = Regex.Replace(processedText, @"(?<=[a-zA-Z])(?=\d)", "\n");
                processedText = Regex.Replace(processedText, @"(?<=\d)(?=[a-zA-Z])", "\n");
                processedText = Regex.Replace(processedText, @"\s{2,}", "\n");

                // Add more specific rules based on the structure of this receipt
                processedText = Regex.Replace(processedText, @"(SUBTOTAL|GST|TOTAL|VISA TEND|CHANGE DUE)\s+(\$?\d)", "\n$1 $2", RegexOptions.IgnoreCase);
                processedText = Regex.Replace(processedText, @"([a-zA-Z0-9]{3,})\s+(\$?\d+\.\d{2})", "$1\n$2"); // Split between potential item and price
            }

            return processedText;
        }
    }
}
