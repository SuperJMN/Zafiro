using System.Net.Http;
using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes;

/// <summary>
/// HTTP URI content provider implementation that uses HttpClient to download content from HTTP/HTTPS URIs
/// </summary>
public class HttpUriContentProvider : IUriContentProvider, IDisposable
{
    private readonly HttpClient httpClient;
    private readonly bool ownsHttpClient;

    /// <summary>
    /// Creates a new instance using an externally provided HttpClient
    /// </summary>
    /// <param name="httpClient">The HttpClient to use for downloads</param>
    public HttpUriContentProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ownsHttpClient = false;
    }

    /// <summary>
    /// Creates a new instance with an internal HttpClient (for simple cases)
    /// </summary>
    public HttpUriContentProvider()
    {
        httpClient = new HttpClient();
        ownsHttpClient = true;
    }

    public async Task<Result<IByteSource>> GetByteSourceAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        try
        {
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return Result.Failure<IByteSource>($"Unsupported URI scheme: {uri.Scheme}. Only HTTP and HTTPS are supported.");
            }

            var byteSource = ByteSource.FromAsyncStreamFactory(async () =>
            {
                var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                try
                {
                    response.EnsureSuccessStatusCode();
                    return await HttpResponseMessageStream.Create(response);
                }
                catch
                {
                    response.Dispose();
                    throw;
                }
            });

            return Result.Success(byteSource);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<IByteSource>($"Network error downloading from {uri}: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return Result.Failure<IByteSource>($"Download cancelled for {uri}");
        }
        catch (Exception ex)
        {
            return Result.Failure<IByteSource>($"Unexpected error downloading from {uri}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (ownsHttpClient)
        {
            httpClient?.Dispose();
        }
    }
}
