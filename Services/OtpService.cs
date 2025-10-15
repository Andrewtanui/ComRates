namespace TanuiApp.Services
{
    public class OtpService : IOtpService
    {
        private readonly Random _random = new Random();

        public string GenerateOTP()
        {
            // Generate a 6-digit OTP
            return _random.Next(100000, 999999).ToString();
        }

        public bool ValidateOTP(string storedOTP, DateTime? expiryTime, string enteredOTP)
        {
            if (string.IsNullOrEmpty(storedOTP) || string.IsNullOrEmpty(enteredOTP))
                return false;

            if (!expiryTime.HasValue || DateTime.Now > expiryTime.Value)
                return false;

            return storedOTP.Equals(enteredOTP, StringComparison.Ordinal);
        }
    }
}
