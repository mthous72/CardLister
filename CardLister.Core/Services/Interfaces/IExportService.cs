using System.Collections.Generic;
using System.Threading.Tasks;
using CardLister.Core.Models;
using CardLister.Core.Models.Enums;

namespace CardLister.Core.Services
{
    public interface IExportService
    {
        string GenerateTitle(Card card);
        string GenerateDescription(Card card);
        Task ExportCsvAsync(List<Card> cards, string outputPath);
        Task ExportCsvAsync(List<Card> cards, string outputPath, ExportPlatform platform);
        List<string> ValidateCardForExport(Card card);
        Task ExportTaxCsvAsync(List<Card> soldCards, string outputPath);
    }
}
