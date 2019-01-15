using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SharperCryptoApiAnalysis.Interop.CodeAnalysis;

namespace IDisposableAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisposableAnalyzer : SharperCryptoApiAnalysisDiagnosticAnalyzer
    {
        public const string DiagnosticId = DiagnosticPrefix.Prefix + "100";

        private static readonly DiagnosticDescriptor WarningRule = new DiagnosticDescriptor(DiagnosticId,
            AnalysisReports.NotDisposedReport.Summary,
            AnalysisReports.NotDisposedReport.Summary, AnalysisReports.NotDisposedReport.Category,
            DiagnosticSeverity.Warning, true, AnalysisReports.NotDisposedReport.Description);

        public override string Name => "IDisposable Analyzer";
        public override uint AnalyzerId => 100;

        public override ImmutableArray<IAnalysisReport> SupportedReports =>
            ImmutableArray.Create(AnalysisReports.NotDisposedReport);
        public override DiagnosticDescriptor DefaultRule => WarningRule;
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(WarningRule);

        protected override DiagnosticSeverity AnalysisSeverityToDiagnosticSeverity(AnalysisSeverity severity)
        {
            return DiagnosticSeverity.Warning;
        }

        protected override void InitializeContext(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(ObjectCreationAction, SyntaxKind.ObjectCreationExpression);
        }

        private void ObjectCreationAction(SyntaxNodeAnalysisContext context)
        {
            var rule = GetRule(DiagnosticId, CurrentSeverity);
            context.ReportDiagnostic(Diagnostic.Create(rule,
                Location.None,
                context.ContainingSymbol.Name));
        }
    }
}
