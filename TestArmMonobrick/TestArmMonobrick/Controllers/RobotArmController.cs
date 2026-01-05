using System;
using System.Threading;
using System.Threading.Tasks;
using TestArmMonobrick.Hardware;
using TestArmMonobrick.Kinematics;
using TestArmMonobrick.Models;

namespace TestArmMonobrick.Controllers;

/// <summary>
/// Controls the NXT robot arm using abstracted hardware interface
/// </summary>
public class RobotArmController : IDisposable
{
    private INxtBrick? _brick;
    private readonly InverseKinematics _kinematics;
    private bool _isConnected;
    private bool _isHomed;
    private JointAngles _currentAngles;
    private readonly object _lockObj = new();

    // Motor ports configuration
    private const MotorPort ShoulderMotor = MotorPort.A;
    private const MotorPort ElbowMotor = MotorPort.B;

    // Motor configuration - adjust these based on your gear ratios
    private const double DegreesPerMotorDegree_Shoulder = 1.0; // Gear ratio for shoulder
    private const double DegreesPerMotorDegree_Elbow = 1.0;    // Gear ratio for elbow

    // Home position angles (when homed, this is where the arm is)
    public JointAngles HomeAngles { get; set; } = new JointAngles(90, 90);

    // Movement speed (0-100)
    public sbyte MotorSpeed { get; set; } = 50;

    public event EventHandler<ArmState>? StateChanged;

    public bool IsConnected => _isConnected;
    public bool IsHomed => _isHomed;
    public JointAngles CurrentAngles => _currentAngles;

    public RobotArmController(double upperArmLength, double forearmLength)
    {
        _kinematics = new InverseKinematics(upperArmLength, forearmLength);
        _currentAngles = HomeAngles;
    }

    public InverseKinematics Kinematics => _kinematics;

    /// <summary>
    /// Connect to the NXT brick
    /// </summary>
    /// <param name="connectionString">Connection string: "simulation", "usb", or COM port for Bluetooth</param>
    public async Task<bool> ConnectAsync(string connectionString = "simulation")
    {
        try
        {
            // Only use simulation if explicitly requested
            if (connectionString.Equals("simulation", StringComparison.OrdinalIgnoreCase))
            {
                _brick = new SimulatedNxtBrick();
            }
            else
            {
                // Real hardware requested but not implemented yet
                // Return false to indicate connection failed
                throw new NotImplementedException(
                    $"Real NXT connection ({connectionString}) is not yet implemented. " +
                    "Use 'Simulation' mode for testing, or add MonoBrick library for real hardware.");
            }

            bool success = await _brick.ConnectAsync(connectionString);
            
            if (success)
            {
                lock (_lockObj)
                {
                    _isConnected = true;
                    
                    // Reset motor tacho counters
                    _brick.ResetMotorTacho(ShoulderMotor);
                    _brick.ResetMotorTacho(ElbowMotor);
                    
                    RaiseStateChanged();
                }
            }
            
            return success;
        }
        catch (Exception)
        {
            _isConnected = false;
            throw; // Re-throw so the ViewModel can show the error message
        }
    }

    /// <summary>
    /// Disconnect from the NXT brick
    /// </summary>
    public void Disconnect()
    {
        lock (_lockObj)
        {
            if (_brick != null)
            {
                try
                {
                    _brick.StopMotor(ShoulderMotor);
                    _brick.StopMotor(ElbowMotor);
                    _brick.Disconnect();
                }
                catch { }
                _brick.Dispose();
                _brick = null;
            }
            _isConnected = false;
            _isHomed = false;
            RaiseStateChanged();
        }
    }

    /// <summary>
    /// Home the robot arm - moves to home position and resets coordinates
    /// </summary>
    public async Task HomeAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _brick == null)
            throw new InvalidOperationException("Not connected to NXT brick");

        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                // Reset tacho counters - this sets current position as zero
                _brick.ResetMotorTacho(ShoulderMotor);
                _brick.ResetMotorTacho(ElbowMotor);
                
                _currentAngles = HomeAngles;
                _isHomed = true;
                RaiseStateChanged();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Move the arm tip to the specified Cartesian position
    /// </summary>
    public async Task<bool> MoveToPositionAsync(CartesianPosition target, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _brick == null)
            throw new InvalidOperationException("Not connected to NXT brick");

        if (!_isHomed)
            throw new InvalidOperationException("Arm must be homed before moving");

        var targetAngles = _kinematics.CalculateAngles(target);
        if (targetAngles == null)
        {
            return false; // Position unreachable
        }

        await MoveToAnglesAsync(targetAngles.Value, cancellationToken);
        return true;
    }

    /// <summary>
    /// Move the arm to specific joint angles
    /// </summary>
    public async Task MoveToAnglesAsync(JointAngles targetAngles, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _brick == null)
            throw new InvalidOperationException("Not connected to NXT brick");

        await Task.Run(() =>
        {
            lock (_lockObj)
            {
                // Calculate the delta from current position
                double shoulderDelta = targetAngles.Shoulder - _currentAngles.Shoulder;
                double elbowDelta = targetAngles.Elbow - _currentAngles.Elbow;

                // Convert to motor degrees (accounting for gear ratio)
                int shoulderMotorDegrees = (int)(shoulderDelta / DegreesPerMotorDegree_Shoulder);
                int elbowMotorDegrees = (int)(elbowDelta / DegreesPerMotorDegree_Elbow);

                // Move motors
                if (Math.Abs(shoulderMotorDegrees) > 1)
                {
                    _brick.MoveMotor(ShoulderMotor, shoulderMotorDegrees, MotorSpeed);
                }

                if (Math.Abs(elbowMotorDegrees) > 1)
                {
                    _brick.MoveMotor(ElbowMotor, elbowMotorDegrees, MotorSpeed);
                }

                _currentAngles = targetAngles;
                RaiseStateChanged();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Get the current Cartesian position of the arm tip
    /// </summary>
    public CartesianPosition GetCurrentPosition()
    {
        return _kinematics.CalculatePosition(_currentAngles);
    }

    /// <summary>
    /// Stop all motors immediately
    /// </summary>
    public void Stop()
    {
        if (_brick == null) return;

        lock (_lockObj)
        {
            try
            {
                _brick.StopMotor(ShoulderMotor);
                _brick.StopMotor(ElbowMotor);
            }
            catch { }
            RaiseStateChanged();
        }
    }

    private void RaiseStateChanged()
    {
        var state = new ArmState
        {
            IsConnected = _isConnected,
            IsHomed = _isHomed,
            CurrentAngles = _currentAngles,
            CurrentPosition = GetCurrentPosition(),
            IsMoving = false
        };
        StateChanged?.Invoke(this, state);
    }

    public void Dispose()
    {
        Disconnect();
    }
}
