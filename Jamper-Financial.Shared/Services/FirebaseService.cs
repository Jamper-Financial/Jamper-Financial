using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace Jamper_Financial.Shared.Services
{
    public class FirebaseService
    {
        public FirebaseService()
        {
            // Initialize Firebase App if not already initialized
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile("wwwroot/credentials/jamper-finance-firebase-adminsdk-dsr42-13bb4f4464.json") // U
                });
            }
        }

        // Verify Firebase ID Token
        public async Task<string> VerifyTokenAsync(string idToken)
        {
            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                return decodedToken.Uid; // Return the user ID
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid token.", ex);
            }
        }

        // Create a New User
        public async Task<string> CreateUserAsync(string email, string password)
        {
            var args = new UserRecordArgs
            {
                Email = email,
                Password = password
            };

            var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
            return userRecord.Uid; // Return the user ID
        }

        // Retrieve User Information by UID
        public async Task<UserRecord> GetUserByUidAsync(string uid)
        {
            return await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
        }

        // Delete user by UID
    public async Task<bool> DeleteUserAsync(string uid)
        {
            try
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user: {ex.Message}");
                return false;
            }
        }
    }
}