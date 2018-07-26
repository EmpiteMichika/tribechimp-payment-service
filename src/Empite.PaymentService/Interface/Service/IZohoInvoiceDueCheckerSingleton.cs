using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Empite.TribechimpService.PaymentService.Domain.Interface.Service
{
    public interface IZohoInvoiceDueCheckerSingleton
    {
        Task CheckInvoicesDueAsync();
    }
}
