using Jamper_Financial.Shared.Data;
using Jamper_Financial.Shared.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Jamper_Financial.Shared.Utilities
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Category { get; set; }
        public string Color { get; set; }
        public string Frequency { get; set; }
        public DateTime? EndDate { get; set; }
        public int CategoryID { get; set; } = 0;
        public int HasReceipt { get; set; } = 0;
    }

    public static class TransactionManager
    {
        public static async Task<List<Transaction>> LoadTransactionsAsync()
        {
            return (await TransactionHelper.GetTransactionsAsync()).ToList();
        }

        public static async Task AddOrUpdateTransactionAsync(Transaction transaction, bool isEditMode)
        {
            if (transaction.Category == "Expenses" || transaction.Category == "Debts & Loans" || transaction.Category == "Subscriptions & Memberships")
            {
                transaction.Debit = transaction.Amount;
                transaction.Credit = 0;
            }
            else
            {
                transaction.Debit = 0;
                transaction.Credit = transaction.Amount;
            }

            if (transaction.Frequency == "None")
            {
                transaction.Frequency = null;
                transaction.EndDate = null;
            }

            if (isEditMode)
            {
                await TransactionHelper.UpdateTransactionAsync(transaction);
                if (transaction.Frequency == null)
                {
                    RemoveRecurringTransactions(transaction);
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
            RemoveRecurringTransactions(transaction);
        }

        private static void EditRecurringTransactions(Transaction transaction)
        {
            var recurringTransactions = TransactionHelper.GetTransactionsAsync().Result
                .Where(t => t.Description == transaction.Description &&
                            t.Category == transaction.Category &&
                            t.Color == transaction.Color &&
                            t.Frequency == transaction.Frequency &&
                            t.Date > transaction.Date)
                .ToList();

            foreach (var recurringTransaction in recurringTransactions)
            {
                recurringTransaction.Description = transaction.Description;
                recurringTransaction.Amount = transaction.Amount;
                recurringTransaction.Debit = transaction.Debit;
                recurringTransaction.Credit = transaction.Credit;
                recurringTransaction.Category = transaction.Category;
                recurringTransaction.Color = transaction.Color;
                recurringTransaction.Frequency = transaction.Frequency;
                recurringTransaction.EndDate = transaction.EndDate;

                TransactionHelper.UpdateTransactionAsync(recurringTransaction).Wait();
            }
        }

        private static void RemoveRecurringTransactions(Transaction transaction)
        {
            var recurringTransactions = TransactionHelper.GetTransactionsAsync().Result
                .Where(t => t.Description == transaction.Description &&
                            t.Category == transaction.Category &&
                            t.Color == transaction.Color &&
                            t.Frequency == transaction.Frequency &&
                            t.Date > transaction.Date)
                .ToList();

            foreach (var recurringTransaction in recurringTransactions)
            {
                TransactionHelper.DeleteTransactionAsync(recurringTransaction.TransactionID).Wait();
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

                var existingTransaction = TransactionHelper.GetTransactionsAsync().Result
                    .FirstOrDefault(t => t.Date == nextDate && t.Description == transaction.Description && t.Category == transaction.Category);

                if (existingTransaction == null)
                {
                    var newTransaction = new Transaction
                    {
                        Date = nextDate,
                        Description = transaction.Description,
                        Amount = transaction.Amount,
                        Debit = transaction.Debit,
                        Credit = transaction.Credit,
                        Category = transaction.Category,
                        Color = transaction.Color,
                        Frequency = transaction.Frequency,
                        EndDate = transaction.EndDate
                    };

                    TransactionHelper.AddTransactionAsync(newTransaction).Wait();
                }
                else if (isEditing)
                {
                    existingTransaction.Description = transaction.Description;
                    existingTransaction.Amount = transaction.Amount;
                    existingTransaction.Debit = transaction.Debit;
                    existingTransaction.Credit = transaction.Credit;
                    existingTransaction.Category = transaction.Category;
                    existingTransaction.Color = transaction.Color;
                    existingTransaction.Frequency = transaction.Frequency;
                    existingTransaction.EndDate = transaction.EndDate;

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
            transaction.HasReceipt = 1;

            // Update the transaction in the database
            await TransactionHelper.UpdateTransactionAsync(transaction);
                        return true;
            } catch (Exception ex) {
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