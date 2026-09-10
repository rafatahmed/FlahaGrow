using FlahaGrow.Core.Annual;
using Xunit;

namespace FlahaGrow.Core.Tests.Annual;

public sealed class AnnualSkySubdivisionTests
{
    [Theory]
    [InlineData(AnnualSkySubdivision.Tregenza, "h=r1")]
    [InlineData(AnnualSkySubdivision.Reinhart, "h=r4")]
    public void ReceiverMatchesTheWeatherMatrixSubdivision(int subdivision, string expected) =>
        Assert.Equal(expected, AnnualSkySubdivision.ReceiverDirective(subdivision));

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    public void UnsupportedSubdivisionIsRejected(int subdivision) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => AnnualSkySubdivision.Validate(subdivision));
}
