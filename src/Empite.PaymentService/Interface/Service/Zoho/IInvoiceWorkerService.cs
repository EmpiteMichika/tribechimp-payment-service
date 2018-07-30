using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Models.Dto;

namespace Empite.PaymentService.Interface.Service.Zoho
{
    public interface IInvoiceWorkerService<T>
    {
        Task<InvoiceContact> CreateContact(CreateContact model, ApplicationDbContext _dbContext);
        Task<bool> EnablePaymentReminder(InvoiceContact contactDetails);
        Task<ZohoItem> CreateItem(CreateZohoItemDto model, ApplicationDbContext _dbContext);
        Task DeleteInvoice(string invoiceId);
        Task<bool> CreateInvoice(CreatePurchesDto model, ApplicationDbContext dbContext, bool isFirst = false);
    }
}