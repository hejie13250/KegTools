﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace KegConfig
{
  /// <summary>
  /// DoubleNumericUpDown.xaml 的交互逻辑
  /// </summary>
  public partial class DoubleNumericUpDown : UserControl
  {
    public static readonly DependencyProperty ValueProperty =
           DependencyProperty.Register("Value", typeof(double), typeof(DoubleNumericUpDown),
               new FrameworkPropertyMetadata(4.50, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValuePropertyChanged));

    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register("MinValue", typeof(double), typeof(DoubleNumericUpDown), new PropertyMetadata(0.0));

    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register("MaxValue", typeof(double), typeof(DoubleNumericUpDown), new PropertyMetadata(100.0));

    public event RoutedPropertyChangedEventHandler<double> ValueChanged;

    public double Value
    {
      get { return (double)GetValue(ValueProperty); }
      set { SetValue(ValueProperty, value); }
    }

    public double MinValue
    {
      get { return (double)GetValue(MinValueProperty); }
      set { SetValue(MinValueProperty, value); }
    }

    public double MaxValue
    {
      get { return (double)GetValue(MaxValueProperty); }
      set { SetValue(MaxValueProperty, value); }
    }

    public DoubleNumericUpDown()
    {
      InitializeComponent();
      this.DataContext = this;
    }

    private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      var numericUpDown = (DoubleNumericUpDown)d;
      var oldValue = (double)e.OldValue;
      var newValue = (double)e.NewValue;

      if (numericUpDown.ValueChanged == null) return;
      // 创建事件参数并调用事件
      var args = new RoutedPropertyChangedEventArgs<double>(oldValue, newValue);
      numericUpDown.ValueChanged(numericUpDown, args);
    }

    protected virtual void OnValueChanged(RoutedPropertyChangedEventArgs<double> args)
    {
      ValueChanged?.Invoke(this, args);
    }

    private void Increase_Click(object sender, RoutedEventArgs e)
    {
      if (Value < MaxValue) Value += 0.1;
    }

    private void Decrease_Click(object sender, RoutedEventArgs e)
    {
      if (Value > MinValue) Value -= 0.1;
    }

    private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var step = 0.1; // 设置滚动步长，默认每次滚动增加或减少0.1
      if (Keyboard.Modifiers == ModifierKeys.Control) step = 0.01; // 如果按住Ctrl键，则步长 0.01

      switch (e.Delta)
      {
        // 滚动向上
        case > 0:
        {
          if (Value + step <= MaxValue)
            Value += step;
          break;
        }
        // 滚动向下
        case < 0:
        {
          if (Value - step >= MinValue)
            Value -= step;
          break;
        }
      }

      Value = Math.Round(Value, 4);
      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }

  }
}
