using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jamper_Financial.Shared.Models
{
    public class BankAccount
    {
        public int AccountId { get; set; }
        public String AccountType { get; set; }

        [Required(ErrorMessage = "Account Name is required.")]
        public string AccountName { get; set; }
        public int Balance { get; set; } = 0;
        public string AccountNumber { get; set; }
        public int UserId { get; set; }

    }

}
