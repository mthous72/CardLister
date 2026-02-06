using System.Threading.Tasks;
using CardLister.Models;

namespace CardLister.Services
{
    public interface ISettingsService
    {
        AppSettings Load();
        void Save(AppSettings settings);
        bool HasValidConfig();
        Task<bool> TestOpenRouterConnectionAsync(string apiKey);
        Task<bool> TestImgBBConnectionAsync(string apiKey);
    }
}
