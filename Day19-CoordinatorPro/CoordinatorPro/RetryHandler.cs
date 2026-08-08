// RetryHandler
// ---------------------------------------------------------------------------
// A plain DelegatingHandler that retries transient HTTP failures (500/503/
// 429) with exponential backoff before giving up. This is entirely
// provider-agnostic - it just wraps whatever HttpClient the connector uses,
// so it works unchanged no matter which LLM provider is registered.
using System.Net.Http;
using System.Threading;

namespace CoordinatorPro
{
    /// <summary>Provider-agnostic DelegatingHandler that retries transient HTTP failures (500/503/429) with exponential backoff.</summary>
    public class RetryHandler : DelegatingHandler
    {
        private readonly int _maxRetries;

        /// <summary>Wraps a modern <see cref="SocketsHttpHandler"/>, retrying up to <paramref name="maxRetries"/> times on transient failures.</summary>
        /// <param name="maxRetries">The maximum number of retry attempts before giving up and returning the failed response.</param>
        public RetryHandler(int maxRetries = 3)
        {
            _maxRetries = maxRetries;
            // Use the modern default HTTP handler under the hood
            InnerHandler = new SocketsHttpHandler();
        }

        /// <summary>Sends the request, retrying with exponential backoff on 500/503/429 responses until <see cref="_maxRetries"/> is exhausted.</summary>
        /// <param name="request">The HTTP request to send.</param>
        /// <param name="cancellationToken">Token used to cancel the request or any pending retry delay.</param>
        /// <returns>The first successful or non-transient-failure response, or the final failed response after exhausting retries.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            for (int i = 0; i <= _maxRetries; i++)
            {
                // .NET marks an HttpRequestMessage as "sent" the moment it's submitted,
                // so resending the same instance on a retry throws InvalidOperationException
                // instead of actually retrying. Send a clone every attempt instead.
                using var requestClone = await CloneAsync(request);
                var response = await base.SendAsync(requestClone, cancellationToken);

                // If it succeeds, or fails with a non-transient error (like a 404), return immediately
                int statusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode || (statusCode != 503 && statusCode != 500 && statusCode != 429))
                {
                    return response;
                }

                // If we've hit our max retries, just return the failed response
                if (i == _maxRetries) return response;

                // Exponential backoff: Wait 1s, then 2s, then 4s...
                int delaySeconds = (int)Math.Pow(2, i);
                Console.WriteLine($"\n[API Error {statusCode}] Server busy. Retrying in {delaySeconds}s... (Attempt {i + 1} of {_maxRetries})");

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }

            throw new Exception("Unreachable code reached in RetryHandler.");
        }

        /// <summary>Creates a fresh, unsent copy of an <see cref="HttpRequestMessage"/> so it can be resent on a retry attempt.</summary>
        /// <param name="request">The request to clone. Its content, if any, is buffered so it can be read again.</param>
        /// <returns>A new <see cref="HttpRequestMessage"/> with the same method, URI, version, headers, and content.</returns>
        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Version = request.Version
            };

            if (request.Content is not null)
            {
                byte[] contentBytes = await request.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(contentBytes);
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.Add(header.Key, header.Value);
                }
            }

            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}