using System;
using System.ComponentModel.DataAnnotations;

namespace Jamper_Financial.Shared.Models
{
    public class BankAccount
    {
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Account Type is required.")]
        [CustomAccountTypeValidation]
        public int AccountTypeID { get; set; }

        [Required(ErrorMessage = "Account Name is required.")]
        public string AccountName { get; set; }

        public int Balance { get; set; } = 0;
        public string AccountNumber { get; set; } = string.Empty;
        public int UserId { get; set; }
    }

    public class BankAccountType
    {
        public int AccountTypeId { get; set; }
        public string AccountTypeName { get; set; }
    }

    public class CustomAccountTypeValidationAttribute : ValidationAttribute
    {
        public CustomAccountTypeValidationAttribute()
        {
            ErrorMessage = "Account Type is required.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if ((int)value == 0)
            {
                // This is the key change: return a validation error if the value is 0 or null.
                return new ValidationResult(ErrorMessage);
            }

            // If the value is not null and not 0, it's valid.
            return ValidationResult.Success;
        }
    }
}