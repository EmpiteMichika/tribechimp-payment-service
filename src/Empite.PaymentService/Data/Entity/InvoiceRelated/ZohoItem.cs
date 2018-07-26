using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Empite.TribechimpService.PaymentService.Domain.Entity;

namespace Empite.PaymentService.Data.Entity.InvoiceRelated
{
    public class ZohoItem: BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Rate { get; set; }
        public string ZohoItemId { get; set; }
        public List<ZohoItem_Purchese> RecurringInvoices { get; set; }
    }
}
