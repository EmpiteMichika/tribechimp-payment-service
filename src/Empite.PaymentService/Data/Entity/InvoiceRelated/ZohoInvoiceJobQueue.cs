﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Empite.PaymentService.Data.Entity.InvoiceRelated
{
    public class ZohoInvoiceJobQueue:BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string JsonData { get; set; }
        public InvoiceJobQueueType JobType { get; set; }
        public bool IsSuccess { get; set; }
        public int ReTryCount { get; set; } = 0;
        public string LastErrorMessage { get; set; }
    }

    public enum InvoiceJobQueueType
    {
        CreateContact = 1,
        EnablePaymentReminders = 2,
        CreateItem = 3,
        CreateFirstInvoice = 4,
        CreateSubInvoice = 5
    }
}
