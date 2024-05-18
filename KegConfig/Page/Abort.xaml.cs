using System.Reflection;
using System.Windows.Controls;
using System.Windows;
using GroupBox = System.Windows.Controls.GroupBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Effects;
using System;
using System.IO.Ports;
using System.Windows.Media;
using Brushes = System.Drawing.Brushes;
using Color = System.Windows.Media.Color;
using KegConfig.Contorl;
using KegConfig.Class;
using System.Windows.Input;
using System.Text.RegularExpressions;
using FontFamily = System.Windows.Media.FontFamily;

namespace KegConfig.Page
{
  /// <summary>
  /// Abort.xaml 的交互逻辑
  /// </summary>
  public partial class Abort
  {
    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion



    public Abort()
    {
      InitializeComponent();
    }



  }
}
