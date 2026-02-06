using System.Collections.Generic;
using System.Threading.Tasks;
using CardLister.Models;

namespace CardLister.Services
{
    public interface IExportService
    {
        string GenerateTitle(Card card);
        string GenerateDescription(Card card);
        Task ExportCsvAsync(List<Card> cards, string outputPath);
        List<string> ValidateCardForExport(Card card);
        Task ExportTaxCsvAsync(List<Card> soldCards, string outputPath);
    }
}
