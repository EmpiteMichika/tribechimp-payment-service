using System.Threading.Tasks;

namespace Empite.PaymentService.Interface.Service
{
    public interface IDbInitializer
    {
        Task Initialize();
    }
}
