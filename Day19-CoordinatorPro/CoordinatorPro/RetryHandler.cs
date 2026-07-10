using System.Net.Http;
using System.Threading;

public class RetryHandler : DelegatingHandler
{
    private readonly int _maxRetries;

    public RetryHandler(int maxRetries = 3)
    {
        _maxRetries = maxRetries;
        // Use the modern default HTTP handler under the hood
        InnerHandler = new SocketsHttpHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (int i = 0; i <= _maxRetries; i++)
        {
            var response = await base.SendAsync(request, cancellationToken);

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
}