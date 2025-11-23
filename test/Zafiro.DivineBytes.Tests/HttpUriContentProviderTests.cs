using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;

namespace Zafiro.DivineBytes.Tests;

public class HttpUriContentProviderTests
{
    [Fact]
    public async Task Uses_single_streaming_request_without_buffering()
    {
        var payload = Encoding.UTF8.GetBytes("streaming-content");
        var handler = new RecordingHandler(payload);
        using var client = new HttpClient(handler);
        var provider = new HttpUriContentProvider(client);
        var uri = new Uri("http://example.com/resource");

        var result = await provider.GetByteSourceAsync(uri);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, handler.StreamRequestCount);
        var collected = await result.Value.Bytes.SelectMany(chunk => chunk).ToArray().ToTask();
        Assert.Equal(payload, collected);
        Assert.Equal(1, handler.SendCount);
        Assert.Equal(1, handler.StreamRequestCount);
    }

    private class RecordingHandler : HttpMessageHandler
    {
        private readonly byte[] payload;

        public RecordingHandler(byte[] payload)
        {
            this.payload = payload;
        }

        public int SendCount { get; private set; }

        public int StreamRequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;

            var content = new RecordingHttpContent(payload, () => StreamRequestCount++);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content,
            };

            return Task.FromResult(response);
        }

        private class RecordingHttpContent : HttpContent
        {
            private readonly byte[] payload;
            private readonly Action onStreamRequested;

            public RecordingHttpContent(byte[] payload, Action onStreamRequested)
            {
                this.payload = payload;
                this.onStreamRequested = onStreamRequested;
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                onStreamRequested();
                return stream.WriteAsync(payload, 0, payload.Length);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = payload.Length;
                return true;
            }

            protected override Task<Stream> CreateContentReadStreamAsync()
            {
                onStreamRequested();
                return Task.FromResult<Stream>(new MemoryStream(payload, writable: false));
            }
        }
    }
}
