using Xunit;
using Moq;

public class Tests
{
    [Fact]
    public void SimpleFact() { }

    [Fact]
    public void MockAvailable()
    {
        var m = new Moq.Mock<System.IDisposable>();
        Assert.NotNull(m);
    }
}
