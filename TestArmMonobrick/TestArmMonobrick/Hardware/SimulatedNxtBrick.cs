using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestArmMonobrick.Hardware;

/// <summary>
/// Simulated NXT brick for testing without actual hardware
/// </summary>
public class SimulatedNxtBrick : INxtBrick
{
    private bool _isConnected;
    private readonly Dictionary<MotorPort, int> _motorPositions = new()
    {
        { MotorPort.A, 0 },
        { MotorPort.B, 0 },
        { MotorPort.C, 0 }
    };

    public bool IsConnected => _isConnected;

    public Task<bool> ConnectAsync(string connectionString)
    {
        return Task.Run(() =>
        {
            // Simulate connection delay
            Thread.Sleep(500);
            _isConnected = true;
            return true;
        });
    }

    public void Disconnect()
    {
        _isConnected = false;
    }

    public void ResetMotorTacho(MotorPort port)
    {
        _motorPositions[port] = 0;
    }

    public void MoveMotor(MotorPort port, int degrees, sbyte speed)
    {
        if (!_isConnected) return;
        
        // Simulate motor movement with delay proportional to degrees
        int absSpeed = Math.Abs((int)speed);
        int delayMs = Math.Abs(degrees) * 10 / Math.Max(1, absSpeed);
        Thread.Sleep(Math.Min(delayMs, 2000));
        
        _motorPositions[port] += degrees;
    }

    public void StopMotor(MotorPort port)
    {
        // In simulation, motors stop immediately
    }

    public int GetMotorTacho(MotorPort port)
    {
        return _motorPositions[port];
    }

    public void Dispose()
    {
        Disconnect();
    }
}
