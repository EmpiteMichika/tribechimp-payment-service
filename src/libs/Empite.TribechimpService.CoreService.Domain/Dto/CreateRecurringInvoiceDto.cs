﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Dto
{
    public class CreateRecurringInvoiceDto
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
