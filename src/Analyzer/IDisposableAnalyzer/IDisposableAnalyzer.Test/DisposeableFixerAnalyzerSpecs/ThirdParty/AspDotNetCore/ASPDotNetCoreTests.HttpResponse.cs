using NUnit.Framework;

namespace IDisposableAnalyzer.Test.DisposeableFixerAnalyzerSpecs.ThirdParty.AspDotNetCore
{
    internal partial class ASPDotNetCoreTests
    {
        private static TestCaseData HttpResponse_RegisterForDispose() {
            const string code = @"using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace UsingRegisterForDispose
{
    internal class SomeClass
    {
        public SomeClass()
        {
            var response = new MyHttpResponse();
            var stream = new MemoryStream();
            response.RegisterForDispose(stream);
        }

        private class MyHttpResponse : HttpResponse
        {
        }
    }
}

namespace Microsoft.AspNetCore.Http
{
    public abstract class HttpResponse
    {
        public virtual void RegisterForDispose(IDisposable disposable)
        {
        }
    }
}";

            return new TestCaseData(code, 0)
                .SetName("HttpResponse.RegisterForDispose");
        }
    }
}