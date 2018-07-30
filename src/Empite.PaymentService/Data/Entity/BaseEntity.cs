namespace Empite.PaymentService.Data.Entity
{
    public abstract class BaseEntity : BaseModel
    {
        public string OrganizationId { get; set; }
    }
}
