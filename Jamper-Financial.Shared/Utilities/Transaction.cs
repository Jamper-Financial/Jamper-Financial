using Jamper_Financial.Shared.Data;
using Jamper_Financial.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jamper_Financial.Shared.Utilities
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public int UserID { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0;
        public int CategoryID { get; set; }
        public string TransactionType { get; set; } = "e"; // Expense by default
        public bool HasReceipt { get; set; } = false;
        public string? Frequency { get; set; } = null;
        public DateTime? EndDate { get; set; }
        public int AccountID { get; set; }
        public bool IsPaid { get; set; } = true;


    }

    public static class TransactionManager
    {
        public static async Task<List<Transaction>> LoadTransactionsAsync(int userId)
        {
            return (await TransactionHelper.GetTransactionsAsync(userId)).ToList();
        }

        public static async Task AddOrUpdateTransactionAsync(Transaction transaction, bool isEditMode)
        {
            Console.WriteLine("Check if it's looping here " + isEditMode);
            if (isEditMode)
            {
                await TransactionHelper.UpdateTransactionAsync(transaction);
                if (transaction.Frequency == null)
                {
                    await DeleteRecurringTransactionsAsync(transaction);
                }
                else
                {
                    EditRecurringTransactions(transaction);
                }

                if (transaction.Frequency != null && transaction.EndDate.HasValue)
                {
                    AddRecurringTransactions(transaction, true);
                }
            }
            else
            {
                await TransactionHelper.AddTransactionAsync(transaction);
                if (transaction.Frequency != null && transaction.EndDate.HasValue)
                {
                    AddRecurringTransactions(transaction, false);
                }
            }
        }

        public static async Task DeleteTransactionAsync(Transaction transaction)
        {
            await TransactionHelper.DeleteTransactionAsync(transaction.TransactionID);
        }

        public static async Task DeleteRecurringTransactionsAsync(Transaction transaction)
        {
            var transactions = await TransactionHelper.GetTransactionsAsync(transaction.UserID);
            var recurringTransactions = transactions
                .Where(t => t.Description == transaction.Description &&
                            t.CategoryID == transaction.CategoryID &&
                            t.Frequency == transaction.Frequency)
                .ToList();

            foreach (var recurringTransaction in recurringTransactions)
            {
                await TransactionHelper.DeleteTransactionAsync(recurringTransaction.TransactionID);
            }
        }

        private static void EditRecurringTransactions(Transaction transaction)
        {
            var recurringTransactions = TransactionHelper.GetTransactionsAsync(transaction.UserID).Result
                .Where(t => t.Description == transaction.Description &&
                            t.CategoryID == transaction.CategoryID &&
                            t.Frequency == transaction.Frequency &&
                            t.Date > transaction.Date)
                .ToList();

            foreach (var recurringTransaction in recurringTransactions)
            {
                recurringTransaction.Description = transaction.Description;
                recurringTransaction.Amount = transaction.Amount;
                //recurringTransaction.Debit = transaction.Debit;
                //recurringTransaction.Credit = transaction.Credit;
                recurringTransaction.CategoryID = transaction.CategoryID;
                recurringTransaction.Frequency = transaction.Frequency;
                recurringTransaction.EndDate = transaction.EndDate;
                recurringTransaction.AccountID = transaction.AccountID;

                TransactionHelper.UpdateTransactionAsync(recurringTransaction).Wait();
            }
        }

        private static void AddRecurringTransactions(Transaction transaction, bool isEditing)
        {
            
            DateTime nextDate = transaction.Date;
            while (true)
            {
                nextDate = transaction.Frequency switch
                {
                    "Monthly" => nextDate.AddMonths(1),
                    "Yearly" => nextDate.AddYears(1),
                    _ => nextDate
                };

                if (nextDate > transaction.EndDate.Value)
                {
                    break;
                }

                var existingTransaction = TransactionHelper.GetTransactionsAsync(transaction.UserID).Result
                    .FirstOrDefault(t => t.Date == nextDate && t.Description == transaction.Description && t.CategoryID == transaction.CategoryID);

                if (existingTransaction == null)
                {
                    Console.WriteLine("Get Next Date: " + nextDate);
                    var newTransaction = new Transaction
                    {
                        Date = nextDate,
                        Description = transaction.Description,
                        Amount = transaction.Amount,
                        CategoryID = transaction.CategoryID,
                        Frequency = transaction.Frequency,
                        EndDate = transaction.EndDate,
                        AccountID = transaction.AccountID,
                        UserID = transaction.UserID,
                        TransactionType = transaction.TransactionType,
                        IsPaid = false // For recurring transactions, set Paid to 0 (no) by default
                    };

                    TransactionHelper.AddTransactionAsync(newTransaction).Wait();
                }
                else if (isEditing)
                {
                    existingTransaction.Description = transaction.Description;
                    existingTransaction.Amount = transaction.Amount;
                    existingTransaction.CategoryID = transaction.CategoryID;
                    existingTransaction.Frequency = transaction.Frequency;
                    existingTransaction.EndDate = transaction.EndDate;
                    existingTransaction.AccountID = transaction.AccountID;
                    existingTransaction.UserID = transaction.UserID;
                    existingTransaction.TransactionType = transaction.TransactionType;
                    existingTransaction.IsPaid = transaction.IsPaid;

                    TransactionHelper.UpdateTransactionAsync(existingTransaction).Wait();
                }
            }
        }

        public static async Task<bool> UpdateReceiptAsync(Transaction transaction, IBrowserFile file)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.OpenReadStream().CopyToAsync(stream);
                    var receiptData = new ReceiptData
                    {
                        ReceiptDescription = transaction.Description,
                        ReceiptFileData = stream.ToArray(),
                        TransactionID = transaction.TransactionID
                    };

                    // Save the receipt data to the database
                    await TransactionHelper.AddOrUpdateReceiptAsync(receiptData);
                }

                // Update the transaction's receipt status in the database
                transaction.HasReceipt = true;

                // Update the transaction in the database
                await TransactionHelper.UpdateTransactionAsync(transaction);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;

            }
        }

        public static async Task<ReceiptData> GetReceiptAsync(Transaction transaction)
        {
            ReceiptData receiptData = await TransactionHelper.GetReceiptAsync(transaction.TransactionID);

            return receiptData;
        }
    }
}