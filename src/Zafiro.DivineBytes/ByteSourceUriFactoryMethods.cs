using System.Net.Http;
using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes;

/// <summary>
/// Factory methods for ByteSource related to URIs
/// These methods extend the existing ByteSource functionality
/// </summary>
public static class ByteSourceUriFactoryMethods
{
    /// <summary>
    /// Creates a ByteSource from a URI using a provided HttpClient
    /// This is the recommended method for applications that use dependency injection
    /// </summary>
    /// <param name="uri">The URI from which to obtain the content</param>
    /// <param name="httpClient">The HttpClient to use for the download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A Result with the ByteSource if the operation is successful</returns>
    public static async Task<Result<IByteSource>> FromUri(
        Uri uri,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        var contentProvider = new HttpUriContentProvider(httpClient);
        return await contentProvider.GetByteSourceAsync(uri, cancellationToken);
    }

    /// <summary>
    /// Creates a ByteSource from a URI string using a provided HttpClient
    /// This is the recommended method for applications that use dependency injection
    /// </summary>
    /// <param name="uriString">The URI string from which to obtain the content</param>
    /// <param name="httpClient">The HttpClient to use for the download</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A Result with the ByteSource if the operation is successful</returns>
    public static async Task<Result<IByteSource>> FromUri(
        this string uriString,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            return Result.Failure<IByteSource>($"Invalid URI: {uriString}");
        }

        return await FromUri(uri, httpClient, cancellationToken);
    }

    /// <summary>
    /// Creates a ByteSource from a URI with internal HttpClient
    /// NOTE: Creates an internal HttpClient for simple cases and occasional use
    /// For production applications, prefer using FromUriAsync(uri, httpClient)
    /// </summary>
    /// <param name="uri">The URI from which to obtain the content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A Result with the ByteSource if the operation is successful</returns>
    public static Task<Result<IByteSource>> FromUri(
        this Uri uri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the URI first
            if (uri.Scheme != "http" && uri.Scheme != "https")
            {
                return Task.FromResult(Result.Failure<IByteSource>($"Unsupported URI scheme: {uri.Scheme}. Only HTTP and HTTPS are supported."));
            }

            // Create the ByteSource using Zafiro's HttpResponseMessageStream
            var byteSource = ByteSource.FromAsyncStreamFactory(async () =>
            {
                var httpClient = new HttpClient();
                try
                {
                    var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    // Create a wrapper that disposes both the HttpClient and the HttpResponseMessage
                    var responseStream = await HttpResponseMessageStream.Create(response);
                    return new HttpClientOwningStream(responseStream, httpClient);
                }
                catch
                {
                    httpClient.Dispose();
                    throw;
                }
            });

            return Task.FromResult(Result.Success(byteSource));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<IByteSource>($"Error creating ByteSource from {uri}: {ex.Message}"));
        }
    }

    /// <summary>
    /// Creates a ByteSource from a URI string with internal HttpClient
    /// NOTE: Creates an internal HttpClient for simple cases and occasional use
    /// For production applications, prefer using FromUriAsync(uriString, httpClient)
    /// </summary>
    /// <param name="uriString">The URI string from which to obtain the content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A Result with the ByteSource if the operation is successful</returns>
    public static async Task<Result<IByteSource>> FromUri(
        string uriString,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
        {
            return Result.Failure<IByteSource>($"Invalid URI: {uriString}");
        }

        return await FromUri(uri, cancellationToken);
    }

    /// <summary>
    /// Stream wrapper that manages the HttpClient in addition to the HttpResponseMessageStream
    /// </summary>
    private class HttpClientOwningStream : Stream
    {
        private readonly Stream innerStream;
        private readonly HttpClient httpClient;

        public HttpClientOwningStream(Stream innerStream, HttpClient httpClient)
        {
            this.innerStream = innerStream;
            this.httpClient = httpClient;
        }

        protected override void Dispose(bool disposing)
        {
            innerStream.Dispose();
            httpClient.Dispose();
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await innerStream.DisposeAsync();
            httpClient.Dispose();
            await base.DisposeAsync();
        }

        // Delegate all properties and methods to the inner stream
        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanWrite => innerStream.CanWrite;
        public override long Length => innerStream.Length;
        public override long Position
        {
            get => innerStream.Position;
            set => innerStream.Position = value;
        }

        public override void Flush() => innerStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => innerStream.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => innerStream.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);
        public override void SetLength(long value) => innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => innerStream.WriteAsync(buffer, cancellationToken);
    }
}
