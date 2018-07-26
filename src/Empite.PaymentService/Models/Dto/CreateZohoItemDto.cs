using System;
using System.Collections.Generic;
using System.Text;

namespace Empite.TribechimpService.PaymentService.Domain.Dto
{
    public class CreateZohoItemDto
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
