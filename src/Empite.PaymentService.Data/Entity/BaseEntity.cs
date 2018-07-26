namespace Empite.TribechimpService.PaymentService.Domain.Entity
{
    public abstract class BaseEntity : BaseModel
    {
        public string OrganizationId { get; set; }
    }
}
