using System.Threading.Tasks;

namespace Empite.PaymentService.Interface.Service.Zoho
{
    public interface IRecurringInvoiceService<T>
    {
        Task CheckInvoicesDueAsync();
        Task CreateRecurringInvoice();
    }
}
