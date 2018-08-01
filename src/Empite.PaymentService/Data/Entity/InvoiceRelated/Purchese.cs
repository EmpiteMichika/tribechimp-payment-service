using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Empite.PaymentService.Data.Entity.InvoiceRelated
{
    public class Purchese: BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string InvoiceName { get; set; }
        public InvoiceContact InvoiceContact { get; set; }
        public List<Item_Purchese> Items { get; set; }
        public Guid? ReferenceGuid { get; set; }
        public List<InvoiceHistory> InvoiceHistories { get; set; }
        public InvoicingType InvoiceType { get; set; } = InvoicingType.Recurring;
        public InvoicingStatus InvoiceStatus { get; set; } = InvoicingStatus.Active;
        public ExternalInvoiceGatewayType InvoiceGatewayType { get; set; }
        public DateTime LastSuccessPayment { get; set; } = DateTime.MinValue;
        public DateTime LastSuccessInvoiceIssue { get; set; } = DateTime.MinValue;
        
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
