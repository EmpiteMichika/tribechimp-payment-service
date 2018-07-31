namespace Empite.PaymentService.Models.Dto.Zoho
{
    public class ZohoCreateItemDto
    {
        public string Name { get; set; }
        public double Rate { get; set; }
        public string Description { get; set; }
        public ZohoItemProductType ProdcuType { get; set; }
    }
    public enum ZohoItemProductType {
        service = 1,
        goods =2
    }
}
