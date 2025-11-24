using System.Text;
using System.Threading;

namespace Zafiro.FileSystem.Tests;

public class HashCompareStrategyTests
{
    [Fact]
    public async Task Same_hash_are_equal()
    {
        var sut = new HashCompareStrategy();
        IZafiroFile a = new TestFile(Result.Success(new[] { (HashMethod.Md5, "Hash")}));
        IZafiroFile b = new TestFile(Result.Success(new[] { (HashMethod.Md5, "Hash")}));
        var result = await sut.Compare(a, b);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Different_hash_are_different()
    {
        var sut = new HashCompareStrategy();
        IZafiroFile a = new TestFile(Result.Success(new[] { (HashMethod.Md5, "Hash") }));
        IZafiroFile b = new TestFile(Result.Success(new[] { (HashMethod.Md5, "Yo!") }));
        var result = await sut.Compare(a, b);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Multiple_hashes_with_matching_has_are_equal()
    {
        var sut = new HashCompareStrategy();
        IZafiroFile a = new TestFile(Result.Success(new[] { (HashMethod.Md5, "Hash"), (HashMethod.Sha256, "MATCH") }));
        IZafiroFile b = new TestFile(Result.Success(new[] { (HashMethod.Md5, "3234"), (HashMethod.Sha256, "MATCH") }));
        var result = await sut.Compare(a, b);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Multiple_hashes_without_matching_has_are_different()
    {
        var sut = new HashCompareStrategy();
        IZafiroFile a = new TestFile(Result.Success(new[] { (HashMethod.Md5, "Hash"), (HashMethod.Sha256, "Other hash") }));
        IZafiroFile b = new TestFile(Result.Success(new[] { (HashMethod.Md5, "Don't match"), (HashMethod.Sha256, "No no no") }));
        var result = await sut.Compare(a, b);
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task Failure()
    {
        var sut = new HashCompareStrategy();
        IZafiroFile a = new TestFile(Result.Failure<(HashMethod, string)[]>("Error"));
        IZafiroFile b = new TestFile(Result.Success(new[] { (HashMethod.Md5, "Don't match"), (HashMethod.Sha256, "No no no") }));
        var result = await sut.Compare(a, b);
        result.IsSuccess.Should().BeFalse();
    }

    public class TestFile : IZafiroFile
    {
        public TestFile(Result<(HashMethod, string)[]> hashes)
        {
            var result = hashes.Map(tuples => (IDictionary<HashMethod, byte[]>)tuples.ToDictionary(x => x.Item1, x => Encoding.UTF8.GetBytes(x.Item2)));
            Hashes = Task.FromResult(result);
        }

        public IObservable<byte> Contents { get; }
        public Task<Result<bool>> Exists { get; }
        public Path Path { get; }
        public Task<Result<FileProperties>> Properties { get; }
        public Task<Result<IDictionary<HashMethod, byte[]>>> Hashes { get; }
        public IFileSystemRoot FileSystem { get; }
        public Task<Result> Delete() => throw new NotImplementedException();

        public Task<Result> SetContents(IObservable<byte> contents, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Result<Stream>> GetData() => throw new NotImplementedException();
        public Task<Result> SetData(Stream stream, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Result> SetData(Stream data) => throw new NotImplementedException();
    }
}