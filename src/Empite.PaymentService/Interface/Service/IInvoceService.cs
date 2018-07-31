using System.Threading.Tasks;
using Empite.PaymentService.Data.Entity.InvoiceRelated;

namespace Empite.PaymentService.Interface.Service
{
    public interface IInvoceService
    {
        Task RunJobs();
        Task AddJob(dynamic DataObject, InvoiceJobQueueType JobType, ExternalInvoiceGatewayType externalInvoiceGatewayType);
    }
}
