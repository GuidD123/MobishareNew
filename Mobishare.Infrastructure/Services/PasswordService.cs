using Microsoft.AspNetCore.Identity;

namespace Mobishare.Infrastructure.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<string> _passwordHasher = new(); 

        public string Hash(string password)
        {
            return _passwordHasher.HashPassword("user", password); 
        }

        public bool Verify(string password, string hashedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword("user", hashedPassword, password);
            return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
