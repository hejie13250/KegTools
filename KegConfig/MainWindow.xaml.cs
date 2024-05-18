using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using KegConfig.Class;
using KegConfig.Contorl;
using KegConfig.Page;
using LevelDB;
using Newtonsoft.Json;
using WpfAnimatedGif;
using static KegConfig.Page.SchemeSetting;
using GroupBox = System.Windows.Controls.GroupBox;
using Path = System.IO.Path;
using RadioButton = System.Windows.Controls.RadioButton;

namespace KegConfig
{
  /// <summary>
  /// MainWindow.xaml 的交互逻辑
  /// </summary>
  /// 
  public partial class MainWindow
  {
    private static readonly string _appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private StackPanel _leftStackPanel;

    private readonly string[] _方案设置页面 = {
      "候选框配色", "候选框样式", "字体和渲染", "码元设置", "标点设置", "动作设置"  , "顶功设置"  , "上屏设置"  , "中英切换", "翻页按键",
      "词语联想", "码表调频与检索", "重复上屏", "自动造词", "反查方案设置", "临时码表检索", "引导码表检索"
    };
    private readonly string[] _全局设置页面 = {
      "状态条", "在线查找", "外部工具", "全局快捷命令", "快捷命令", "快捷键", "自启动应用", "定时关机", "其它选项"
    };
    private readonly string[] _帮助页面 = { "关于", "全局设置说明" };
    private readonly string[] _打字统计 = { "曲线图", "今日和累计数据" };
    private readonly string[] _码表设置 = { "码表修改" };


    #region 配色方案相关定义

    private readonly string _backgroundFilePath;
    // 定义配色方案类
    public class BackgroundScheme
    {
      public string 名称 { get; set; }
      public bool 显示背景图 { get; set; }
      public int 分组框圆角半径 { get; set; }
      public int 分组框阴影半径 { get; set; }
      public int 分组框阴影深度 { get; set; }
      public string 分组框阴影颜色 { get; set; }
      public string 分组框背景颜色 { get; set; }
      public string 窗口背景颜色 { get; set; }
      public string 分组框前景颜色 { get; set; }
      public string 窗口前景颜色 { get; set; }
      public string 文字字体 { get; set; }
      public string 窗口背景图 { get; set; }
    }
    public class BackgroundSchemeCollection
    {
      public List<BackgroundScheme> 配色方案 { get; } = new();
    }

    private List<BackgroundScheme> _配色方案 = new();

    private BackgroundScheme _colorScheme = new()
    {
      名称 = "默认",
      显示背景图 = false,
      分组框圆角半径 = 20,
      分组框阴影半径 = 10,
      分组框阴影深度 = 2,
      分组框阴影颜色 = "#ff808080",
      分组框背景颜色 = "#33ffffff",
      窗口背景颜色   = "#fffdfdfd",
      分组框前景颜色 = "#ff000000",
      窗口前景颜色   = "#ff000000",
      文字字体       = "微软雅黑",
      窗口背景图     = $"{_appPath}\\background\\默认.jpg"
    };
    #endregion



    #region 初始化
    public MainWindow()
    {
      if (!Directory.Exists($"{_appPath}\\configs")) Directory.CreateDirectory($"{_appPath}\\configs");
      if (!Directory.Exists($"{_appPath}\\background")) Directory.CreateDirectory($"{_appPath}\\background");
      _backgroundFilePath = $"{_appPath}\\configs\\窗口配色方案.json";
      InitializeComponent();

      title.Text = $"{title.Text} v {GetAssemblyVersion()}";  // 标题加上版本号
      Base.GetKegPath();                                      // 获取小科狗主程序目录

      Height = 850;
      frame1.Height = Height - 50;
      frame2.Height = Height - 50;
      frame3.Height = Height - 50;
      frame4.Height = Height - 50;
      frame5.Height = Height - 50;

      方案设置页面();

      frame1.Navigated += Frame1_Navigated;
      frame2.Navigated += Frame2_Navigated;
      frame3.Navigated += Frame3_Navigated;
      frame4.Navigated += Frame4_Navigated;
      frame5.Navigated += Frame5_Navigated;

      //var page = frame3.Content as KegConfig.Page.Abort;
      //if (page != null)
      //  page.BackgroundSwitchRequested += Page_BackgroundSwitchRequested;

      Loaded += MainWindow_Loaded;

    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      colorPicker.ColorChanged += ColorPicker_ColorChanged;
      settingStackPanel.Width = 0;
      读取系统字体列表();
      读取窗口设置();
      LoadJson();
      LoadKegSkinImages();
    }


