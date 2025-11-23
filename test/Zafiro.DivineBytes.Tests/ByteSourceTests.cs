using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Zafiro.DivineBytes.Tests;

public class ByteSourceTests
{
    [Fact]
    public async Task ToByteStream_respects_buffer_boundaries()
    {
        var data = Enumerable.Range(0, 10_000).Select(i => (byte)(i % 256)).ToArray();

        var chunks = await data.ToByteStream(bufferSize: 1024).ToList().ToTask();

        Assert.All(chunks.Take(chunks.Count - 1), chunk => Assert.Equal(1024, chunk.Count()));
        Assert.Equal(data.Length % 1024, chunks.Last().Count());
        Assert.Equal(data, chunks.SelectMany(x => x).ToArray());
    }

    [Fact]
    public async Task FromStream_keeps_stream_open_when_requested()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        var trackingStream = new TrackingStream(new MemoryStream(payload));
        var source = ByteSource.FromStream(trackingStream, leaveOpen: true, bufferSize: 2);

        var collected = await source.Bytes.SelectMany(chunk => chunk).ToArray().ToTask();

        Assert.Equal(payload, collected);
        Assert.False(trackingStream.IsDisposed);
    }

    [Fact]
    public async Task FromStream_disposes_stream_by_default()
    {
        var payload = new byte[] { 10, 20, 30 };
        var trackingStream = new TrackingStream(new MemoryStream(payload));
        var source = ByteSource.FromStream(trackingStream, bufferSize: 2);

        var collected = await source.Bytes.SelectMany(chunk => chunk).ToArray().ToTask();

        Assert.Equal(payload, collected);
        Assert.True(trackingStream.IsDisposed);
    }

    private sealed class TrackingStream : Stream
    {
        private readonly Stream inner;

        public TrackingStream(Stream inner)
        {
            this.inner = inner;
        }

        public bool IsDisposed { get; private set; }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => inner.WriteAsync(buffer, offset, count, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
