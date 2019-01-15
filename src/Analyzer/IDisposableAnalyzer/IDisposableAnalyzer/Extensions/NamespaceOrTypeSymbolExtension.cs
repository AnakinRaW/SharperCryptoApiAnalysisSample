using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace IDisposableAnalyzer.Extensions
{
    public static class NamespaceOrTypeSymbolExtension
    {
        public static string GetFullNamespace(this INamespaceOrTypeSymbol @namespace)
        {
            var stack = new Stack<string>();
            while (@namespace.ContainingNamespace != null)
            {
                stack.Push(@namespace.Name);
                @namespace = @namespace.ContainingNamespace;
            }

            return string.Join(".", stack);
        }
    }
}