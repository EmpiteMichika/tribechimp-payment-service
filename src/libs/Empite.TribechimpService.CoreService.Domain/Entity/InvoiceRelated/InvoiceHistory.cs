using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using RawRabbit.Operations.Publish.Context;

namespace Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated
{
    public class InvoiceHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string ZohoInvoiceId { get; set; }
        public Invoice RecurringInvoice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime PaymentRecordedDate { get; set; }
        public InvoiceStatus InvoiceStatus { get; set; }
    }

    public enum InvoiceStatus
    {
        Paid=1,
        Unpaid=2,
        Canceled=3
    }
}
