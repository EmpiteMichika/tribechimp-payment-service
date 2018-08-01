using System.Threading.Tasks;
using Empite.PaymentService.Data;
using Empite.PaymentService.Data.Entity.InvoiceRelated;
using Empite.PaymentService.Models.Dto;
using Empite.PaymentService.Models.Dto.Zoho;

namespace Empite.PaymentService.Interface.Service
{
    public interface IInvoiceWorkerService<T>
    {
        Task<InvoiceContact> CreateContact(ZohoCreateContact model, ApplicationDbContext _dbContext);
        Task<bool> EnablePaymentReminder(InvoiceContact contactDetails);
        Task<Item> CreateItem(ZohoCreateItemDto model, ApplicationDbContext _dbContext);
        Task DeleteInvoice(string invoiceId);
        Task<bool> CreateInvoice(InvoiceJobQueue job,ZohoCreatePurchesDto model, ApplicationDbContext dbContext);
        Task<bool> CreateSubInvoice(InvoiceJobQueue job, string purchaseId, ApplicationDbContext dbContext);
        Task<bool> IsPaidForCurrentMonth(string purchaseId);
    }
}