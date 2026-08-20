using System.Threading.Tasks;

namespace AuthWeb.Services
{
    public interface IEmailSender
    {
        Task SendEmailConfirmationAsync(string email, string name, string confirmationLink);
    }
}
