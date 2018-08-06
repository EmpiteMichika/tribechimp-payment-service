using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Empite.PaymentService.Services.PaymentService.Zoho
{
    #region response classes
    internal class RootZohoBasicResponse
    {
        public int code { get; set; } = -1;
        public string message { get; set; }
    }
    #region ContactResponse

    internal class RootContactResponse : RootZohoBasicResponse
    {
        public Contact contact { get; set; }
    }
    internal class Contact
    {
        public string contact_id { get; set; }
        public string primary_contact_id { get; set; }
    }

    #endregion

    #region ItemCreateResponse
    internal class ItemResponse
    {
        public string item_id { get; set; }
    }

    internal class RootItemCreateResponse : RootZohoBasicResponse
    {

        public ItemResponse item { get; set; }
    }


    #endregion



    #region InvoiceCreateResponse

    internal class RootInvoiceResponse : RootZohoBasicResponse
    {
        public SubInvoiceResponse invoice { get; set; }
    }

    internal class SubInvoiceResponse
    {
        public string invoice_id { get; set; }
        public string invoice_number { get; set; }
    }

    #endregion

    #endregion



    /// <summary>
    /// Region for Request classes
    /// </summary>
    #region Request Classes
    #region Create Contact

    internal class ContactPerson
    {
        //public string salutation { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        //public string phone { get; set; }
        public string mobile { get; set; }
        public bool is_primary_contact { get; set; }
    }

    internal class ContactPersonRoot
    {
        public string contact_name { get; set; }
        public List<ContactPerson> contact_persons { get; set; }
        public int payment_terms { get; set; }
    }

    #endregion


    #region Create Item
    internal class ZohoCreateItemReqeust
    {
        public string name { get; set; }
        public double rate { get; set; }
        public string description { get; set; }
        public string product_type { get; set; }
    }


    #endregion


    #region Create Recurring Purchese

    internal class LineItemRecurringInvoiceCreateRequest
    {

        public string item_id { get; set; }
        public int quantity { get; set; }
    }

    internal class PaymentGatewayRecurringInvoiceCreateRequest
    {

        public bool configured { get; set; }
        public string gateway_name { get; set; }
    }

    internal class PaymentOptionsRecurringInvoiceCreateRequest
    {
        public List<PaymentGatewayRecurringInvoiceCreateRequest> payment_gateways { get; set; }
    }



    internal class CommonRootRecurringInvoiceCreateRequest
    {

        public string customer_id { get; set; }
        public List<string> contact_persons { get; set; }

        public List<LineItemRecurringInvoiceCreateRequest> line_items { get; set; }
        public PaymentOptionsRecurringInvoiceCreateRequest payment_options { get; set; }
        public int payment_terms { get; set; }

    }

    internal class RootInvoiceCreateRequest : CommonRootRecurringInvoiceCreateRequest
    {
        public string recurring_invoice_id { get; set; }
        public string date { get; set; }
    }
    #endregion
    #endregion
}
