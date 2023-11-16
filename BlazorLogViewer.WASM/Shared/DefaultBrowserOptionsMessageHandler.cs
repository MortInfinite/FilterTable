using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace BlazorLogViewer.WASM.Shared
{
    /// <summary>
    /// Message handler which is called every time a REST request is made, in order to modify the request headers.
    /// </summary>
    /// <remarks>
    /// Based on:
    /// https://www.meziantou.net/bypass-browser-cache-using-httpclient-in-blazor-webassembly.htm
    /// and
    /// https://github.com/meziantou/Meziantou.Framework/blob/1296b15558029d5bb2880cfd376561895055565d/src/Meziantou.AspNetCore.Components.WebAssembly/DefaultBrowserOptionsMessageHandler.cs
    /// </remarks>
    public sealed class DefaultBrowserOptionsMessageHandler : DelegatingHandler
    {
        static readonly HttpRequestOptionsKey<IDictionary<string, object>> s_fetchRequestOptionsKey = new("WebAssemblyFetchOptions");

        public DefaultBrowserOptionsMessageHandler()
        {
        }

        public DefaultBrowserOptionsMessageHandler(HttpMessageHandler innerHandler)
        {
            InnerHandler = innerHandler;
        }

        public BrowserRequestCache DefaultBrowserRequestCache
        {
            get; set;
        }

        public BrowserRequestCredentials DefaultBrowserRequestCredentials
        {
            get; set;
        }

        public BrowserRequestMode DefaultBrowserRequestMode
        {
            get; set;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get the existing options to not override them if set explicitly
            if (!request.Options.TryGetValue(s_fetchRequestOptionsKey, out var fetchOptions))
                fetchOptions = null;

            if (fetchOptions?.ContainsKey("cache") != true)
                request.SetBrowserRequestCache(DefaultBrowserRequestCache);

            if (fetchOptions?.ContainsKey("credentials") != true)
                request.SetBrowserRequestCredentials(DefaultBrowserRequestCredentials);

            if (fetchOptions?.ContainsKey("mode") != true)
                request.SetBrowserRequestMode(DefaultBrowserRequestMode);

            return base.SendAsync(request, cancellationToken);
        }
    }
}