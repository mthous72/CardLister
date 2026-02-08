using System.Threading.Tasks;

namespace CardLister.Core.Services
{
    public interface IImageUploadService
    {
        Task<string> UploadImageAsync(string imagePath, string? name = null);
        Task<(string? url1, string? url2)> UploadCardImagesAsync(string frontPath, string? backPath = null);
    }
}
