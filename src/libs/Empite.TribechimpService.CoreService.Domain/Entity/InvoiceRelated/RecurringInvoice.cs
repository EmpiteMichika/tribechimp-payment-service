using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated
{
    public class RecurringInvoice: BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string RecurringInvoiceId { get; set; }
        public string RecurringInvoiceName { get; set; }
        public InvoiceContact InvoiceContact { get; set; }
        public List<ZohoItemRecurringInvoice> ZohoItems { get; set; }
        public bool IsDue { get; set; }
        public bool AllTaskCompleted { get; set; }
    }
}
