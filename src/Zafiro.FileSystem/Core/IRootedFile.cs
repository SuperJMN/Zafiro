using Zafiro.DivineBytes;

namespace Zafiro.FileSystem.Core;

public interface IRootedFile : INamedByteSource, IRooted<INamedByteSource>;