using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated
{
    public class Purchese: BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string InvoiceName { get; set; }
        public InvoiceContact InvoiceContact { get; set; }
        public List<ZohoItem_Purchese> ZohoItems { get; set; }
        public Guid? ReferenceGuid { get; set; }
        public bool IsPaidForThisMonth { get; set; } = false;
        public List<InvoiceHistory> InvoiceHistories { get; set; }
        public InvoicingType InvoiceType { get; set; } = InvoicingType.Recurring;
        public InvoicingStatus InvoiceStatus { get; set; } = InvoicingStatus.Active;
    }

    public enum InvoicingType
    {
        Recurring=1,
        Temporary=2
    }

    public enum InvoicingStatus
    {
        Active=1,
        Canceled=2,
        Paused = 3
    }
}
