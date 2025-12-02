using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using CSharpFunctionalExtensions;
using Zafiro.CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes.Tests;

public class ReadAllTests
{
    [Fact]
    public async Task ReadAll_succeeds_with_valid_source()
    {
        // Arrange
        var data = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 256)).ToArray();
        var source = ByteSource.FromBytes(data, bufferSize: 1024);

        // Act
        var result = await source.ReadAll();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(data, result.Value);
    }

    [Fact]
    public async Task ReadAll_captures_errors_as_failure()
    {
        // Arrange
        var source = new ByteSource(Observable.Throw<byte[]>(new InvalidOperationException("Test error")));

        // Act
        var result = await source.ReadAll();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Test error", result.Error);
    }

    [Fact]
    public async Task ReadAll_handles_empty_source()
    {
        // Arrange
        var empty = Array.Empty<byte>();
        var source = ByteSource.FromBytes(empty);

        // Act
        var result = await source.ReadAll();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ReadAllText_succeeds_with_utf8()
    {
        // Arrange
        var text = "Hello, World! 你好 🌍";
        var source = ByteSource.FromString(text, Encoding.UTF8);

        // Act
        var result = await source.ReadAllText(Encoding.UTF8);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(text, result.Value);
    }

    [Fact]
    public async Task ReadAllText_uses_utf8_by_default()
    {
        // Arrange
        var text = "Default encoding test";
        var source = ByteSource.FromString(text);

        // Act
        var result = await source.ReadAllText();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(text, result.Value);
    }

    [Fact]
    public async Task ReadAllText_captures_errors_as_failure()
    {
        // Arrange
        var source = new ByteSource(Observable.Throw<byte[]>(new IOException("Network failure")));

        // Act
        var result = await source.ReadAllText();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Network failure", result.Error);
    }

    [Fact]
    public async Task ToResultSequence_emits_successes_for_valid_chunks()
    {
        // Arrange
        var data = Enumerable.Range(0, 5000).Select(i => (byte)(i % 256)).ToArray();
        var source = ByteSource.FromBytes(data, bufferSize: 1024);

        // Act
        var results = await source.ToResultSequence().ToList().ToTask();

        // Assert
        Assert.All(results, r => Assert.True(r.IsSuccess));
        var allBytes = results.Successes().SelectMany(chunk => chunk).ToArray();
        Assert.Equal(data, allBytes);
    }

    [Fact]
    public async Task ToResultSequence_emits_failure_on_error()
    {
        // Arrange
        var source = new ByteSource(Observable.Throw<byte[]>(new TimeoutException("Request timed out")));

        // Act
        var results = await source.ToResultSequence().ToList().ToTask();

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsFailure);
        Assert.Contains("Request timed out", results[0].Error);
    }

    [Fact]
    public async Task ToResultSequence_completes_without_OnError()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3 };
        var source = ByteSource.FromBytes(data);

        // Act & Assert (no exception should be thrown)
        var completed = false;
        await source.ToResultSequence().Do(_ => { }, () => completed = true).ToTask();
        Assert.True(completed);
    }

    [Fact]
    public async Task ToResultSequence_allows_functional_error_handling()
    {
        // Arrange
        var validChunk = new byte[] { 1, 2, 3 };
        var observable = Observable.Return(validChunk)
            .Concat(Observable.Throw<byte[]>(new Exception("Simulated error")));
        var source = new ByteSource(observable);

        // Act
        var successCount = 0;
        var failureCount = 0;

        await source.ToResultSequence()
            .Do(result =>
            {
                if (result.IsSuccess) successCount++;
                if (result.IsFailure) failureCount++;
            })
            .ToTask();

        // Assert
        Assert.Equal(1, successCount);
        Assert.Equal(1, failureCount);
    }
}
