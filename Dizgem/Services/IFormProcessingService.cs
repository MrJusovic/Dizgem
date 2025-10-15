using Dizgem.Models;

namespace Dizgem.Services
{
    public interface IFormProcessingService
    {
        Task<bool> ProcessFormAsync(FormHandler handler, IFormCollection formData);
    }
}
