using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestArmMonobrick.Hardware;

/// <summary>
/// Interface for NXT brick communication - allows simulation and real hardware implementations
/// </summary>
public interface INxtBrick : IDisposable
{
    bool IsConnected { get; }
    
    Task<bool> ConnectAsync(string connectionString);
    void Disconnect();
    
    void ResetMotorTacho(MotorPort port);
    void MoveMotor(MotorPort port, int degrees, sbyte speed);
    void StopMotor(MotorPort port);
    int GetMotorTacho(MotorPort port);
}

public enum MotorPort
{
    A,
    B,
    C
}
