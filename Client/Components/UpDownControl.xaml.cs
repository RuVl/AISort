using System.Windows;
using System.Windows.Controls;

namespace Client.Components;

public partial class UpDownControl : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(int), typeof(UpDownControl),
            new PropertyMetadata(0));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(UpDownControl),
            new PropertyMetadata(int.MinValue));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(UpDownControl),
            new PropertyMetadata(int.MaxValue));

    public UpDownControl()
    {
        InitializeComponent();
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set
        {
            int newValue = value;
            if (newValue > Maximum) newValue = Maximum;
            if (newValue < Minimum) newValue = Minimum;
            SetValue(ValueProperty, newValue);
        }
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        if (Value < Maximum) Value++;
    }

    private void DownButton_Click(object sender, RoutedEventArgs e)
    {
        if (Value > Minimum) Value--;
    }
}