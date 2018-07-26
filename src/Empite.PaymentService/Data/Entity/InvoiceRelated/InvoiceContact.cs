using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Empite.TribechimpService.PaymentService.Domain.Entity;

namespace Empite.PaymentService.Data.Entity.InvoiceRelated
{
    public class InvoiceContact: BaseEntity
    {
        [Key]
        public string UserId { get; set; }
        public string ZohoContactUserId { get; set; }
        public string ZohoPrimaryContactId { get; set; }
        public string Email { get; set; }
        public List<Purchese> Invoices { get; set; }
    }
}
