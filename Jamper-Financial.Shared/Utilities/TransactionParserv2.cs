using System.Globalization;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.Runtime;
using Amazon.Textract;
using Amazon.Textract.Model;

namespace Jamper_Financial.Shared.Utilities
{

    //NOTE USE THIS FOR HIGHER ACCURACY, USE THE OTHER PARSER FOR FREE BUT LOWER ACCURACY
    public class TransactionParser2
    {
        private static AmazonTextractClient GetTextractClient()
        {
            // Replace with your AWS credentials and region
            var credentials = new BasicAWSCredentials("AKIAWAGWZ7XHNKY4W2HY", "iFkBzVgavA6x+1ToFn+AvPPcehvJU/MAs8O6MOqe");
            var region = RegionEndpoint.CACentral1; // Example: Canada Central

            return new AmazonTextractClient(credentials, region);
        }

        public static async Task<string> ExtractTextFromImageTextractAsync(string imagePath)
        {
            try
            {
                using (var client = GetTextractClient())
                {
                    using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                    {
                        var imageBytes = new byte[fs.Length];
                        fs.Read(imageBytes, 0, (int)fs.Length);

                        var request = new DetectDocumentTextRequest
                        {
                            Document = new Document
                            {
                                Bytes = new MemoryStream(imageBytes)
                            }
                        };

                        var response = await client.DetectDocumentTextAsync(request);

                        if (response?.Blocks != null)
                        {
                            return string.Join(Environment.NewLine, response.Blocks
                                .Where(b => b.BlockType == BlockType.LINE)
                                .Select(b => b.Text));
                        }

                        return string.Empty;
                    }
                }
            }
            catch (AmazonServiceException e)
            {
                Console.WriteLine($"Amazon Textract Service Error: {e.Message}");
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
            string extractedText = Task.Run(() => ExtractTextFromImageTextractAsync(imagePath)).GetAwaiter().GetResult();

            try
            {
                Console.WriteLine($"Parsing image with Textract: {imagePath}");
                Console.WriteLine($"Raw extracted text from Textract:\n{extractedText}");
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

                    if (lines.Length <= 2 && !string.IsNullOrWhiteSpace(firstLine))
                    {
                        description = firstLine.Length > 25 ? firstLine.Substring(0, 25).Trim() : firstLine.Trim();
                        Console.WriteLine($"Single-line potential description: {description}");
                    }
                }

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
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

                        bool isTotalLine = trimmedLine.Contains("grand total") ||
                                           trimmedLine.Contains("total due") ||
                                           trimmedLine.Contains("amount due") ||
                                           trimmedLine.Contains("balance due") ||
                                           trimmedLine.Contains("total paid") ||
                                           (trimmedLine == "total" && IsLikelyFinalTotal(lines, i)) || // Check for standalone "total"
                                           (trimmedLine == "total:" && IsLikelyFinalTotal(lines, i)) || // Check for "total:"
                                           (trimmedLine == "total $" && IsLikelyFinalTotal(lines, i)); // Check for "total $"

                        if (isTotalLine && (line.Contains("$") || line.Contains("CAD")))
                        {
                            grandTotalLine = line;
                        }
                        else if ((trimmedLine.Contains("total paid") || trimmedLine == "total" || trimmedLine == "total:" || trimmedLine == "total $") &&
                                 !(line.Contains("$") || line.Contains("CAD")) && i + 1 < lines.Length)
                        {
                            string nextLine = lines[i + 1].Trim();
                            Match amountMatchOnNextLine = Regex.Match(nextLine, @"(?:[$]|CAD)?\s*(\d{1,}(?:,\d{3})*(?:\.\d{2})?)");
                            if (amountMatchOnNextLine.Success)
                            {
                                grandTotalLine = nextLine;
                                i++;
                            }
                        }
                        else if (isTotalLine && grandTotalLine == null && i + 1 < lines.Length)
                        {
                            string nextLine = lines[i + 1].Trim();
                            Match amountMatchOnNextLine = Regex.Match(nextLine, @"(?:[$]|CAD)?\s*(\d{1,}(?:,\d{3})*(?:\.\d{2})?)");
                            if (amountMatchOnNextLine.Success)
                            {
                                grandTotalLine = nextLine;
                                i++;
                            }
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
    }
}