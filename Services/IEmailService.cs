namespace StrayCareAPI.Services
{
    public interface IEmailService
    {
        Task SendOtpAsync(string email, string otpCode);
        Task SendStatusUpdateAsync(string email, string subject, string statusMessage, string userName);
    }
}
