namespace Empite.PaymentService.Data.Entity.InvoiceRelated
{
    public class ZohoItem_Purchese
    {
        public string RecurringInvoiceId { get; set; }
        public Purchese Purchese { get; set; }

        public string ZohoItemId { get; set; }
        public ZohoItem ZohoItem { get; set; }

        public int Qty { get; set; }
    }
}
