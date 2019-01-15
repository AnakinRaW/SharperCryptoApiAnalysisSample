using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IDisposableAnalyzer.Configuration;
using IDisposableAnalyzer.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SharperCryptoApiAnalysis.Interop.CodeAnalysis;

namespace IDisposableAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisposableAnalyzer : SharperCryptoApiAnalysisDiagnosticAnalyzer
    {
        public const string DiagnosticId = DiagnosticPrefix.Prefix + "999";

        private static readonly IDetector Detector = new TrackingTypeDetector();
        private static readonly IConfiguration Configuration = ConfigurationManager.Instance;

        public override string Name => "IDisposable Analyzer";
        public override uint AnalyzerId => 999;

        public override ImmutableArray<IAnalysisReport> SupportedReports =>
            ImmutableArray.Create(AnalysisReports.NotDisposedReport);
        public override DiagnosticDescriptor DefaultRule => SyntaxNodeAnalysisContextExtension.InfoRule;
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(SyntaxNodeAnalysisContextExtension.WarningRule, 
            SyntaxNodeAnalysisContextExtension.ErrorRule, SyntaxNodeAnalysisContextExtension.InfoRule);

        protected override DiagnosticSeverity AnalysisSeverityToDiagnosticSeverity(AnalysisSeverity severity)
        {
            switch (severity)
            {
                case AnalysisSeverity.Default:
                    return DiagnosticSeverity.Info;
                case AnalysisSeverity.Strict:
                    return DiagnosticSeverity.Error;
                case AnalysisSeverity.Medium:
                    return DiagnosticSeverity.Warning;
                case AnalysisSeverity.Low:
                    return DiagnosticSeverity.Info;
                case AnalysisSeverity.Informative:
                    return DiagnosticSeverity.Info;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }

        protected override void InitializeContext(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(ObjectCreationAction, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(InvocationAction, SyntaxKind.InvocationExpression);
        }

        private void ObjectCreationAction(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as ObjectCreationExpressionSyntax;
            if (node == null) return; //something went wrong

            var symbolInfo = context.SemanticModel.GetSymbolInfo(node);
            var type = (symbolInfo.Symbol as IMethodSymbol)?.ReceiverType as INamedTypeSymbol;
            if (type == null)
            {
            }
            else if (!type.IsDisposeableOrImplementsDisposable()) return;
            else if (node.IsParentADisposeCallIgnoringParenthesis()) return; //(new MemoryStream()).Dispose()
            else if (Detector.IsIgnoredTypeOrImplementsIgnoredInterface(type))
            {
            }
            else if (node.IsReturnedInProperty())
                AnalyseNodeInReturnStatementOfProperty(context, node, DisposableSource.ObjectCreation);
            else if (node.IsPartOfReturnStatementInBlock())
            {
            } // return new MemoryStream() or return Task.FromResult(new MemoryStream())
            else if (node.IsArrowExpressionClauseOfMethod())
            {
            } // void Create()=>CreateMemoryStream()
            else if (node.IsReturnValueInLambdaExpression())
            {
            }
            else if (node.IsReturnedLaterWithinMethod())
            {
            }
            else if (node.IsReturnedLaterWithinParenthesizedLambdaExpression())
            {
            }
            else if (!type.IsDisposeableOrImplementsDisposable())
            {
            }
            else if (node.IsPartOfMethodCall())
            {
                var methodInvocation = node.Parent.Parent.Parent as InvocationExpressionSyntax;
                if (Detector.IsTrackingMethodCall(methodInvocation, context.SemanticModel))
                    return;

                var rule = GetRule(DiagnosticId, CurrentSeverity);
                context.ReportNotDisposedAnonymousObject(rule, DisposableSource.ObjectCreation);
            }
            else if (node.IsMaybePartOfMethodChainUsingTrackingExtensionMethod())
            {
                var methodInvokation = node.Parent.Parent as InvocationExpressionSyntax;
                if (Detector.IsTrackingMethodCall(methodInvokation, context.SemanticModel)) return;
            }
            else if (node.IsArgumentInObjectCreation())
                AnalyseNodeInArgumentList(context, node, DisposableSource.ObjectCreation);
            else if (node.IsPartIfArrayInitializerThatIsPartOfObjectCreation())
            {
                var objectCreation = node.Parent.Parent.Parent.Parent.Parent as ObjectCreationExpressionSyntax;
                CheckIfObjectCreationTracksNode(context, objectCreation, DisposableSource.ObjectCreation);
            }
            else if (node.IsDescendantOfUsingHeader())
            {
            } //this have to be checked after IsArgumentInObjectCreation
            else if (node.IsDescendantOfVariableDeclarator())
                AnalyseNodeWithinVariableDeclarator(context, node, DisposableSource.ObjectCreation);
            else if (node.IsPartOfAssignmentExpression())
                AnalyseNodeInAssignmentExpression(context, node, DisposableSource.ObjectCreation);
            else if (node.IsPartOfPropertyExpressionBody())
                AnalyseNodeInAutoPropertyOrPropertyExpressionBody(context, node, DisposableSource.ObjectCreation);
            else if (node.IsPartOfAutoProperty())
                AnalyseNodeInAutoPropertyOrPropertyExpressionBody(context, node, DisposableSource.ObjectCreation);
            else
            {
                //new MemoryStream();
                var rule = GetRule(DiagnosticId, CurrentSeverity);
                context.ReportNotDisposedAnonymousObject(rule, DisposableSource.ObjectCreation);
            }
        }

        private void InvocationAction(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as InvocationExpressionSyntax;
            if (node == null) return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(node);
            var symbol = symbolInfo.Symbol as IMethodSymbol;
            var type = symbol?.ReturnType as INamedTypeSymbol;

            if (type == null)
            {
            }
            else if (node.IsParentADisposeCallIgnoringParenthesis()) return; //(new object()).AsDisposable().Dispose()
            else if (node.IsPartOfAwaitExpression()) AnalyseInvokationExpressionInsideAwaitExpression(context, node);
            else if (!type.IsDisposeableOrImplementsDisposable()) return;
            else if (node.IsReturnedInProperty())
                AnalyseNodeInReturnStatementOfProperty(context, node, DisposableSource.InvokationExpression);
            else if (Detector.IsIgnoredTypeOrImplementsIgnoredInterface(type))
            {
            } //GetEnumerator()
            else if (Detector.IsTrackingMethodCall(node, context.SemanticModel))
            {
            } //ignored extension methods
            else if (Detector.IsIgnoredFactoryMethod(node, context.SemanticModel)) return; //A.Fake<IDisposable>
            else if (node.IsMaybePartOfMethodChainUsingTrackingExtensionMethod())
            {
                //there maybe multiple method invocations within one chain
                var baseNode = node;
                while (baseNode?.Parent is MemberAccessExpressionSyntax &&
                       baseNode?.Parent?.Parent is InvocationExpressionSyntax)
                {
                    baseNode = baseNode.Parent.Parent as InvocationExpressionSyntax;
                    if (Detector.IsTrackingMethodCall(baseNode, context.SemanticModel)) return;
                }
            }
            else if (!type.IsDisposeableOrImplementsDisposable())
            {
            }
            else if (node.IsPartOfMethodCall())
            {
                var methodInvocation = node.Parent.Parent.Parent as InvocationExpressionSyntax;
                if (Detector.IsTrackingMethodCall(methodInvocation, context.SemanticModel))
                    return;

                var rule = GetRule(DiagnosticId, CurrentSeverity);
                context.ReportNotDisposedAnonymousObject(rule, DisposableSource.ObjectCreation);
            }
            else if (node.IsPartOfReturnStatementInBlock())
            {
            } // return new MemoryStream() or return Task.FromResult(new MemoryStream())
            else if (node.IsArrowExpressionClauseOfMethod())
            {
            } // void Create()=>new MemoryStream()
            else if (node.IsReturnValueInLambdaExpression())
            {
            } //e.g. ()=> new MemoryStream
            else if (node.IsReturnedLaterWithinMethod())
            {
            }
            else if (node.IsReturnedLaterWithinParenthesizedLambdaExpression())
            {
            }
            else if (node.IsArgumentInObjectCreation())
                AnalyseNodeInArgumentList(context, node, DisposableSource.InvokationExpression);
            else if (node.IsPartIfArrayInitializerThatIsPartOfObjectCreation())
            {
                var objectCreation = node.Parent.Parent.Parent.Parent.Parent as ObjectCreationExpressionSyntax;
                CheckIfObjectCreationTracksNode(context, objectCreation, DisposableSource.ObjectCreation);
            }
            else if (node.IsDescendantOfUsingHeader())
            {
            } //using(memstream) or using(new MemoryStream())
            else if (node.IsDescendantOfVariableDeclarator())
                AnalyseNodeWithinVariableDeclarator(context, node, DisposableSource.InvokationExpression);
            else if (node.IsPartOfAssignmentExpression())
                AnalyseNodeInAssignmentExpression(context, node, DisposableSource.InvokationExpression);
            else if (node.IsPartOfAutoProperty())
                AnalyseNodeInAutoPropertyOrPropertyExpressionBody(context, node, DisposableSource.InvokationExpression);
            else if (node.IsPartOfPropertyExpressionBody())
                AnalyseNodeInAutoPropertyOrPropertyExpressionBody(context, node, DisposableSource.InvokationExpression);
            else
            {
                var rule = GetRule(DiagnosticId, CurrentSeverity);
                context.ReportNotDisposedAnonymousObject(rule, DisposableSource
                    .InvokationExpression); //call to Create(): MemeoryStream
            }
        }

        private void AnalyseNodeInArgumentList(SyntaxNodeAnalysisContext context,
            SyntaxNode node, DisposableSource source)
        {
            var objectCreation = node.Parent.Parent.Parent as ObjectCreationExpressionSyntax;
            var t = context.SemanticModel.GetReturnTypeOf(objectCreation);
            if (t == null) return;//return type could not be determind
            if (Detector.IsTrackedType(t, objectCreation, context.SemanticModel)) return;

            var rule = GetRule(DiagnosticId, CurrentSeverity);
            context.ReportNotDisposedAnonymousObject(rule, source);
        }

        private void AnalyseNodeInAutoPropertyOrPropertyExpressionBody(SyntaxNodeAnalysisContext context, SyntaxNode node, DisposableSource source)
        {
            var propertyDeclaration = node.Parent.Parent as PropertyDeclarationSyntax;
            if (propertyDeclaration == null) return; // should not happen => we cke this before

            if (node.IsDisposedInDisposingMethod(propertyDeclaration.Identifier.Text, Configuration, context.SemanticModel)) return;
            var rule = GetRule(DiagnosticId, CurrentSeverity);
            context.ReportNotDisposedProperty(rule, propertyDeclaration.Identifier.Text, source);
        }

        private void AnalyseNodeInReturnStatementOfProperty(SyntaxNodeAnalysisContext context, SyntaxNode node, DisposableSource source)
        {
            var propertyDeclaration = node.Parent.Parent.Parent.Parent.Parent as PropertyDeclarationSyntax;
            if (propertyDeclaration == null) return; // should not happen => we cke this before

            if (node.IsDisposedInDisposingMethod(propertyDeclaration.Identifier.Text, Configuration, context.SemanticModel)) return;
            var rule = GetRule(DiagnosticId, CurrentSeverity);
            context.ReportNotDisposedProperty(rule, propertyDeclaration.Identifier.Text, source);
        }

        private void CheckIfObjectCreationTracksNode(SyntaxNodeAnalysisContext context, ObjectCreationExpressionSyntax objectCreation, DisposableSource source)
        {
            var t = context.SemanticModel.GetReturnTypeOf(objectCreation);
            if (t == null) return;//return type could not be determind
            if (Detector.IsTrackedType(t, objectCreation, context.SemanticModel)) return;

            var rule = GetRule(DiagnosticId, CurrentSeverity);
            context.ReportNotDisposedAnonymousObject(rule, source);
        }

        private void AnalyseNodeWithinVariableDeclarator(SyntaxNodeAnalysisContext context,
            SyntaxNode node, DisposableSource source)
        {
            var identifier = node.GetIdentifierIfIsPartOfVariableDeclarator();//getIdentifier
            if (identifier == null) return;

            if (node.IsLocalDeclaration()) //var m = new MemoryStream();
            {
                AnalyseNodeWithinLocalDeclaration(context, node, identifier);
            }
            else if (node.IsFieldDeclaration()) //_field = new MemoryStream();
            {
                AnalyseNodeInFieldDeclaration(context, node, identifier, source);
            }
        }

        private void AnalyseNodeWithinLocalDeclaration(SyntaxNodeAnalysisContext context,
            SyntaxNode node, string localVariableName)
        {
            SyntaxNode parentScope;//lamda or ctor or method or property
            if (!node.TryFindParentScope(out parentScope)) return;

            var localVariableInsideUsing = parentScope
                .DescendantNodes<UsingStatementSyntax>()
                .SelectMany(@using => @using.DescendantNodes<IdentifierNameSyntax>())
                .Where(id => localVariableName != null && (string)id.Identifier.Value == localVariableName)
                .ToArray();

            var rule = GetRule(DiagnosticId, CurrentSeverity);
            if (localVariableInsideUsing.Any())
            {
                if (localVariableInsideUsing.Any(id => id.Parent is UsingStatementSyntax)) //using(mem))
                {
                    return;
                }
                if (IsArgumentInConstructorOfTrackingTypeWithinUsing(context, localVariableInsideUsing)) return;

                context.ReportNotDisposedLocalVariable(rule);
                return;
            }
            var invocationExpressions = parentScope.DescendantNodes<InvocationExpressionSyntax>().ToArray();
            if (ExistsDisposeCall(localVariableName, invocationExpressions, context.SemanticModel)) return;
            if (IsArgumentInTrackingMethod(context, localVariableName, invocationExpressions)) return;
            if (IsArgumentInConstructorOfTrackingType(context, localVariableName, parentScope)) return;
            if (IsCallToMethodThatIsConsideredAsDisposeCall(invocationExpressions, context)) return;

            context.ReportNotDisposedLocalVariable(rule);
        }

        private static bool IsArgumentInConstructorOfTrackingTypeWithinUsing(SyntaxNodeAnalysisContext context, IEnumerable<IdentifierNameSyntax> localVariableInsideUsing)
        {
            return localVariableInsideUsing
                .Select(id => id.Parent?.Parent?.Parent)
                .Where(parent => parent is ObjectCreationExpressionSyntax)
                .Cast<ObjectCreationExpressionSyntax>()
                .Any(ocs =>
                {
                    var sym = context.SemanticModel.GetSymbolInfo(ocs);
                    var s = context.SemanticModel.GetDeclaredSymbol(ocs);
                    var type2 = (sym.Symbol as IMethodSymbol)?.ReceiverType as INamedTypeSymbol;

                    return Detector.IsTrackedType(type2, ocs, context.SemanticModel);
                });
        }

        private static bool ExistsDisposeCall(string localVariableName, IEnumerable<InvocationExpressionSyntax> invocationExpressions, SemanticModel semanticModel)
        {
            return invocationExpressions.Any(ies => localVariableName != null && ies.IsCallToDisposeFor(localVariableName, semanticModel, Configuration));
        }

        private static bool IsArgumentInTrackingMethod(SyntaxNodeAnalysisContext context, string localVariableName, IEnumerable<InvocationExpressionSyntax> invocationExpressions)
        {
            return invocationExpressions.Any(ie => ie.UsesVariableInArguments(localVariableName) && Detector.IsTrackingMethodCall(ie, context.SemanticModel));
        }

        private static bool IsArgumentInConstructorOfTrackingType(SyntaxNodeAnalysisContext context,
            string localVariableName, SyntaxNode parentScope)
        {
            return parentScope
                .DescendantNodes<ObjectCreationExpressionSyntax>()
                .Any(oce =>
                {
                    var argumentListSyntax = oce.ArgumentList;
                    if (argumentListSyntax == null) return false;
                    return argumentListSyntax.Arguments.Any(arg =>
                    {
                        var expression = arg.Expression as IdentifierNameSyntax;
                        var isPartOfObjectcreation = expression?.Identifier.Text == localVariableName;
                        if (!isPartOfObjectcreation) return false;

                        //check if is tracking instance
                        var sym = context.SemanticModel.GetSymbolInfo(oce);
                        return (sym.Symbol as IMethodSymbol)?.ReceiverType is INamedTypeSymbol type2
                               && Detector.IsTrackedType(type2, oce, context.SemanticModel);
                    });
                });
        }

        private static bool IsCallToMethodThatIsConsideredAsDisposeCall(IEnumerable<InvocationExpressionSyntax> invocations,
            SyntaxNodeAnalysisContext context)
        {
            var fullName = GetReturnOrReceivedType(context);
            IReadOnlyCollection<MethodCall> methodCalls;
            if (Configuration.DisposingMethodsAtSpecialClasses.TryGetValue(fullName, out methodCalls))
            {
                //todo check parameres of each ies
                return methodCalls.Any(mc => invocations.Any(ies => ies.IsCallToMethod(mc)));

            }
            return false;
        }

        private static string GetReturnOrReceivedType(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            var typeInfo = context.SemanticModel.GetSymbolInfo(context.Node);
            if (node is ObjectCreationExpressionSyntax)
            {
                return ((typeInfo.Symbol as IMethodSymbol)?.ReceiverType as INamedTypeSymbol).GetFullNamespace();
            }
            if (node is InvocationExpressionSyntax)
            {
                return ((typeInfo.Symbol as IMethodSymbol)?.ReturnType as INamedTypeSymbol).GetFullNamespace();
            }
            throw new ArgumentException($"Unexpected Node Type: '{node.GetType()}'");
        }

        private void AnalyseNodeInFieldDeclaration(SyntaxNodeAnalysisContext context,
            SyntaxNode node, string variableName, DisposableSource source)
        {
            if (node.IsDisposedInDisposingMethod(variableName, Configuration, context.SemanticModel)) return;

            var rule = GetRule(DiagnosticId, CurrentSeverity);
            context.ReportNotDisposedField(rule, variableName, source);
        }

        private void AnalyseNodeInAssignmentExpression(SyntaxNodeAnalysisContext context,
            SyntaxNode node, DisposableSource source)
        {
            //is local or global variable
            var assignmentExrepssion = node.Parent as AssignmentExpressionSyntax;
            var variableName = (assignmentExrepssion?.Left as IdentifierNameSyntax)?.Identifier.Text;

            var rule = GetRule(DiagnosticId, CurrentSeverity);

            MethodDeclarationSyntax containingMethod;
            if (node.TryFindContainingMethod(out containingMethod))
            {
                if (containingMethod.ContainsDisposeCallFor(variableName, context.SemanticModel, Configuration)) return;

                if (containingMethod.HasDecendentVariableDeclaratorFor(variableName))
                {
                    //local declaration in method
                    if (containingMethod.Returns(variableName)) return;
                    if (node.IsDescendantOfUsingHeader()) return;
                    if (node.IsArgumentInObjectCreation())
                    {
                        AnalyseNodeInArgumentList(context, node, source);
                        return;
                    }
                    //is part of tracking call  
                    context.ReportNotDisposedLocalVariable(rule);
                    return;
                }
                if (node.IsDisposedInDisposingMethod(variableName, Configuration, context.SemanticModel)) return;
                if (node.IsArgumentInObjectCreation())
                {
                    AnalyseNodeInArgumentList(context, node, source);
                    return;
                }

                //assignment to field or property
                var containingClass = node.FindContainingClass();
                if (containingClass == null) return;
                if (containingClass.FindFieldNamed(variableName) != null)
                    context.ReportNotDisposedField(rule, variableName, source);
                else
                    context.ReportNotDisposedProperty(rule, variableName, source);

                return;
            }
            ConstructorDeclarationSyntax ctor;
            if (node.TryFindContainingCtor(out ctor))
            {
                if (ctor.HasDecendentVariableDeclaratorFor(variableName))
                {
                    //local variable in ctor
                    if (node.IsDescendantOfUsingHeader()) return;
                    if (node.IsArgumentInObjectCreation())
                    {
                        AnalyseNodeInArgumentList(context, node, source);
                        return;
                    }
                    if (ctor.ContainsDisposeCallFor(variableName, context.SemanticModel, Configuration)) return;
                    context.ReportNotDisposedLocalVariable(rule);
                }
                else //field or property
                {
                    if (node.IsDisposedInDisposingMethod(variableName, Configuration, context.SemanticModel)) return;

                    if (node.IsAssignmentToProperty(variableName))
                    {
                        context.ReportNotDisposedProperty(rule, variableName, source);
                    }
                    else
                    {
                        context.ReportNotDisposedField(rule, variableName, source);
                    }

                }
            }
        }

        private void AnalyseInvokationExpressionInsideAwaitExpression(SyntaxNodeAnalysisContext context,
            InvocationExpressionSyntax node)
        {
            var awaitExpression = node.Parent as AwaitExpressionSyntax;
            var awaitExpressionInfo = context.SemanticModel.GetAwaitExpressionInfo(awaitExpression);
            var returnType = awaitExpressionInfo.GetResultMethod?.ReturnType as INamedTypeSymbol;

            var rule = GetRule(DiagnosticId, CurrentSeverity);

            if (returnType == null) return;
            if (!returnType.IsDisposeableOrImplementsDisposable()) return;
            if (Detector.IsIgnoredTypeOrImplementsIgnoredInterface(returnType)) return;
            if (awaitExpression.IsDescendantOfUsingHeader()) return;
            if (awaitExpression.IsPartOfVariableDeclaratorInsideAUsingDeclaration()) return;
            if (awaitExpression.IsPartOfReturnStatementInMethod()) return;
            if (awaitExpression.IsReturnedLaterWithinMethod()) return;
            if (awaitExpression.IsDescendantOfVariableDeclarator())
            {
                AnalyseNodeWithinVariableDeclarator(context, awaitExpression, DisposableSource.InvokationExpression);
            }
            else if (awaitExpression.IsDescendantOfAssignmentExpressionSyntax())
            {
                if (node.TryFindParentClass(out var @class))
                {
                    var assignment = awaitExpression?.Parent as AssignmentExpressionSyntax;
                    var member = (assignment?.Left as IdentifierNameSyntax)?.Identifier.Text;
                    var isDisposed = @class.ContainsDisposeCallFor(member, context.SemanticModel, Configuration);
                    if (isDisposed) return;
                }
                context.ReportNotDisposedLocalVariable(rule);
            }
            else
            {
                context.ReportNotDisposedAnonymousObject(rule, DisposableSource.InvokationExpression);
            }
        }
    }
}
