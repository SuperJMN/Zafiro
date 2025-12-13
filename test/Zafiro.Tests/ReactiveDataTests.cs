using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using Zafiro.Reactive;

namespace Zafiro.Tests
{
    public class ReactiveDataTests
    {
        [Fact]
        public async Task ToObservable_disposes_stream_on_completion()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var observable = stream.ToObservable(1);

            await observable.ToList();

            // Currently fails because stream is not disposed
            Assert.False(stream.CanRead);
        }

        [Fact]
        public async Task ToObservable_disposes_stream_on_unsubscription()
        {
            var stream = new MemoryStream(new byte[100]);
            var observable = stream.ToObservable(10);

            using (observable.Subscribe())
            {
            }

            // Currently fails because stream is not disposed
            Assert.False(stream.CanRead);
        }

        [Fact]
        public async Task ToObservable_disposes_stream_on_error()
        {
            var stream = new ErrorStream();
            var observable = stream.ToObservable(10);

            try
            {
                await observable.ToList();
            }
            catch
            {
                // Expected
            }

            // Currently fails because stream is not disposed
            Assert.False(stream.CanRead);
        }

        private class ErrorStream : MemoryStream
        {
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
            {
                throw new Exception("Test exception");
            }
            // Add other overrides if needed...
        }

        [Fact]
        public async Task ToObservableMemory_disposes_stream_on_completion()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var observable = stream.ToObservableMemory(1);

            await observable.ToList();

            Assert.False(stream.CanRead);
        }

        [Fact]
        public async Task ToObservableMemory_recycles_buffers()
        {
            var data = new byte[] { 1, 2, 3, 4, 5, 6 };
            var stream = new MemoryStream(data);
            var observable = stream.ToObservableMemory(2);

            var chunks = await observable.ToList();

            Assert.Equal(3, chunks.Count);
            // Verify alternating buffers mechanism: Chunk 1 and 3 should act on the same backing array (buffer1) if recycled correctly
            // Note: Since they come from ArrayPool, they are actually different instances if the pool behaves normally, 
            // BUT ToObservableMemory explicitly rotates between two RENTED arrays.
            // Check that we are indeed getting Memory from the same backing objects for interleaved items.

            // To be precise: ToObservableMemory rents TWO buffers at start and swaps them.
            // So chunk[0] and chunk[2] should theoretically share the underlying array reference? 
            // Memory<T> equality is tricky, we need to inspect the underlying object.

            global::System.Runtime.InteropServices.MemoryMarshal.TryGetArray<byte>(chunks[0], out var segment0);
            global::System.Runtime.InteropServices.MemoryMarshal.TryGetArray<byte>(chunks[1], out var segment1);
            global::System.Runtime.InteropServices.MemoryMarshal.TryGetArray<byte>(chunks[2], out var segment2);

            // They must be different (buffer1 vs buffer2)
            Assert.NotSame(segment0.Array, segment1.Array);

            // Chunk 2 should reuse the buffer from Chunk 0
            Assert.Same(segment0.Array, segment2.Array);
        }
    }
}