    // 读取 Json 文件
    void LoadJson()
    {
      if (File.Exists(_backgroundFilePath))
      {
        // 读取整个文件内容,将JSON字符串反序列化为对象
        var jsonString = File.ReadAllText(_backgroundFilePath);
        var colorSchemesJson = JsonConvert.DeserializeObject<BackgroundSchemeCollection>(jsonString);
        _配色方案 = colorSchemesJson.配色方案;

        foreach (var scheme in _配色方案)
        {
          colorSchemeListBox.Items.Add(scheme.名称);
        }
      }
      else
      {
        _配色方案.Add(_colorScheme);
        var jsonString = JsonConvert.SerializeObject(new { 配色方案 = _配色方案 }, Formatting.Indented);
        File.WriteAllText(_backgroundFilePath, jsonString);

        colorSchemeListBox.Items.Add("默认");
      }
    }






    // 获取版本号
    private string GetAssemblyVersion()
    {
      var assembly = Assembly.GetExecutingAssembly();
      var version = assembly.GetName().Version.ToString();
      return version.Substring(0, version.Length - 2);
    }
    #endregion

    #region 点击 RadioButton 跳转到指定的 GroupBox
    private void LeftRadioButton1_Click(object sender, RoutedEventArgs e)
    {
      if (sender is not RadioButton radioButton) return;
      var stackPanel = radioButton.Content as StackPanel;
      var textBlock = stackPanel?.Children.OfType<TextBlock>().FirstOrDefault();

      if (textBlock == null) return;
      switch (textBlock.Text)
      {
        case "方案设置":
          方案设置页面();
          break;
        case "全局设置":
          全局设置页面();
          break;
        case "帮助":
          帮助页面();
          break;
        case "打字统计":
          打字统计();
          break;
        case "码表设置":
          码表设置();
          break;
      }
      SettingToggle.IsChecked = false;
    }

    private void LeftRadioButton2_Click(object sender, RoutedEventArgs e)
    {
      if (sender is not RadioButton radioButton) return;
      var stackPanel = radioButton.Content as StackPanel;
      var textBlock = stackPanel?.Children.OfType<TextBlock>().FirstOrDefault();
      if (textBlock == null) return;
      var name = textBlock.Text;

      if (_方案设置页面.Contains(name)) { 方案设置页面(); ScrollViewerOffset(name, 1); }
      if (_全局设置页面.Contains(name)) { 全局设置页面(); ScrollViewerOffset(name, 2); }
      if (_帮助页面.Contains(name)) { 帮助页面(); ScrollViewerOffset(name, 3); }
      if (_打字统计.Contains(name)) { 打字统计(); ScrollViewerOffset(name, 4); }
      if (_码表设置.Contains(name)) { 码表设置(); ScrollViewerOffset(name, 5); }
      SettingToggle.IsChecked = false;
    }


    private void 方案设置页面()
    {
      SetPageVisibility(
      Visibility.Visible,
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Collapsed,
      leftStackPanel1
      );
    }

    private void 全局设置页面()
    {
      SetPageVisibility(
      Visibility.Collapsed,
      Visibility.Visible,
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Collapsed,
      leftStackPanel2
      );
    }

    private void 帮助页面()
    {
      SetPageVisibility(
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Visible,
      Visibility.Collapsed,
      Visibility.Collapsed,
      leftStackPanel3
      );
    }

    private void 打字统计()
    {
      SetPageVisibility(
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Visible,
      Visibility.Collapsed,
      leftStackPanel4
      );
    }

    private void 码表设置()
    {
      SetPageVisibility(
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Collapsed,
      Visibility.Visible,
      leftStackPanel5
      );

    }

