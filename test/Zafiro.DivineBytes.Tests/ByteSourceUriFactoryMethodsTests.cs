using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes.Tests;

public class ByteSourceUriFactoryMethodsTests
{
    [Fact]
    public async Task FromUri_string_returns_failure_for_invalid_uri()
    {
        var result = await ByteSourceUriFactoryMethods.FromUri("not a uri");

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid URI", result.Error);
    }

    [Fact]
    public async Task FromUri_rejects_unsupported_schemes()
    {
        var result = await ByteSourceUriFactoryMethods.FromUri(new Uri("ftp://example.com/file"));

        Assert.True(result.IsFailure);
        Assert.Contains("Unsupported URI scheme", result.Error);
    }

    [Fact]
    public async Task ToByteSource_uses_provided_content_provider()
    {
        var provider = new RecordingProvider();
        var uri = new Uri("https://example.com/data");

        var result = await uri.ToByteSourceAsync(provider);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { uri }, provider.RequestedUris);
    }

    private sealed class RecordingProvider : IUriContentProvider
    {
        public List<Uri> RequestedUris { get; } = new();

        public Task<Result<IByteSource>> GetByteSourceAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            RequestedUris.Add(uri);
            return Task.FromResult(Result.Success<IByteSource>(ByteSource.FromBytes(Array.Empty<byte>())));
        }
    }
}
