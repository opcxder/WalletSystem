

namespace WalletSystem.Core.Interfaces.Services
{
    public  interface IEmailService
    {
        Task SendMailAsync(string toEmail , string subject , string htmlBody);
       
    }
}
