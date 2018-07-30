using System;
using System.Collections.Generic;

namespace Empite.PaymentService.Models.Dto
{
    public class CreatePurchesDto
    {
        public string UserId { get; set; }
        public List<RecurringInvoiceItemDto> Items { get; set; }
        public Guid? ReferenceGuid { get; set; }
    }

    public class RecurringInvoiceItemDto
    {
        public string ItemId { get; set; }
        public int Qty { get; set; }
    }
}
