using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using TestArmMonobrick.ViewModels;

namespace TestArmMonobrick.Controls;

/// <summary>
/// Custom control that displays the robot arm workspace and allows clicking to set target positions
/// </summary>
public class WorkspaceCanvas : Control
{
    private MainWindowViewModel? _viewModel;

    public static readonly StyledProperty<double> CurrentXProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(CurrentX));

    public static readonly StyledProperty<double> CurrentYProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(CurrentY));

    public static readonly StyledProperty<double> TargetXProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(TargetX));

    public static readonly StyledProperty<double> TargetYProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(TargetY));

    public static readonly StyledProperty<double> MaxReachProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(MaxReach), 270);

    public static readonly StyledProperty<double> MinReachProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(MinReach), 30);

    public static readonly StyledProperty<double> UpperArmLengthProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(UpperArmLength), 150);

    public static readonly StyledProperty<double> ForearmLengthProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(ForearmLength), 120);

    public static readonly StyledProperty<double> ShoulderAngleProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(ShoulderAngle), 90);

    public static readonly StyledProperty<double> ElbowAngleProperty =
        AvaloniaProperty.Register<WorkspaceCanvas, double>(nameof(ElbowAngle), 90);

    public double CurrentX
    {
        get => GetValue(CurrentXProperty);
        set => SetValue(CurrentXProperty, value);
    }

    public double CurrentY
    {
        get => GetValue(CurrentYProperty);
        set => SetValue(CurrentYProperty, value);
    }

    public double TargetX
    {
        get => GetValue(TargetXProperty);
        set => SetValue(TargetXProperty, value);
    }

    public double TargetY
    {
        get => GetValue(TargetYProperty);
        set => SetValue(TargetYProperty, value);
    }

    public double MaxReach
    {
        get => GetValue(MaxReachProperty);
        set => SetValue(MaxReachProperty, value);
    }

    public double MinReach
    {
        get => GetValue(MinReachProperty);
        set => SetValue(MinReachProperty, value);
    }

    public double UpperArmLength
    {
        get => GetValue(UpperArmLengthProperty);
        set => SetValue(UpperArmLengthProperty, value);
    }

    public double ForearmLength
    {
        get => GetValue(ForearmLengthProperty);
        set => SetValue(ForearmLengthProperty, value);
    }

    public double ShoulderAngle
    {
        get => GetValue(ShoulderAngleProperty);
        set => SetValue(ShoulderAngleProperty, value);
    }

    public double ElbowAngle
    {
        get => GetValue(ElbowAngleProperty);
        set => SetValue(ElbowAngleProperty, value);
    }

    static WorkspaceCanvas()
    {
        AffectsRender<WorkspaceCanvas>(
            CurrentXProperty, CurrentYProperty,
            TargetXProperty, TargetYProperty,
            MaxReachProperty, MinReachProperty,
            UpperArmLengthProperty, ForearmLengthProperty,
            ShoulderAngleProperty, ElbowAngleProperty);
    }

    public WorkspaceCanvas()
    {
        ClipToBounds = true;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel = DataContext as MainWindowViewModel;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (_viewModel == null) return;

        var point = e.GetPosition(this);
        var (worldX, worldY) = ScreenToWorld(point.X, point.Y);
        
        _viewModel.SetTargetPosition(worldX, worldY);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        double size = Math.Min(bounds.Width, bounds.Height);
        double scale = size / (MaxReach * 2.2);
        double centerX = bounds.Width / 2;
        double centerY = bounds.Height / 2;

        // Background
        context.FillRectangle(Brushes.White, new Rect(0, 0, bounds.Width, bounds.Height));

        // Draw reachable workspace (annulus)
        var outerCirclePen = new Pen(Brushes.LightGray, 2, lineCap: PenLineCap.Round);
        var innerCirclePen = new Pen(Brushes.LightGray, 2, lineCap: PenLineCap.Round);
        
        // Outer reach circle
        context.DrawEllipse(
            new SolidColorBrush(Color.FromArgb(30, 0, 150, 0)),
            outerCirclePen,
            new Point(centerX, centerY),
            MaxReach * scale,
            MaxReach * scale);

        // Inner unreachable circle
        context.DrawEllipse(
            Brushes.White,
            innerCirclePen,
            new Point(centerX, centerY),
            MinReach * scale,
            MinReach * scale);

        // Draw coordinate axes
        var axisPen = new Pen(Brushes.LightGray, 1);
        context.DrawLine(axisPen, new Point(0, centerY), new Point(bounds.Width, centerY));
        context.DrawLine(axisPen, new Point(centerX, 0), new Point(centerX, bounds.Height));

        // Draw the arm segments
        DrawArm(context, centerX, centerY, scale);

        // Draw current position (green dot)
        var (currentScreenX, currentScreenY) = WorldToScreen(CurrentX, CurrentY);
        context.DrawEllipse(
            Brushes.Green,
            new Pen(Brushes.DarkGreen, 2),
            new Point(currentScreenX, currentScreenY),
            8, 8);

        // Draw target position (red crosshair)
        var (targetScreenX, targetScreenY) = WorldToScreen(TargetX, TargetY);
        var targetPen = new Pen(Brushes.Red, 2);
        context.DrawLine(targetPen, 
            new Point(targetScreenX - 10, targetScreenY), 
            new Point(targetScreenX + 10, targetScreenY));
        context.DrawLine(targetPen, 
            new Point(targetScreenX, targetScreenY - 10), 
            new Point(targetScreenX, targetScreenY + 10));
        context.DrawEllipse(
            null,
            targetPen,
            new Point(targetScreenX, targetScreenY),
            6, 6);

        // Draw origin marker
        context.DrawEllipse(
            Brushes.Black,
            null,
            new Point(centerX, centerY),
            4, 4);
    }

    private void DrawArm(DrawingContext context, double centerX, double centerY, double scale)
    {
        // Calculate arm segment positions using same formula as InverseKinematics.CalculatePosition
        double shoulderRad = ShoulderAngle * Math.PI / 180.0;
        
        // Convert motor angle back to elbow angle
        double elbowAngleDeg = 180.0 - ElbowAngle;
        double elbowAngleRad = elbowAngleDeg * Math.PI / 180.0;

        // Elbow position
        double elbowX = UpperArmLength * Math.Cos(shoulderRad);
        double elbowY = UpperArmLength * Math.Sin(shoulderRad);

        // Tip position - forearm angle for elbow-up configuration
        double forearmAngle = shoulderRad - (Math.PI - elbowAngleRad);
        double tipX = elbowX + ForearmLength * Math.Cos(forearmAngle);
        double tipY = elbowY + ForearmLength * Math.Sin(forearmAngle);

        // Convert to screen coordinates
        var (elbowScreenX, elbowScreenY) = WorldToScreen(elbowX, elbowY);
        var (tipScreenX, tipScreenY) = WorldToScreen(tipX, tipY);

        // Draw upper arm (shoulder to elbow)
        var armPen = new Pen(Brushes.DarkBlue, 6, lineCap: PenLineCap.Round);
        context.DrawLine(armPen, new Point(centerX, centerY), new Point(elbowScreenX, elbowScreenY));

        // Draw forearm (elbow to tip)
        var forearmPen = new Pen(Brushes.Blue, 5, lineCap: PenLineCap.Round);
        context.DrawLine(forearmPen, new Point(elbowScreenX, elbowScreenY), new Point(tipScreenX, tipScreenY));

        // Draw joints
        context.DrawEllipse(Brushes.Orange, new Pen(Brushes.DarkOrange, 2), new Point(centerX, centerY), 6, 6);
        context.DrawEllipse(Brushes.Orange, new Pen(Brushes.DarkOrange, 2), new Point(elbowScreenX, elbowScreenY), 5, 5);
    }

    private (double x, double y) WorldToScreen(double worldX, double worldY)
    {
        var bounds = Bounds;
        double size = Math.Min(bounds.Width, bounds.Height);
        double scale = size / (MaxReach * 2.2);
        double centerX = bounds.Width / 2;
        double centerY = bounds.Height / 2;

        // Note: Y is flipped because screen Y increases downward
        return (centerX + worldX * scale, centerY - worldY * scale);
    }

    private (double x, double y) ScreenToWorld(double screenX, double screenY)
    {
        var bounds = Bounds;
        double size = Math.Min(bounds.Width, bounds.Height);
        double scale = size / (MaxReach * 2.2);
        double centerX = bounds.Width / 2;
        double centerY = bounds.Height / 2;

        // Note: Y is flipped because screen Y increases downward
        return ((screenX - centerX) / scale, (centerY - screenY) / scale);
    }
}
