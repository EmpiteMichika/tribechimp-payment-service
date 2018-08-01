using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Empite.PaymentService.Data.Entity.InvoiceRelated
{
    public class InvoiceHistory: BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public Purchese Purchese { get; set; }
        public DateTime? PaymentRecordedDate { get; set; }
        public InvoiceStatus InvoiceStatus { get; set; }
    }

    public enum InvoiceStatus
    {
        Paid=1,
        Unpaid=2,
        Canceled=3
    }
}
