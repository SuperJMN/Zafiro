using Zafiro.FileSystem.Mutable;
using Zafiro.DivineBytes;

namespace Zafiro.UI;

public interface IFileSystemPicker
{
    Task<Result<IEnumerable<INamedByteSource>>> PickForOpenMultiple(params FileTypeFilter[] filters);
    Task<Result<Maybe<INamedByteSource>>> PickForOpen(params FileTypeFilter[] filters);
    Task<Maybe<IMutableFile>> PickForSave(string desiredName, Maybe<string> defaultExtension, params FileTypeFilter[] filters);
    Task<Maybe<IMutableDirectory>> PickFolder();
}