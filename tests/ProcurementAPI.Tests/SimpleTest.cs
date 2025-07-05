using Xunit;

namespace ProcurementAPI.Tests;

public class SimpleTest
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AnotherSimpleTest_ShouldAlsoPass()
    {
        // Arrange
        var expected = 42;

        // Act
        var actual = 42;

        // Assert
        Assert.Equal(expected, actual);
    }
}