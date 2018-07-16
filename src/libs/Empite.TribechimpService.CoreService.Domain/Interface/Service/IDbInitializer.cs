using System.Threading.Tasks;

namespace Empite.TribechimpService.PaymentService.Domain.Interface.Service
{
    public interface IDbInitializer
    {
        Task Initialize();
    }
}
