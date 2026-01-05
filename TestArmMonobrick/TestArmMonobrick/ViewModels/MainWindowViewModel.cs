using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using TestArmMonobrick.Controllers;
using TestArmMonobrick.Models;

namespace TestArmMonobrick.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly RobotArmController _controller;
    
    private bool _isConnected;
    private bool _isHomed;
    private bool _isBusy;
    private double _currentX;
    private double _currentY;
    private double _targetX;
    private double _targetY;
    private double _shoulderAngle;
    private double _elbowAngle;
    private string _statusMessage = "Disconnected";
    private string _selectedConnectionType = "Simulation";
    private string _bluetoothPort = "COM3";

    // Arm dimensions in mm - adjust to match your physical arm
    public double UpperArmLength { get; } = 150.0;
    public double ForearmLength { get; } = 120.0;

    // Connection type options
    public List<string> ConnectionTypes { get; } = new() { "Simulation", "USB", "Bluetooth" };

    public MainWindowViewModel()
    {
        _controller = new RobotArmController(UpperArmLength, ForearmLength);
        _controller.StateChanged += OnControllerStateChanged;
        
        // Initialize target to home position
        var homePos = _controller.Kinematics.CalculatePosition(_controller.HomeAngles);
        _targetX = homePos.X;
        _targetY = homePos.Y;
        _currentX = homePos.X;
        _currentY = homePos.Y;
        _shoulderAngle = _controller.HomeAngles.Shoulder;
        _elbowAngle = _controller.HomeAngles.Elbow;

        ConnectCommand = new RelayCommand(async () => await ConnectAsync(), () => !IsConnected && !IsBusy);
        DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected && !IsBusy);
        HomeCommand = new RelayCommand(async () => await HomeAsync(), () => IsConnected && !IsBusy);
        MoveToTargetCommand = new RelayCommand(async () => await MoveToTargetAsync(), () => IsConnected && IsHomed && !IsBusy);
        StopCommand = new RelayCommand(Stop, () => IsConnected);
    }

    public double MaxReach => _controller.Kinematics.MaxReach;
    public double MinReach => _controller.Kinematics.MinReach;
    public RobotArmController Controller => _controller;

    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); UpdateCommands(); }
    }

    public bool IsHomed
    {
        get => _isHomed;
        set { _isHomed = value; OnPropertyChanged(); UpdateCommands(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); UpdateCommands(); }
    }

    public double CurrentX
    {
        get => _currentX;
        set { _currentX = value; OnPropertyChanged(); }
    }

    public double CurrentY
    {
        get => _currentY;
        set { _currentY = value; OnPropertyChanged(); }
    }

    public double TargetX
    {
        get => _targetX;
        set { _targetX = value; OnPropertyChanged(); }
    }

    public double TargetY
    {
        get => _targetY;
        set { _targetY = value; OnPropertyChanged(); }
    }

    public double ShoulderAngle
    {
        get => _shoulderAngle;
        set { _shoulderAngle = value; OnPropertyChanged(); }
    }

    public double ElbowAngle
    {
        get => _elbowAngle;
        set { _elbowAngle = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string SelectedConnectionType
    {
        get => _selectedConnectionType;
        set 
        { 
            _selectedConnectionType = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(IsBluetoothSelected));
            OnPropertyChanged(nameof(IsSimulationSelected));
        }
    }

    public bool IsBluetoothSelected => SelectedConnectionType == "Bluetooth";
    
    public bool IsSimulationSelected => SelectedConnectionType == "Simulation";

    public string BluetoothPort
    {
        get => _bluetoothPort;
        set { _bluetoothPort = value; OnPropertyChanged(); }
    }

    public string ConnectionString => SelectedConnectionType switch
    {
        "USB" => "usb",
        "Bluetooth" => BluetoothPort,
        _ => "simulation"
    };

    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand HomeCommand { get; }
    public ICommand MoveToTargetCommand { get; }
    public ICommand StopCommand { get; }

    public void SetTargetPosition(double x, double y)
    {
        var target = new CartesianPosition(x, y);
        if (_controller.Kinematics.IsReachable(target))
        {
            TargetX = x;
            TargetY = y;
            StatusMessage = $"Target set: ({x:F1}, {y:F1})";
        }
        else
        {
            StatusMessage = $"Position ({x:F1}, {y:F1}) is unreachable";
        }
    }

    private async Task ConnectAsync()
    {
        IsBusy = true;
        StatusMessage = "Connecting...";
        
        try
        {
            bool success = await _controller.ConnectAsync(ConnectionString);
            if (success)
            {
                StatusMessage = "Connected to NXT";
            }
            else
            {
                StatusMessage = "Failed to connect";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Disconnect()
    {
        _controller.Disconnect();
        StatusMessage = "Disconnected";
    }

    private async Task HomeAsync()
    {
        IsBusy = true;
        StatusMessage = "Homing...";
        
        try
        {
            await _controller.HomeAsync();
            StatusMessage = "Homed successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Homing error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task MoveToTargetAsync()
    {
        IsBusy = true;
        StatusMessage = $"Moving to ({TargetX:F1}, {TargetY:F1})...";
        
        try
        {
            var target = new CartesianPosition(TargetX, TargetY);
            bool success = await _controller.MoveToPositionAsync(target);
            
            if (success)
            {
                StatusMessage = $"Moved to ({TargetX:F1}, {TargetY:F1})";
            }
            else
            {
                StatusMessage = "Target position unreachable";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Move error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Stop()
    {
        _controller.Stop();
        StatusMessage = "Stopped";
    }

    private void OnControllerStateChanged(object? sender, ArmState state)
    {
        // Dispatch to UI thread to avoid cross-thread exceptions
        Dispatcher.UIThread.Post(() =>
        {
            IsConnected = state.IsConnected;
            IsHomed = state.IsHomed;
            CurrentX = state.CurrentPosition.X;
            CurrentY = state.CurrentPosition.Y;
            ShoulderAngle = state.CurrentAngles.Shoulder;
            ElbowAngle = state.CurrentAngles.Elbow;
        });
    }

    private void UpdateCommands()
    {
        (ConnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DisconnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (HomeCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MoveToTargetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (StopCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Func<Task>? _executeAsync;
    private readonly Action? _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter)
    {
        if (_executeAsync != null)
            await _executeAsync();
        else
            _execute?.Invoke();
    }

    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
