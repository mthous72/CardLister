using System.Threading.Tasks;
using CardLister.Core.Models;

namespace CardLister.Core.Services
{
    public interface IVariationVerifier
    {
        Task<VerificationResult> VerifyCardAsync(ScanResult scanResult, string imagePath);
        Task<SetChecklist?> GetChecklistAsync(string manufacturer, string brand, int year, string? sport = null);
        bool NeedsConfirmationPass(VerificationResult result);
        Task<VerificationResult> RunConfirmationPassAsync(ScanResult scanResult, VerificationResult verification, string imagePath);
    }
}
