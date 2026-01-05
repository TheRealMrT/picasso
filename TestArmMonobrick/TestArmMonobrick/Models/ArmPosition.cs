namespace TestArmMonobrick.Models;

/// <summary>
/// Represents a position in 2D Cartesian space
/// </summary>
public record struct CartesianPosition(double X, double Y);

/// <summary>
/// Represents the joint angles of the robot arm in degrees
/// </summary>
public record struct JointAngles(double Shoulder, double Elbow);

/// <summary>
/// Represents the current state of the robot arm
/// </summary>
public class ArmState
{
    public CartesianPosition CurrentPosition { get; set; }
    public CartesianPosition TargetPosition { get; set; }
    public JointAngles CurrentAngles { get; set; }
    public bool IsConnected { get; set; }
    public bool IsHomed { get; set; }
    public bool IsMoving { get; set; }
}
