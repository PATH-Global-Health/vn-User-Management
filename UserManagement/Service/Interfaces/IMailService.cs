using Data.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IMailService
    {
        Task<bool> SendEmail(EmailViewModel email);
    }
}
