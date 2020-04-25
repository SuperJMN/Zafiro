﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Zafiro.Core.Mixins;

namespace Zafiro.Core
{
    public interface IOperationProgress
    {
        ISubject<double> Percentage { get; set; }
        ISubject<long> Value { get; set; }
    }

    public static class NewStreamMixin
    {
        private const int DefaultBufferSize = 4096;

        public static IObservable<byte[]> ReadToEndObservable(this Stream stream, int bufferSize = DefaultBufferSize)
            =>
                Observable.Defer<byte[]>(() =>
                {
                    var bytesRead = -1;
                    var bytes = new byte[bufferSize];
                    return
                        Observable.While(
                            () => bytesRead != 0,
                            Observable
                                .FromAsync(() => stream.ReadAsync(bytes, 0, bufferSize))
                                .Do(x =>
                                {
                                    bytesRead = x;
                                })
                                .Select(x => bytes.Take(x).ToArray()));
                });
    }

    public class Downloader : IDownloader
    {
        private readonly HttpClient client;

        public Downloader(HttpClient client)
        {
            this.client = client;
        }

        public async Task Download(string url, string path, IOperationProgress progressObserver = null, int timeout = 30)
        {
            using (var fileStream = File.OpenWrite(path))
            {
                await Download(url, fileStream, progressObserver, timeout);
            }
        }

        private async Task Download(string url, Stream destination, IOperationProgress progressObserver = null,
            int timeout = 30)
        {
            long? totalBytes = 0;
            long bytesWritten = 0;

            await ObservableMixin.Using(() => client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead),
                    s =>
                    {
                        totalBytes = s.Content.Headers.ContentLength;
                        if (!totalBytes.HasValue)
                        {
                            progressObserver?.Percentage.OnNext(double.PositiveInfinity);
                        }
                        return ObservableMixin.Using(() => s.Content.ReadAsStreamAsync(),
                            contentStream => contentStream.ReadToEndObservable());
                    })
                .Do(bytes =>
                {
                    bytesWritten += bytes.Length;
                    if (totalBytes.HasValue)
                    {
                        progressObserver?.Percentage.OnNext((double)bytesWritten / totalBytes.Value);
                    }

                    progressObserver?.Value?.OnNext(bytesWritten);
                })
                .Timeout(TimeSpan.FromSeconds(timeout))
                .Select(bytes => Observable.FromAsync(async () =>
                {
                    await destination.WriteAsync(bytes, 0, bytes.Length);
                    return Unit.Default;
                }))
                .Merge(1);
        }

        private static readonly int BufferSize = 8 * 1024;

        public async Task<Stream> GetStream(string url, IOperationProgress progress = null, int timeout = 30)
        {
            var tmpFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            var stream = File.Create(tmpFile, BufferSize, FileOptions.DeleteOnClose);

            await Download(url, stream, progress, timeout);
            stream.Position = 0;
            return stream;
        }
    }
}