    private void SetPageVisibility(Visibility f1, Visibility f2, Visibility f3, Visibility f4, Visibility f5, StackPanel activePanel)
    {
      frame1.Width = f1 == Visibility.Visible ? 770 : 0;
      frame2.Width = f2 == Visibility.Visible ? 770 : 0;
      frame3.Width = f3 == Visibility.Visible ? 770 : 0;
      frame4.Width = f4 == Visibility.Visible ? 770 : 0;
      frame5.Width = f5 == Visibility.Visible ? 770 : 0;

      frame1.Visibility = f1;
      frame2.Visibility = f2;
      frame3.Visibility = f3;
      frame4.Visibility = f4;
      frame5.Visibility = f5;

      leftStackPanel1.Visibility = Visibility.Collapsed;
      leftStackPanel2.Visibility = Visibility.Collapsed;
      leftStackPanel3.Visibility = Visibility.Collapsed;
      leftStackPanel4.Visibility = Visibility.Collapsed;
      leftStackPanel5.Visibility = Visibility.Collapsed;

      activePanel.Visibility = Visibility.Visible;
      _leftStackPanel = activePanel;
    }

    /// <summary>
    /// 点击左侧的控件偏移右侧滚动条（滚动页面）
    /// </summary>
    /// <param name="content"></param>
    /// <param name="n">右侧第几个滚动条</param>
    private void ScrollViewerOffset(string content, int n)
    {
      GroupBox groupBox = null;

      // 根据n的值，选择正确的Frame
      if (n == 1)
      {
        var page = frame1.Content as System.Windows.Controls.Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      else if (n == 2)
      {
        var page = frame2.Content as System.Windows.Controls.Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      else if (n == 3)
      {
        var page = frame3.Content as System.Windows.Controls.Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      else if (n == 4)
      {
        var page = frame4.Content as System.Windows.Controls.Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      else if (n == 5)
      {
        var page = frame5.Content as System.Windows.Controls.Page;
        var groupBoxStackPanel = page?.FindName("groupBoxStackPanel") as StackPanel;
        groupBox = FindGroupBox(content, groupBoxStackPanel);
      }
      // 如果找到了GroupBox，将其滚动到视图中
      groupBox?.BringIntoView();
    }


    /// <summary>
    /// 查找指定容器 StackPanel 内指定 content 的 GroupBox
    /// </summary>
    /// <param name="content">GroupBox的content</param>
    /// <param name="groupBoxStackPanel"></param>
    /// <returns></returns>
    private GroupBox FindGroupBox(string content, Panel groupBoxStackPanel)
    {
      foreach (var child in groupBoxStackPanel.Children)
      {
        if (child is GroupBox groupBox && groupBox.Header.ToString() == content)
        {
          return groupBox;
        }
      }
      return null;
    }

    #endregion

    #region 鼠标移到 GroupBox 时选中指定 RadioButton
    private void Frame1_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is SchemeSetting page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }
    private void Frame2_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is GlobalSetting page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }
    private void Frame3_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is Abort page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }
    private void Frame4_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is KegStatistics page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }
    private void Frame5_Navigated(object sender, NavigationEventArgs e)
    {
      if (e.Content is ModifyTable page)
        page.NameOfSelectedGroupBoxChanged += Frame_NameOfSelectedGroupBoxChanged;
    }


    private void Frame_NameOfSelectedGroupBoxChanged(object sender, string e)
    {
      foreach (var child in _leftStackPanel.Children)
      {
        if (child is not RadioButton radioButton) continue;
        var textBlock = FindChildTextBlock(radioButton);
        if (textBlock != null && textBlock.Text.Equals(e, StringComparison.OrdinalIgnoreCase))
          radioButton.IsChecked = true;
        else
          radioButton.IsChecked = false;
      }
    }



    private TextBlock FindChildTextBlock(DependencyObject parent)
    {
      for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
      {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is TextBlock textBlock) return textBlock;

        var result = FindChildTextBlock(child);
        if (result != null) return result;
      }
      return null;
    }

    #endregion

