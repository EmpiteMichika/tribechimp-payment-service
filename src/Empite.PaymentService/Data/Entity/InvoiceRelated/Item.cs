using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Empite.PaymentService.Data.Entity.InvoiceRelated
{
    public class Item: BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string ReferenceId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Rate { get; set; }
        public string ItemId { get; set; }
        public List<Item_Purchese> RecurringInvoices { get; set; }
    }
}
