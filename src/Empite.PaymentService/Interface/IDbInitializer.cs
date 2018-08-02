using System.Threading.Tasks;

namespace Empite.PaymentService.Interface
{
    public interface IDbInitializer
    {
        Task Initialize();
    }
}
