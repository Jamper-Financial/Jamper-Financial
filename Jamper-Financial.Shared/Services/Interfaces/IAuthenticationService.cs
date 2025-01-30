namespace Jamper_Financial.Shared.Services
{
    public interface IAuthenticationService
    {
        Task CreateUserAccount(string firstName, string lastName, string username, DateTime birthDate, string email, string password);
        Task<bool> IsUsernameTaken(string username);
        Task<bool> IsEmailTaken(string email);
        Task<bool> ValidateUserCredentials(string identifier, string password);
        Task<bool> DeleteUser(string username, string email);
    }
}
