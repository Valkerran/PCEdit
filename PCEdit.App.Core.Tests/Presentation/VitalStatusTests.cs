using PCEdit.App.Core.Presentation;

namespace PCEdit.App.Core.Tests.Presentation;

public sealed class VitalStatusTests
{
    [Theory]
    [InlineData(19.9, VitalLevel.Critical)]
    [InlineData(20.0, VitalLevel.Low)]
    [InlineData(49.9, VitalLevel.Low)]
    [InlineData(50.0, VitalLevel.Ok)]
    [InlineData(1E+30, VitalLevel.Ok)]
    public void Classify_LowIsBad(double value, VitalLevel expected)
    {
        Assert.Equal(expected, VitalStatus.Classify(value, null));
    }

    [Theory]
    [InlineData(19.9, VitalLevel.Ok)]
    [InlineData(20.0, VitalLevel.Low)]
    [InlineData(50.0, VitalLevel.Critical)]
    public void Classify_HighIsBad(double value, VitalLevel expected)
    {
        Assert.Equal(expected, VitalStatus.Classify(value, VitalStatus.HighIsBadParameter));
    }
}
