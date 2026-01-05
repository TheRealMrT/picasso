using System;
using TestArmMonobrick.Models;

namespace TestArmMonobrick.Kinematics;

/// <summary>
/// Inverse Kinematics solver for a 2-joint robot arm (shoulder and elbow)
/// Uses geometric approach for a 2-link planar arm
/// </summary>
public class InverseKinematics
{
    /// <summary>
    /// Length of the upper arm segment (shoulder to elbow) in mm
    /// </summary>
    public double UpperArmLength { get; }

    /// <summary>
    /// Length of the forearm segment (elbow to tip) in mm
    /// </summary>
    public double ForearmLength { get; }

    /// <summary>
    /// Maximum reach of the arm
    /// </summary>
    public double MaxReach => UpperArmLength + ForearmLength;

    /// <summary>
    /// Minimum reach of the arm (when fully folded)
    /// </summary>
    public double MinReach => Math.Abs(UpperArmLength - ForearmLength);

    public InverseKinematics(double upperArmLength, double forearmLength)
    {
        if (upperArmLength <= 0 || forearmLength <= 0)
            throw new ArgumentException("Arm segment lengths must be positive");

        UpperArmLength = upperArmLength;
        ForearmLength = forearmLength;
    }

    /// <summary>
    /// Calculate joint angles for a given target position using inverse kinematics
    /// </summary>
    /// <param name="target">Target position in Cartesian coordinates</param>
    /// <param name="elbowUp">If true, use elbow-up configuration; otherwise elbow-down</param>
    /// <returns>Joint angles in degrees, or null if position is unreachable</returns>
    public JointAngles? CalculateAngles(CartesianPosition target, bool elbowUp = true)
    {
        double x = target.X;
        double y = target.Y;
        double l1 = UpperArmLength;
        double l2 = ForearmLength;

        // Distance from origin to target
        double distance = Math.Sqrt(x * x + y * y);

        // Check if target is reachable
        if (distance > MaxReach || distance < MinReach)
        {
            return null;
        }

        // Handle the edge case at origin
        if (distance < 0.001)
        {
            return null;
        }

        // Calculate elbow angle using law of cosines
        // c² = a² + b² - 2ab*cos(C)
        double cosElbow = (l1 * l1 + l2 * l2 - distance * distance) / (2 * l1 * l2);
        
        // Clamp to valid range to handle floating point errors
        cosElbow = Math.Clamp(cosElbow, -1.0, 1.0);
        
        double elbowAngleRad = Math.Acos(cosElbow);
        
        // For elbow-down configuration, negate the elbow angle
        if (!elbowUp)
        {
            elbowAngleRad = -elbowAngleRad;
        }

        // Calculate shoulder angle
        double angleToTarget = Math.Atan2(y, x);
        
        // Angle from upper arm to line connecting shoulder to target
        double cosInnerAngle = (l1 * l1 + distance * distance - l2 * l2) / (2 * l1 * distance);
        cosInnerAngle = Math.Clamp(cosInnerAngle, -1.0, 1.0);
        double innerAngle = Math.Acos(cosInnerAngle);

        double shoulderAngleRad;
        if (elbowUp)
        {
            shoulderAngleRad = angleToTarget + innerAngle;
        }
        else
        {
            shoulderAngleRad = angleToTarget - innerAngle;
        }

        // Convert to degrees
        double shoulderAngleDeg = RadToDeg(shoulderAngleRad);
        double elbowAngleDeg = RadToDeg(elbowAngleRad);

        // The elbow angle is relative to the upper arm, so we convert it to the angle
        // the elbow motor needs to rotate (typically 180 - angle for physical setup)
        double elbowMotorAngle = 180.0 - elbowAngleDeg;

        return new JointAngles(shoulderAngleDeg, elbowMotorAngle);
    }

    /// <summary>
    /// Calculate the tip position from joint angles (forward kinematics)
    /// </summary>
    /// <param name="angles">Current joint angles in degrees</param>
    /// <returns>Cartesian position of the arm tip</returns>
    public CartesianPosition CalculatePosition(JointAngles angles)
    {
        double shoulderRad = DegToRad(angles.Shoulder);
        
        // Convert motor angle back to elbow angle (reverse of what IK does)
        // IK does: elbowMotorAngle = 180 - elbowAngleDeg
        // So: elbowAngleDeg = 180 - elbowMotorAngle
        double elbowAngleDeg = 180.0 - angles.Elbow;
        double elbowAngleRad = DegToRad(elbowAngleDeg);

        // Position of elbow joint
        double elbowX = UpperArmLength * Math.Cos(shoulderRad);
        double elbowY = UpperArmLength * Math.Sin(shoulderRad);

        // The elbow angle is the internal angle between upper arm and forearm
        // For elbow-up configuration (which IK uses by default):
        // The forearm points at angle = shoulderAngle - (PI - elbowAngle)
        // This is because the elbow "bends" the arm
        double forearmAngle = shoulderRad - (Math.PI - elbowAngleRad);
        
        double tipX = elbowX + ForearmLength * Math.Cos(forearmAngle);
        double tipY = elbowY + ForearmLength * Math.Sin(forearmAngle);

        return new CartesianPosition(tipX, tipY);
    }

    /// <summary>
    /// Check if a position is within the reachable workspace
    /// </summary>
    public bool IsReachable(CartesianPosition target)
    {
        double distance = Math.Sqrt(target.X * target.X + target.Y * target.Y);
        return distance <= MaxReach && distance >= MinReach;
    }

    private static double RadToDeg(double radians) => radians * 180.0 / Math.PI;
    private static double DegToRad(double degrees) => degrees * Math.PI / 180.0;
}
