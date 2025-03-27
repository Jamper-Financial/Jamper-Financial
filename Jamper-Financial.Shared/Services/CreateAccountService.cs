
using Jamper_Financial.Shared.Data;
using Microsoft.AspNetCore.Components;


namespace Jamper_Financial.Shared.Services
{
    public class CreateAccountService
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;


        // Inject NavigationManager
        [Inject]
        private NavigationManager Navigation { get; set; }

        // Navigate to login page
        public void NavigateToLoginPage()
        {
            Navigation.NavigateTo("/login");
        }

        // This handles users input during account creation
        public bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "Please fill in all fields.";
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return false;
            }

            // Validate password strength
            if (!IsPasswordStrong(Password))
            {
                ErrorMessage = "Password must be at least 8 characters long, include uppercase and lowercase letters, a digit, and a special character.";
                return false;
            }

            if (DatabaseHelper.IsUsernameTaken(Username))
            {
                ErrorMessage = "Username is already taken. Please choose another.";
                return false;
            }

            if (DatabaseHelper.IsEmailTaken(Email))
            {
                ErrorMessage = "You already have an account with this email. Please log in.";
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }

        private bool IsPasswordStrong(string password)
        {
            // Password must be at least 8 characters long, contain uppercase, lowercase, digit, and special character
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => "!@#$%^&*()_+-=[]{}|;:',.<>?/".Contains(ch));
        }

    }
}
