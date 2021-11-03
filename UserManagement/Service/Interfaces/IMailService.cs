using Data.ViewModels;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IMailService
    {
        Task<bool> SendEmail(EmailViewModel email);
    }
}
