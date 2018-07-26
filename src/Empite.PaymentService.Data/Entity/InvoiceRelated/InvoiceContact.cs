using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated
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
