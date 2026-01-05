using TestArmMonobrick.Kinematics;
using TestArmMonobrick.Models;

namespace TestArmMonobrick.Tests;

public class InverseKinematicsTests
{
    private readonly InverseKinematics _ik;
    private const double UpperArmLength = 150.0;
    private const double ForearmLength = 120.0;
    private const double Tolerance = 0.1; // mm tolerance for floating point comparisons

    public InverseKinematicsTests()
    {
        _ik = new InverseKinematics(UpperArmLength, ForearmLength);
    }

    [Fact]
    public void MaxReach_ShouldBeSumOfArmLengths()
    {
        Assert.Equal(270.0, _ik.MaxReach);
    }

    [Fact]
    public void MinReach_ShouldBeDifferenceOfArmLengths()
    {
        Assert.Equal(30.0, _ik.MinReach);
    }

    [Theory]
    [InlineData(270, 0)]    // Max reach along X axis
    [InlineData(0, 270)]    // Max reach along Y axis
    [InlineData(150, 150)]  // Diagonal position
    [InlineData(2, 150)]    // The failing case from user
    [InlineData(100, 100)]  // Mid-range position
    [InlineData(-100, 100)] // Negative X
    [InlineData(0, 150)]    // Along Y axis
    public void RoundTrip_CalculateAngles_ThenPosition_ShouldReturnOriginal(double targetX, double targetY)
    {
        // Arrange
        var target = new CartesianPosition(targetX, targetY);
        
        // Act
        var angles = _ik.CalculateAngles(target);
        
        // Assert - angles should be calculated
        Assert.NotNull(angles);
        
        // Act - convert back to position
        var resultPosition = _ik.CalculatePosition(angles.Value);
        
        // Assert - should match original target
        Assert.True(Math.Abs(resultPosition.X - targetX) < Tolerance,
            $"X mismatch: Expected {targetX}, got {resultPosition.X}. Angles: Shoulder={angles.Value.Shoulder:F2}°, Elbow={angles.Value.Elbow:F2}°");
        Assert.True(Math.Abs(resultPosition.Y - targetY) < Tolerance,
            $"Y mismatch: Expected {targetY}, got {resultPosition.Y}. Angles: Shoulder={angles.Value.Shoulder:F2}°, Elbow={angles.Value.Elbow:F2}°");
    }

    [Fact]
    public void SpecificCase_X2_Y150_ShouldRoundTrip()
    {
        // This is the specific failing case
        var target = new CartesianPosition(2.0, 150.0);
        
        // Calculate angles
        var angles = _ik.CalculateAngles(target);
        Assert.NotNull(angles);
        
        // Log the angles for debugging
        var shoulder = angles.Value.Shoulder;
        var elbow = angles.Value.Elbow;
        
        // Calculate position from angles
        var result = _ik.CalculatePosition(angles.Value);
        
        // Output for debugging
        Assert.True(Math.Abs(result.X - target.X) < Tolerance,
            $"X mismatch: Target=({target.X}, {target.Y}), " +
            $"Angles=(Shoulder={shoulder:F2}°, Elbow={elbow:F2}°), " +
            $"Result=({result.X:F2}, {result.Y:F2})");
        Assert.True(Math.Abs(result.Y - target.Y) < Tolerance,
            $"Y mismatch: Target=({target.X}, {target.Y}), " +
            $"Angles=(Shoulder={shoulder:F2}°, Elbow={elbow:F2}°), " +
            $"Result=({result.X:F2}, {result.Y:F2})");
    }

    [Theory]
    [InlineData(300, 0)]   // Beyond max reach
    [InlineData(0, 300)]   // Beyond max reach
    [InlineData(10, 10)]   // Inside min reach circle (distance ~14.14 < 30)
    [InlineData(0, 0)]     // At origin
    public void CalculateAngles_UnreachablePosition_ShouldReturnNull(double x, double y)
    {
        var target = new CartesianPosition(x, y);
        var angles = _ik.CalculateAngles(target);
        Assert.Null(angles);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(200, 50)]
    public void IsReachable_ValidPosition_ShouldReturnTrue(double x, double y)
    {
        var target = new CartesianPosition(x, y);
        Assert.True(_ik.IsReachable(target));
    }

    [Theory]
    [InlineData(300, 0)]
    [InlineData(0, 0)]
    public void IsReachable_InvalidPosition_ShouldReturnFalse(double x, double y)
    {
        var target = new CartesianPosition(x, y);
        Assert.False(_ik.IsReachable(target));
    }
}
