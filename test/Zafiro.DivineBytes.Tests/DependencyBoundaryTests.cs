namespace Zafiro.DivineBytes.Tests;

public class DependencyBoundaryTests
{
    [Fact]
    public void Byte_stream_consumer_does_not_receive_reactiveui()
    {
        var dependencyManifest = global::System.IO.Path.Combine(
            AppContext.BaseDirectory,
            "Zafiro.DivineBytes.Tests.deps.json");
        var dependencies = global::System.IO.File.ReadAllText(dependencyManifest);

        Assert.DoesNotContain("\"ReactiveUI/", dependencies);
    }
}
