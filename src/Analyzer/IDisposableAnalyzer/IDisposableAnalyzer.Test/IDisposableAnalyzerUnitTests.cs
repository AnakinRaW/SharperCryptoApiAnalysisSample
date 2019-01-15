using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharperCryptoApiAnalysis.Interop.CodeAnalysis;
using TestHelper;

namespace IDisposableAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        public static IAnalysisReport Report = AnalysisReports.NotDisposedReport;

        [TestMethod]
        public void TestMethod2()
        {
            var test = @"
    namespace DisposableTest
    {
        internal class DisposableUsageClass
        {   
            public static void Usage()
            {
                
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = Report.Id,
                Message = Report.Summary,
                Severity = DiagnosticSeverity.Warning,
                //Locations =
                //    new[]
                //    {
                //        new DiagnosticResultLocation("Test0.cs", 0, 0)
                //    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DisposableAnalyzer();
        }
    }
}
