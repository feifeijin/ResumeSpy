using System.Threading;
using System.Threading.Tasks;
using ResumeSpy.Core.Models.Email;

namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IEmailSender
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
