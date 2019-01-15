using SharperCryptoApiAnalysis.Interop.CodeAnalysis;

namespace IDisposableAnalyzer
{
    public static class AnalysisReports
    {
        public static readonly IAnalysisReport NotDisposedReport = new NotDisposedReport();
    }
}