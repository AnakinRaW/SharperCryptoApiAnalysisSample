using Microsoft.CodeAnalysis;
using SharperCryptoApiAnalysis.Interop.CodeAnalysis;
using SharperCryptoApiAnalysis.Interop.CodeAnalysis.Scoring;

namespace IDisposableAnalyzer
{
    public class NotDisposedReport : AnalysisReport
    {
        private static readonly LocalizableString SummaryString = "IDisposable class is not closed";

        private static readonly LocalizableString DescriptionString =
            "'Not using a random initialization Vector (IV) Mode causes algorithms to be susceptible to dictionary attacks.'(CWE-329)";

        private static readonly LocalizableString CategoryString = CommonAnalysisCategories.WeakConfiguration;

        private static readonly LocalizableString Remarks =
            "Use the using syntax expression of C# or call .Dispose() explicit.";

        private static readonly Exploitability ExploitabilityValue = Exploitability.Low;

        private static SecurityGoals SecurityGoals = SecurityGoals.Confidentiality;

        public NotDisposedReport() : base(DisposableAnalyzer.DiagnosticId, SummaryString.ToString(), DescriptionString.ToString(), CategoryString.ToString(),
            ExploitabilityValue, SecurityGoals, null, Remarks.ToString())
        {
        }
    }
}