    #region 窗口相关
    // 移动窗口
    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
      // 只有当用户按下左键时才处理
      if (e.LeftButton != MouseButtonState.Pressed) return;
      DragMove(); // 调用API使窗口跟随鼠标移动
    }

    // 窗口最小化
    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
      WindowState = WindowState.Minimized;
    }

    // 退出
    private void Button_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void SettingToggle_Checked(object sender, RoutedEventArgs e)
    {
      settingStackPanel.Width = 770;
      frameStackPanel.Width = 0;
    }

    private void SettingToggle_Unchecked(object sender, RoutedEventArgs e)
    {
      settingStackPanel.Width = 0;
      frameStackPanel.Width = 770;
    }


    #endregion

    #region 窗口设置



    ResourceDictionary appResources = Application.Current.Resources;

    private int _selectColorLabelNum;    // 用于记录当前选中的 select_color_label
    private bool isUpDate = false;

    private void 读取窗口设置()
    {
      bool isNumberValid1 = int.TryParse(Base.GetValue("window", "groupBoxRadius"), out var groupBoxRadius);
      bool isNumberValid2 = int.TryParse(Base.GetValue("window", "groupBoxBlurRadius"), out var groupBoxBlurRadius);
      bool isNumberValid3 = int.TryParse(Base.GetValue("window", "groupBoxShadowDepth"), out var groupBoxShadowDepth);
      var groupBoxShadowColor   = Base.GetValue("window", "groupBoxShadowDepth");
      var groupBoxBackground    = Base.GetValue("window", "groupBoxBackground");
      var windowBackground      = Base.GetValue("window", "windowBackground");
      var groupBoxForeground    = Base.GetValue("window", "groupBoxForeground");
      var windowForeground      = Base.GetValue("window", "windowForeground");
      var windowFontFamily      = Base.GetValue("window", "windowFontFamily");
      var windowBackgroundImage = Base.GetValue("window", "windowBackgroundImage");

      nud1.Value = (int)(isNumberValid1 ? groupBoxRadius : 20);
      nud2.Value = (int)(isNumberValid2 ? groupBoxBlurRadius : 10);
      nud2.Value = (int)(isNumberValid3 ? groupBoxShadowDepth : 2);
      color_Label_1.Background = groupBoxShadowColor != "" ? StringToColor(groupBoxShadowColor) : StringToColor("#ff808080");
      color_Label_2.Background = groupBoxBackground  != "" ? StringToColor(groupBoxBackground)  : StringToColor("#33ffffff");
      color_Label_3.Background = windowBackground    != "" ? StringToColor(windowBackground)    : StringToColor("#fffdfdfd");
      color_Label_4.Background = groupBoxForeground  != "" ? StringToColor(groupBoxForeground)  : StringToColor("#ff000000");
      color_Label_5.Background = windowForeground    != "" ? StringToColor(windowForeground)    : StringToColor("#ff000000");

      SetFontComboBox(windowFontFamily);

      if (windowBackgroundImage == "")
        SetImage($"{_appPath}\\background\\默认.jpg");
      else
        SetImage(windowBackgroundImage);

      BackgroundCheckBox.IsChecked = Base.GetValue("window", "showBackground") == "1";

      saveConfig();
    }


    private void SetFontComboBox(string font)
    {
      if (font == "") return;

      for (var i = 0; i < fontComboBox.Items.Count; i++)
      {
        if (fontComboBox.Items[i].ToString() == font)
        {
          Console.WriteLine(fontComboBox.Items[i].ToString());
          fontComboBox.SelectedIndex = i;
        }
      }
    }


    private SolidColorBrush StringToColor(string str)
    {
      return new SolidColorBrush((Color)ColorConverter.ConvertFromString(str)!);
    }

    private void LoadKegSkinImages()
    {
      // 获取应用的相对路径并转换为绝对路径
      var directoryPath = _appPath + "\\background\\";

      // 设置皮肤图片路径
      var skin = $"{directoryPath}默认.jpg";

      // 将皮肤图片设置为图像源
      try
      {
        SetImage(Base.GetValue("window", "windowBackgroundImage"));
      }
      catch
      {
        SetImage(skin);
      }

      // 定义支持的图片扩展名数组
      var imageExtensions = new[] { ".png", ".jpg" };

      // 获取目录下所有具有指定扩展名的图片文件
      var allFiles = Directory.GetFiles(directoryPath)
                           .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

      // 查找默认皮肤文件，如果存在则将其从原始文件列表中移除
      var enumerable = allFiles as string[] ?? allFiles.ToArray();
      var defaultFile = enumerable.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == "默认");

      List<string> files;
      if (defaultFile != null)
      {
        // 移除默认皮肤文件后，将其添加到新列表的首位
        files = enumerable.Except(new[] { defaultFile }).ToList();
        files.Insert(0, defaultFile);
      }
      else
      {
        // 若默认皮肤文件不存在，则直接使用所有符合条件的文件列表
        files = enumerable.ToList();
      }

      // 遍历处理后的图片文件集合
      foreach (var file in files)
      {
        // 从文件路径中提取图片名称
        var imageName = Path.GetFileName(file);

        // 将图片名称添加到皮肤列表框中
        skinListBox.Items.Add(imageName);
      }
    }

    private void SetImage(string imagePath)
    {
      if (string.IsNullOrEmpty(imagePath)) return;
      BitmapImage image = new();
      image.BeginInit();
      image.UriSource = new Uri(imagePath);
      image.EndInit();

      // 检查文件扩展名以确定是否为GIF
      //if (Path.GetExtension(imagePath).Equals(".gif", StringComparison.OrdinalIgnoreCase))
      //{
      //  ImageBehavior.SetAnimatedSource(displayImage, image);
      //}
      //else
      //{
      //  // 清除动画状态
      //  ImageBehavior.SetAnimatedSource(displayImage, null);
      //  displayImage.Source = image;
      //}

      ImageBehavior.SetAnimatedSource(displayImage, null);
      displayImage.Source = image;

    }



    private void ColorPicker_ColorChanged(object sender, ColorChangedEventArgs e)
    {
      if (colorPicker.RgbColor == null) return;
      rgbTextBox.RGBText = colorPicker.RgbText;
      SetColorLableColor(colorPicker.RgbColor);
      UpdateBorderProperties();
    }
    private void SetColorLableColor(SolidColorBrush cColor)
    {
      Label[] colorLabels = { color_Label_1, color_Label_2, color_Label_3, color_Label_4, color_Label_5 };
      // 计算反色
      var currentColor = cColor.Color;
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
      var invertedColor2 = Color.FromArgb((byte)slider.Value, (byte)currentColor.R, (byte)currentColor.G, (byte)currentColor.B);
      for (var i = 1; i <= colorLabels.Length; i++)
        if (i == _selectColorLabelNum)
        {
          colorLabels[i - 1].BorderBrush = new SolidColorBrush(invertedColor);
          colorLabels[i - 1].Background = new SolidColorBrush(invertedColor2);
        }
    }



    // 显示颜色的 label 鼠标进入事件
    private void Color_label_MouseEnter(object sender, MouseEventArgs e)
    {
      isUpDate = false;
      color_Label_001.Visibility = Visibility.Hidden;
      color_Label_002.Visibility = Visibility.Hidden;
      color_Label_003.Visibility = Visibility.Hidden;
      color_Label_004.Visibility = Visibility.Hidden;
      color_Label_005.Visibility = Visibility.Hidden;

      if (sender is not Label lb) return;
      switch (lb.Name)
      {
        case "color_Label_1":
          _selectColorLabelNum = 1;
          color_Label_001.Visibility = Visibility.Visible;
          break;
        case "color_Label_2":
          _selectColorLabelNum = 2;
          color_Label_002.Visibility = Visibility.Visible;
          break;
        case "color_Label_3":
          _selectColorLabelNum = 3;
          color_Label_003.Visibility = Visibility.Visible;
          break;
        case "color_Label_4":
          _selectColorLabelNum = 4;
          color_Label_004.Visibility = Visibility.Visible;
          break;
        case "color_Label_5":
          _selectColorLabelNum = 5;
          color_Label_005.Visibility = Visibility.Visible;
          break;
      }

      var currentColor = ((SolidColorBrush)lb.Background).Color;
      slider.Value = currentColor.A;
      // 计算反色
      var invertedColor = Color.FromArgb((byte)~currentColor.A, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
      lb.BorderThickness = new Thickness(3);
      lb.BorderBrush = new SolidColorBrush(invertedColor);
      var hex = Base.RemoveChars(lb.Background.ToString(), 2);
      var rgb = Base.HexToRgb(hex);
      rgbTextBox.RGBText = rgb;
    }

    // 显示颜色的 label 鼠标离开事件
    private void Color_label_MouseLeave(object sender, MouseEventArgs e)
    {
      isUpDate = false;
      if (sender is Label label)
      label.BorderThickness = new Thickness(2);
    }

    private void Contorl_MouseEnter(object sender, MouseEventArgs e)
    {
      isUpDate = true;
    }
    private void Contorl_MouseLeave(object sender, MouseEventArgs e)
    {
      isUpDate = false;
    }
    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (!isUpDate || slider == null || _selectColorLabelNum == 0) return;
      
      Label[] colorLabels = { color_Label_1, color_Label_2, color_Label_3, color_Label_4, color_Label_5 };

      for (var i = 1; i <= colorLabels.Length; i++)
        if (i == _selectColorLabelNum)
        {
          var currentColor = ((SolidColorBrush)colorLabels[i - 1].Background).Color;
          var invertedColor = Color.FromArgb((byte)slider.Value, (byte)currentColor.R, (byte)currentColor.G, (byte)currentColor.B);
          var newColorBrush = new SolidColorBrush(invertedColor);
          colorLabels[i - 1].Background = newColorBrush;
          UpdateBorderProperties();
        }
    }

    private void Alpha_Slider_MouseUp(object sender, MouseButtonEventArgs e)
    {
      if (sender is not Slider slider) return;
      var point = e.GetPosition(slider);
      // 计算点击位置相对于Slider的比例
      slider.Value = (point.X / slider.ActualWidth) * slider.Maximum;
      //slider.Value = slider.Maximum - newValue;  // 反转值，因为Slider垂直方向是从上到下
    }

    private void Alpha_Slider_MouseWheel(object sender, MouseWheelEventArgs e)
    {
      var step = 5;
      if (Keyboard.Modifiers == ModifierKeys.Control) step *= 10;

      switch (e.Delta)
      {
        case > 0 when slider.Value + step <= slider.Maximum:
          slider.Value -= step;
          break;
        case < 0 when slider.Value - step >= slider.Minimum:
          slider.Value += step;
          break;
      }
      // 阻止滚轮事件继续向上冒泡
      e.Handled = true;
    }



    private void nud1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
      if (nud1 is null) return;
      if (!isUpDate) return;
      UpdateBorderProperties();
    }


    private void UpdateBorderProperties()
    {
      if (appResources["CustomBorderStyle"] is not Style originalStyle) return;

      // 创建一个新的Style对象，基于原始样式
      var newStyle = new Style(typeof(Border))
      {
        BasedOn = originalStyle
      };

      bool dropShadowEffectModified = false;
      DropShadowEffect newDropShadowEffect = null;

      // 遍历原始样式的Setters，复制到新样式中并在必要时修改
      foreach (Setter originalSetter in originalStyle.Setters.Cast<Setter>())
      {
        Setter newSetter = new Setter(originalSetter.Property, originalSetter.Value);

        // 如果找到DropShadowEffect，修改它
        if (originalSetter.Value is DropShadowEffect currentEffect)
        {
          dropShadowEffectModified = true;
          if (color_Label_1 is null) return;
          var currentColor = ((SolidColorBrush)color_Label_1.Background).Color;
          var invertedColor = Color.FromArgb((byte)currentColor.A, (byte)currentColor.R, (byte)currentColor.G, (byte)currentColor.B);

          newDropShadowEffect = new DropShadowEffect
          {
            BlurRadius = nud2.Value,
            Opacity = 0.5,
            ShadowDepth = nud3.Value,
            Color = new SolidColorBrush(invertedColor).Color
          };

          newSetter.Value = newDropShadowEffect;
        }
        // 如果是CornerRadius，也进行修改
        else if (originalSetter.Property == Border.CornerRadiusProperty)
        {
          if (nud1 is null) return;
          newSetter.Value = new CornerRadius(nud1.Value);
        }
        // 修改Background
        else if (originalSetter.Property == Border.BackgroundProperty)
        {
          if (color_Label_1 is null) return;
          var backgroundBrush = color_Label_2.Background;
          newSetter.Value = backgroundBrush;
        }
        newStyle.Setters.Add(newSetter);
      }

      // 如果之前没有找到DropShadowEffect，且确实需要添加或修改
      if (!dropShadowEffectModified && newDropShadowEffect != null)
      {
        // 添加新的DropShadowEffect到Setters
        newStyle.Setters.Add(new Setter(EffectProperty, newDropShadowEffect));
      }

      // 更新资源字典中的"CustomBorderStyle"为新样式
      appResources["CustomBorderStyle"] = newStyle;

      UpdateColorProperties();
    }


    private void UpdateColorProperties()
    {
      // 获取资源字典
      ResourceDictionary resources = Application.Current.Resources;

      // 获取颜色资源
      var primaryBackgroundBrush = resources["PrimaryBackground"] as SolidColorBrush;
      var textForegroundBrush = resources["TextForeground"] as SolidColorBrush;

      // 修改颜色
      if (primaryBackgroundBrush != null && color_Label_3 != null)
      {
        var newColor = (color_Label_3.Background as SolidColorBrush)?.Color ?? Colors.Transparent;
        resources["PrimaryBackground"] = new SolidColorBrush(newColor);
      }

      if (textForegroundBrush != null && color_Label_4 != null)
      {
        var newColor = (color_Label_4.Background as SolidColorBrush)?.Color ?? Colors.Black;
        resources["TextForeground"] = new SolidColorBrush(newColor);
      }

      if (textForegroundBrush != null && color_Label_5 != null)
      {
        var newColor = (color_Label_5.Background as SolidColorBrush)?.Color ?? Colors.Black;
        resources["TextForeground2"] = new SolidColorBrush(newColor);
      }
    }

    private void nud2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
      if (color_Label_1 is null) return;
      if (!isUpDate) return;
      UpdateBorderProperties();
    }

    private void nud3_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
      if (color_Label_1 is null) return;
      if (!isUpDate) return;
      System.Windows.Media.Brush backgroundBrush = color_Label_1.Background;

      if (backgroundBrush is SolidColorBrush solidColorBrush)
      {
        Color backgroundColor = solidColorBrush.Color;
        UpdateBorderProperties();
      }
    }


    private void Apply_button_Click(object sender, RoutedEventArgs e)
    {
      saveConfig();
    }

    private void saveConfig()
    {
      var font = fontComboBox.SelectedIndex > -1 ? fontComboBox.SelectedValue.ToString() : "微软雅黑";
      Base.SetValue("window", "groupBoxRadius", nud1.Value.ToString());
      Base.SetValue("window", "groupBoxBlurRadius", nud2.Value.ToString());
      Base.SetValue("window", "groupBoxShadowDepth", nud3.Value.ToString());
      Base.SetValue("window", "groupBoxShadowDepth", color_Label_1.Background.ToString());
      Base.SetValue("window", "groupBoxBackground", color_Label_2.Background.ToString());
      Base.SetValue("window", "windowBackground", color_Label_3.Background.ToString());
      Base.SetValue("window", "groupBoxForeground", color_Label_4.Background.ToString());
      Base.SetValue("window", "windowForeground", color_Label_5.Background.ToString());
      Base.SetValue("window", "windowFontFamily", font);
      Base.SetValue("window", "windowBackgroundImage", displayImage.Source.ToString());
    }


    private void fontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
      {
        var fontName = fontComboBox.SelectedItem.ToString();
        var font = new FontFamily(fontName);
        Application.Current.Resources["FontFamilyResource"] = font;
      }
    }
    private void fontComboBox_MouseEnter(object sender, MouseEventArgs e)
    {
      isUpDate = true;
      var cb = sender as ComboBox;
      cb?.Focus();
    }


    /// <summary>
    /// 读取系统字体列表
    /// </summary>
    private void 读取系统字体列表()
    {
      foreach (var font in System.Drawing.FontFamily.Families)
      {
        //if (排除字体名称里没有中文的字体(font.Name))
          fontComboBox.Items.Add(font.Name);
      }
    }

    /// <summary>
    /// 正则排除 字体名称 里没有中文的字体
    /// </summary>
    /// <param name="fontName"></param>
    /// <returns></returns>
    private bool 排除字体名称里没有中文的字体(string fontName)
    {
      Regex chineseRegex = new(@"[\u4e00-\u9fff]");
      return chineseRegex.IsMatch(fontName);
    }

    // 新建配色方案
    private void MenuItem_Click_1(object sender, RoutedEventArgs e)
    {
      saveButton.Content = "保存配色";
      saveButton.Visibility = Visibility.Visible;
      color_Scheme_Name_TextBox.Visibility = Visibility.Visible;
    }

    // 修改配色方案
    private void MenuItem_Click_2(object sender, RoutedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem == null)
      {
        MessageBox.Show("您没有选中任何配色！",
        "修改操作",
        MessageBoxButton.OK,
        MessageBoxImage.Question);
        return;
      }
      saveButton.Content = "修改配色";
      saveButton.Visibility = Visibility.Visible;
      color_Scheme_Name_TextBox.Visibility = Visibility.Visible;
      color_Scheme_Name_TextBox.Text += colorSchemeListBox.SelectedItem.ToString();
    }

    // 删除选中配色方案
    private void MenuItem_Click_3(object sender, RoutedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem == null)
      {
        MessageBox.Show("您没有选中任何配色！",
        "删除操作",
        MessageBoxButton.OK,
        MessageBoxImage.Question);
        return;
      }
      var name = colorSchemeListBox.SelectedItem.ToString();
      var result = MessageBox.Show(
      $"您确定要删除 {name} 吗？",
      "删除操作",
      MessageBoxButton.OKCancel,
      MessageBoxImage.Question);

      if (result == MessageBoxResult.OK)
      {
        _配色方案.RemoveAt(colorSchemeListBox.SelectedIndex);
        var jsonString = JsonConvert.SerializeObject(new { 配色方案 = _配色方案 }, Formatting.Indented);
        File.WriteAllText(_backgroundFilePath, jsonString);

        colorSchemeListBox.Items.Remove(name);
        colorSchemeListBox.Items.Refresh();
      }

    }
    // 配色列表双击事件
    private void ColorSchemeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton != MouseButton.Left || colorSchemeListBox.SelectedItem == null) return;
      var schemeColor = _配色方案[colorSchemeListBox.SelectedIndex];
      BackgroundCheckBox.IsChecked = schemeColor.显示背景图;
      nud1.Value = schemeColor.分组框圆角半径;
      nud2.Value = schemeColor.分组框阴影半径;
      nud3.Value = schemeColor.分组框阴影深度;
      color_Label_1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.分组框阴影颜色)!);
      color_Label_2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.分组框背景颜色)!);
      color_Label_3.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.窗口背景颜色)!);
      color_Label_4.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.分组框前景颜色)!);
      color_Label_5.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.窗口前景颜色)!);

      SetFontComboBox(schemeColor.文字字体);

      isUpDate = true;
      UpdateBorderProperties();
      isUpDate = false;
    }

    // 配色列表选中项改变事件
    private void ColorSchemeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (colorSchemeListBox.SelectedItem == null) return;
      if (saveButton.Content.ToString() == "保存配色")
        color_Scheme_Name_TextBox.Text = "";
      if (saveButton.Content.ToString() == "修改配色")
        color_Scheme_Name_TextBox.Text = colorSchemeListBox.SelectedItem.ToString();
    }

    // 添加配色
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {

      var name = color_Scheme_Name_TextBox.Text.Trim();
      _colorScheme = new BackgroundScheme
      {
        名称 = name,
        显示背景图     = BackgroundCheckBox.IsChecked != null && (bool)BackgroundCheckBox.IsChecked,
        分组框圆角半径 = nud1.Value,
        分组框阴影半径 = nud2.Value,
        分组框阴影深度 = nud3.Value,
        分组框阴影颜色 = color_Label_1.Background.ToString(),
        分组框背景颜色 = color_Label_2.Background.ToString(),
        窗口背景颜色   = color_Label_3.Background.ToString(),
        分组框前景颜色 = color_Label_4.Background.ToString(),
        窗口前景颜色   = color_Label_5.Background.ToString(),
        文字字体       = fontComboBox.SelectedValue.ToString(),
        窗口背景图     = displayImage.Source.ToString()
      };

      if (saveButton.Content.ToString() == "保存配色")
      {
        foreach (var item in colorSchemeListBox.Items)
        {
          if (item.ToString() == name)
          {
            MessageBox.Show("存在同名配色！");
            return;
          }
          if (color_Scheme_Name_TextBox.Text.Length == 0)
          {
            MessageBox.Show("请输入新的配色名称！");
            color_Scheme_Name_TextBox.Focus();
            return;
          }
        }
        _配色方案.Insert(0, _colorScheme);
        colorSchemeListBox.Items.Insert(0, name);
      }
      if (saveButton.Content.ToString() == "修改配色" && colorSchemeListBox.SelectedItem != null)
      {
        var n = colorSchemeListBox.SelectedIndex;
        _配色方案[n] = _colorScheme;
        colorSchemeListBox.Items.Clear();

        foreach (var scheme in _配色方案)
          colorSchemeListBox.Items.Add(scheme.名称);

        colorSchemeListBox.SelectedIndex = n;
      }
      var jsonString = JsonConvert.SerializeObject(new { 配色方案 = _配色方案 }, Formatting.Indented);
      File.WriteAllText(_backgroundFilePath, jsonString);
    }
    #endregion

    private void skinListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      var selectedItem = (string)skinListBox.SelectedItem;
      var selectedImagePath = _appPath + "\\background\\" + selectedItem;

      SetImage(selectedImagePath);
    }

    private void BackgroundCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      // 切换到ImageBrush
      this.Background = (Brush)this.Resources["DefaultBrush"];
    }

    private void BackgroundCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      // 切换到动态资源PrimaryBackground
      this.Background = (Brush)FindResource("PrimaryBackground");
    }


  }
}
