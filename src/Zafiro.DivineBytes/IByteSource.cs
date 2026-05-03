using CSharpFunctionalExtensions;

namespace Zafiro.DivineBytes;

public interface IByteSource : IObservable<byte[]>
{
    IObservable<byte[]> Bytes { get; }

    /// <summary>
    /// Gets the total number of bytes this source will emit when it is known without consuming the source.
    /// </summary>
    /// <remarks>
    /// This value is metadata, not a command to enumerate the stream. Leave it empty when the size cannot be
    /// known cheaply. Do not compute it by calling blocking or buffering methods such as <c>GetSize().Wait()</c>,
    /// <c>Array()</c>, or <c>ReadAll()</c>; doing so can deadlock reactive pipelines or load large payloads into
    /// memory.
    /// </remarks>
    Maybe<long> Length { get; }
}
