using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Zafiro.DivineBytes.Tests;

public class WriteSafetyTests
{
    [Fact]
    public async Task WriteTo_propagates_buffer_mutations()
    {
        var buffer = Enumerable.Range(0, 8).Select(i => (byte)i).ToArray();
        var source = new ByteSource(Observable.Return(buffer));
        using var stream = new GateStream();

        var writeTask = source.WriteTo(stream);

        Array.Fill(buffer, (byte)0xFF);
        stream.Release();

        var result = await writeTask;

        Assert.True(result.IsSuccess);
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, stream.Written.Single());
    }

    [Fact]
    public async Task WriteToSafe_defensively_copies_chunks()
    {
        var buffer = Enumerable.Range(0, 8).Select(i => (byte)i).ToArray();
        var source = new ByteSource(Observable.Return(buffer));
        using var stream = new GateStream();

        var writeTask = source.WriteToSafe(stream);

        Array.Fill(buffer, (byte)0xAA);
        stream.Release();

        var result = await writeTask;

        Assert.True(result.IsSuccess);
        Assert.Equal(Enumerable.Range(0, 8).Select(i => (byte)i).ToArray(), stream.Written.Single());
    }

    private sealed class GateStream : Stream
    {
        private readonly List<byte[]> written = new();
        private readonly TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IReadOnlyList<byte[]> Written => written;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => written.Sum(chunk => chunk.Length);
        public override long Position { get => Length; set => throw new NotSupportedException(); }

        public void Release() => gate.TrySetResult();

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            var copy = new byte[count];
            Array.Copy(buffer, offset, copy, 0, count);
            written.Add(copy);
        }
    }
}
