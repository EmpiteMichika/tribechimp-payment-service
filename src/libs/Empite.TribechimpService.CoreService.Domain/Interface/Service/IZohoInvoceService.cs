using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated;

namespace Empite.TribechimpService.PaymentService.Domain.Interface.Service
{
    public interface IZohoInvoceService
    {
        Task RunJobs();
        Task AddJob(dynamic DataObject, ZohoInvoiceJobQueueType JobType);
    }
}
