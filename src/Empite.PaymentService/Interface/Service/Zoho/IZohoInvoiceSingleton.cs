using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Empite.PaymentService.Interface.Service.Zoho
{
    public interface IZohoInvoiceSingleton
    {
        Task<string> GetOAuthToken();
    }
}
