using CardLister.Core.Models.Enums;

namespace CardLister.Core.Models
{
    public class FieldConfidence
    {
        public string FieldName { get; set; } = string.Empty;
        public string? Value { get; set; }
        public VerificationConfidence Confidence { get; set; }
        public string? Reason { get; set; }
    }
}
