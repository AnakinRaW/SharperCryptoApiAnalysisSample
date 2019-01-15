/*
 * Additional changes:
 *          Added custom DiagnosticDescriptors to match Sharper Crypto-API Analysis
 *          Methods take the expected DiagnosticDescriptor as parameter 
 */
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IDisposableAnalyzer.Extensions
{
    //todo define correct rules
    public static class SyntaxNodeAnalysisContextExtension
    {
        public const string DiagnosticId = DisposableAnalyzer.DiagnosticId;

        internal static readonly DiagnosticDescriptor WarningRule = new DiagnosticDescriptor(DisposableAnalyzer.DiagnosticId,
            AnalysisReports.NotDisposedReport.Summary,
            AnalysisReports.NotDisposedReport.Summary, AnalysisReports.NotDisposedReport.Category,
            DiagnosticSeverity.Warning, true, AnalysisReports.NotDisposedReport.Description);

        internal static readonly DiagnosticDescriptor ErrorRule = new DiagnosticDescriptor(DisposableAnalyzer.DiagnosticId,
            AnalysisReports.NotDisposedReport.Summary,
            AnalysisReports.NotDisposedReport.Summary, AnalysisReports.NotDisposedReport.Category,
            DiagnosticSeverity.Error, true, AnalysisReports.NotDisposedReport.Description);

        internal static readonly DiagnosticDescriptor InfoRule = new DiagnosticDescriptor(DisposableAnalyzer.DiagnosticId,
            AnalysisReports.NotDisposedReport.Summary,
            AnalysisReports.NotDisposedReport.Summary, AnalysisReports.NotDisposedReport.Category,
            DiagnosticSeverity.Info, true, AnalysisReports.NotDisposedReport.Description);



        public static void ReportNotDisposedField(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule, string variableName, DisposableSource source)
        {
            var location = context.Node.GetLocation();

            var properties = ImmutableDictionary.CreateBuilder<string, string>();
            properties.Add(Constants.Variablename, variableName);

            context.ReportDiagnostic(Diagnostic.Create(rule, location, properties.ToImmutable()));
        }

        public static void ReportNotDisposedProperty(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule, string variableName, DisposableSource source)
        {
            var location = context.Node.GetLocation();

            var properties = ImmutableDictionary.CreateBuilder<string, string>();
            properties.Add(Constants.Variablename, variableName);

            context.ReportDiagnostic(Diagnostic.Create(rule, location, properties.ToImmutable()));

        }

        public static void ReportNotDisposedLocalVariable(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule)
        {
            var location = context.Node.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(rule, location));
        }

        public static void ReportNotDisposedAnonymousObject(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule, DisposableSource source)
        {
            var location = context.Node.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(rule, location));
        }
    }
}