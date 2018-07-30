using System.Threading.Tasks;

namespace Empite.PaymentService.Interface.Service.Zoho
{
    public interface IZohoInvoiceDueCheckerSingleton
    {
        Task CheckInvoicesDueAsync();
    }
}
