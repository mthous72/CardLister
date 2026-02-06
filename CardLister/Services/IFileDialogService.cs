using System.Threading.Tasks;

namespace CardLister.Services
{
    public interface IFileDialogService
    {
        Task<string?> OpenImageFileAsync();
        Task<string?> SaveCsvFileAsync(string defaultFileName);
    }
}
