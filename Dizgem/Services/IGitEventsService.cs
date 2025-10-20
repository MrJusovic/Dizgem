using Dizgem.Models;

namespace Dizgem.Services
{
    public interface IGitEventsService
    {
        Task<(bool Success, IEnumerable<GitEventsViewModel> Events)> GetEvents();
    }
}
