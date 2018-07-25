using System;
using System.Collections.Generic;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated
{
    public class ZohoItemRecurringInvoice
    {
        public string RecurringInvoiceId { get; set; }
        public Purchese Purchese { get; set; }

        public string ZohoItemId { get; set; }
        public ZohoItem ZohoItem { get; set; }

        public int Qty { get; set; }
    }
}
