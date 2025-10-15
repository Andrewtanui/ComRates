namespace TanuiApp.Services
{
    public interface IOtpService
    {
        string GenerateOTP();
        bool ValidateOTP(string storedOTP, DateTime? expiryTime, string enteredOTP);
    }
}
