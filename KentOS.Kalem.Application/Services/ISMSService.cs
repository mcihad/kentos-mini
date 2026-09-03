using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KentOS.Kalem.Application.Services
{
    public interface ISMSService
    {
        Task<bool> SendAsync(string token, string title, string body, string data);
    }
}
