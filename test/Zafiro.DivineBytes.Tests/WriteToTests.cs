using System.Security.Cryptography;
using System.Text;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CSharpFunctionalExtensions;
using Zafiro.Reactive;

namespace Zafiro.DivineBytes.Tests;

public class WriteToTests
{
    [Fact]
    public async Task WriteTo_Stream_roundtrips_random_bytes_with_non_aligned_chunks()
    {
        // Arrange
        var length = 100_000;
        var bufferSize = 4097; // intentionally non power-of-two to exercise chunk boundaries
        var data = RandomNumberGenerator.GetBytes(length);
        var source = ByteSource.FromBytes(data, bufferSize);
        await using var ms = new MemoryStream();

        // Act
        var result = await source.WriteTo(ms);

        // Assert
        Assert.True(result.IsSuccess);
        ms.Position = 0;
        var written = await ms.ReadBytesToEnd();
        Assert.Equal(data, written);
    }

    [Fact]
    public async Task WriteTo_Path_roundtrips_utf8_string()
    {
        // Arrange
        var text = string.Concat(Enumerable.Repeat("áéíóúÑç🙂", 1024));
        var source = ByteSource.FromString(text, Encoding.UTF8, bufferSize: 1234);
        var tmp = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), Guid.NewGuid() + ".bin");

        try
        {
            // Act
            var result = await source.WriteTo(tmp);

            // Assert
            Assert.True(result.IsSuccess);
            var bytes = await File.ReadAllBytesAsync(tmp);
            var roundtrip = Encoding.UTF8.GetString(bytes);
            Assert.Equal(text, roundtrip);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
    }

    [Fact]
    public async Task WriteTo_Chunked_emits_success_and_roundtrips()
    {
        // Arrange
        var data = Enumerable.Range(0, 50_000).Select(i => (byte)(i % 256)).ToArray();
        var source = ByteSource.FromBytes(data, bufferSize: 512);
        await using var ms = new MemoryStream();

        // Act
        var results = await source.WriteToChunked(ms).ToList().ToTask();
        var combined = results.Combine();

        // Assert
        Assert.True(combined.IsSuccess);
        ms.Position = 0;
        var written = await ms.ReadBytesToEnd();
        Assert.Equal(data, written);
    }

    [Fact]
    public async Task WriteTo_handles_empty()
    {
        // Arrange
        var empty = Array.Empty<byte>();
        var source = ByteSource.FromBytes(empty, bufferSize: 64);
        await using var ms = new MemoryStream();

        // Act
        var result = await source.WriteTo(ms);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, ms.Length);
    }
}