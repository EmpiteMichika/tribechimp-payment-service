namespace Empite.PaymentService.Data.Entity.InvoiceRelated
{
    public class Item_Purchese
    {
        public string RecurringInvoiceId { get; set; }
        public Purchese Purchese { get; set; }

        public string ItemId { get; set; }
        public Item Item { get; set; }

        public int Qty { get; set; }
    }
}
