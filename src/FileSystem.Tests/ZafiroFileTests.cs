using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Serilog;
using Xunit;
using Zafiro.FileSystem;

namespace FileSystem.Tests;

public class ZafiroFileTests
{
    [Fact]
    public async Task Content_of_copied_file_should_match()
    {
        var destinationFilesystem = new MockFileSystem();
        var destFs = new ZafiroFileSystem(destinationFilesystem, Maybe<ILogger>.None);
        var origFs = new ZafiroFileSystem(new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["file1.txt"] = "saludos"
        }), Maybe<ILogger>.None);
        var unit =
            from origin in origFs.GetFile("file1.txt")
            from dest in destFs.GetFile("file2.txt")
            select new {dest, origin};

        var result = await unit.Bind(u => u.origin.CopyTo(u.dest)).ConfigureAwait(false);

        result.IsSuccess.Should().BeTrue();
        destinationFilesystem.GetFile("file2.txt").TextContents.Should().Be("saludos");
    }

    [Fact]
    public void Contents_of_directory()
    {
        var originFs = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["c:\\file1.txt"] = "saludos",
            ["c:\\Subdir\\file1.txt"] = "saludos"
        });
        var fs = new ZafiroFileSystem(originFs, Maybe<ILogger>.None);
        var dir = fs.GetDirectory("C:>Subdir");
        dir.IsSuccess.Should().BeTrue();
        dir.Value.Files.Select(x => x.Path).Should().BeEquivalentTo(new[]
        {
            new Path("c:", "Subdir", "file1.txt")
        });
    }

    [Fact]
    public void Get_file()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["c:\\test.txt"] = MockFileData.NullObject
        });
        var sut = new ZafiroFileSystem(fileSystem, Maybe<ILogger>.None);

        var result = sut.GetFile("C:>Test");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Delete_file()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["c:\\test.txt"] = MockFileData.NullObject
        });
        var sut = new ZafiroFileSystem(fileSystem, Maybe<ILogger>.None);
        var result = sut
            .GetFile("C:>test.txt")
            .Bind(r => r.Delete());

        result.IsSuccess.Should().BeTrue();
        fileSystem.GetFile("C:>Test.txt").Should().BeNull();
    }

    [Fact]
    public void Get_directory()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            ["c:\\test.txt"] = MockFileData.NullObject
        });
        var sut = new ZafiroFileSystem(fileSystem, Maybe<ILogger>.None);

        var result = sut.GetDirectory("C:");
        result.IsSuccess.Should().BeTrue();
    }
}
