using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Entity.InvoiceRelated
{
   public class ConfiguredPaymentGateway
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string GatewayName { get; set; }
        public bool IsEnabled { get; set; }
    }
}
