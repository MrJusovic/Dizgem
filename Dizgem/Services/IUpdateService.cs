using Dizgem.Models;

namespace Dizgem.Services
{
    public interface IUpdateService
    {
        /// <summary>
        /// GitHub'ı kontrol ederek yeni bir güncelleme olup olmadığını denetler.
        /// </summary>
        Task<UpdateCheckViewModel> CheckForUpdateAsync();

        /// <summary>
        /// Yeni sürümü indirir, geçici bir klasöre açar ve güncelleme bayrağını oluşturur.
        /// </summary>
        /// <param name="downloadUrl">GitHub'dan alınan indirme linki</param>
        Task DownloadAndPrepareUpdateAsync(string downloadUrl);
    }
}
