using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Empite.PaymentService.Data.Entity.InvoiceRelated
{
    public class InvoiceContact: BaseEntity
    {
        [Key]
        public string UserId { get; set; }
        public string ExternalContactUserId { get; set; }
        public string ExternalPrimaryContactId { get; set; }
        public string Email { get; set; }
        public List<Purchese> Invoices { get; set; }
    }
}
