using Fydar.Dev.Services.EmailTickets.Models;
using System.Threading.Tasks;

namespace Fydar.Dev.Lambda.EmailToTicket.Services;

public interface IEmailSinkService
{
    public Task<bool> ForwardEmailAsync(
        EmailModel email);
}
