using System;
using Microsoft.CodeAnalysis;
using SharperCryptoApiAnalysis.Core;
using SharperCryptoApiAnalysis.Interop.CodeAnalysis;
using SharperCryptoApiAnalysis.Interop.CodeAnalysis.Scoring;

namespace IDisposableAnalyzer
{
    public class NotDisposedReport : AnalysisReport
    {
        private static readonly LocalizableString SummaryString = "IDisposable instance is not closed";

        private static readonly LocalizableString DescriptionString =
            "When working with data streams you need to ensure the data is removed from the memory of the executing system. Otherwise an attacker " +
            "may have the possibilities to read the plain text of an encrypted message if the attacker was able to read the memory of the system. The IDisposable " +
            "interfaces in .NET provides a method the release resources. The impact of what could happen if such data can be stolen from memory was greatly demonstrated " +
            "by the Heartbleed vulnerability.";

        private static readonly LocalizableString CategoryString = CommonAnalysisCategories.WeakConfiguration;

        private static readonly LocalizableString Remarks =
            "Use the using syntax expression of C# or call Dispose() explicitly.";

        private static readonly Exploitability ExploitabilityValue = Exploitability.Low;

        private static SecurityGoals SecurityGoals = SecurityGoals.Confidentiality;

        private static NamedLink Cve = new NamedLink("Read more about Heartbleed", new Uri("https://cve.mitre.org/cgi-bin/cvename.cgi?name=cve-2014-0160"));
        private static NamedLink dispose = new NamedLink("See the documentation of the IDisposable interface", new Uri("https://docs.microsoft.com/en-us/dotnet/api/system.idisposable"));

        public NotDisposedReport() : base(DisposableAnalyzer.DiagnosticId, SummaryString.ToString(), DescriptionString.ToString(), CategoryString.ToString(),
            ExploitabilityValue, SecurityGoals, null, Remarks.ToString(), dispose, Cve)
        {
        }
    }
}