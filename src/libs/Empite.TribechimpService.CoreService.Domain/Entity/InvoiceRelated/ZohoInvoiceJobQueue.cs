using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated
{
    public class ZohoInvoiceJobQueue:BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string JsonData { get; set; }
        public ZohoInvoiceJobQueueType JobType { get; set; }
        public bool IsSuccess { get; set; }
        public int ReTryCount { get; set; } = 0;
        public string LastErrorMessage { get; set; }
    }

    public enum ZohoInvoiceJobQueueType
    {
        CreateContact = 1,
        EnablePaymentReminders = 3,
        CreateItem = 4,
        CreateInvoice = 6
    }
}
