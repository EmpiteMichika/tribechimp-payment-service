using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Empite.PaymentService.Data.Entity.InvoiceRelated
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
