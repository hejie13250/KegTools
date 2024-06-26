﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using KegConfig.Class;
using KegConfig.Contorl;
using Application = System.Windows.Forms.Application;
using Button = System.Windows.Controls.Button;
using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using GroupBox = System.Windows.Controls.GroupBox;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;

namespace KegConfig.Page
{
  /// <summary>
  /// SchemeSetting.xaml 的交互逻辑
  /// </summary>
  public partial class SchemeSetting
  {
    #region 获取GroupBox的Header用于主窗口导航事件
    private void GroupBox_MouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is not GroupBox groupBox) return;
      NameOfSelectedGroupBox = groupBox.Header.ToString();
    }

    #endregion

    #region 配色方案相关定义

    // 定义配色方案类
    public class ColorScheme
    {
      public string 名称        { get; set; }
      public bool   显示背景图     { get; set; }
      public bool   显示候选窗圆角   { get; set; }
      public bool   显示选中项背景圆角 { get; set; }
      public int    候选窗圆角     { get; set; }
      public int    选中项圆角     { get; set; }
      public int    边框线宽      { get; set; }
      public string 下划线色      { get; set; }
      public string 光标色       { get; set; }
      public string 分隔线色      { get; set; }
      public string 窗口边框色     { get; set; }
      public string 窗背景底色     { get; set; }
      public string 选中背景色     { get; set; }
      public string 选中字体色     { get; set; }
      public string 编码字体色     { get; set; }
      public string 候选字色      { get; set; }
    }
    public class ColorSchemesCollection
    {
      public List<ColorScheme> 配色方案 { get; } = new();
    }

    private List<ColorScheme> _配色方案 = new();

    private ColorScheme _colorScheme = new()
    {
      名称        = "默认",
      显示背景图     = false,
      显示候选窗圆角   = true,
      显示选中项背景圆角 = true,
      候选窗圆角     = 15,
      选中项圆角     = 10,
      边框线宽      = 1,
      下划线色      = "#FF0000",
      光标色       = "#004CFF",
      分隔线色      = "#000000",
      窗口边框色     = "#000000",
      窗背景底色     = "#FFFFFF",
      选中背景色     = "#000000",
      选中字体色     = "#333333",
      编码字体色     = "#000000",
      候选字色      = "#000000"
    };
    #endregion

    #region 全局变量定义
    //SolidColorBrush _bkColor = new((Color)ColorConverter.ConvertFromString("#FFFFFFFF")!);  // 候选框无背景色时的值

    private  readonly  string _appPath        = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private   readonly string _schemeFilePath;  // 配色方案.json"
    private            string _labelName      = "方案名称";

    private int    _selectColorLabelNum;    // 用于记录当前选中的 select_color_label
    private string _bgString;                      // 存放字体色串
    private string _currentConfig, _modifiedConfig; // 存少当前配置和当前修改的配置


    #endregion

    #region 初始化

    public SchemeSetting()
    {
      InitializeComponent();

      restor_Default_Button.Visibility = Visibility.Collapsed;
      loading_Templates_Button.Visibility = Visibility.Collapsed;
      set_As_Default_Button.Visibility = Visibility.Collapsed;
      apply_Button.Visibility = Visibility.Collapsed;
      apply_Save_Button.Visibility = Visibility.Collapsed;
      apply_All_Button.Visibility = Visibility.Collapsed;
      comboBox.Visibility = Visibility.Collapsed;

      _schemeFilePath = $"{_appPath}\\configs\\配色方案.json";

      if (!Directory.Exists($"{_appPath}\\configs")) Directory.CreateDirectory($"{_appPath}\\configs");

      Loaded += MainWindow_Loaded;
      colorPicker.ColorChanged += ColorPicker_ColorChanged;
    }

    private void ColorPicker_ColorChanged(object sender, ColorChangedEventArgs e)
    {
      if (colorPicker.RgbColor == null) return;
      rgbTextBox.RGBText = colorPicker.RgbText;
      SetColorLableColor(colorPicker.RgbColor);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      DataContext = this;
      LoadJson();           // 读取 配色方案.jsonString
      LoadHxFile();         // 读取 候选序号.txt
    }




    // 获取版本号
    public string GetAssemblyVersion()
    {
      var assembly = Assembly.GetExecutingAssembly();
      var version = assembly.GetName().Version;
      return version.ToString().Substring(0, 3);
    }
    #endregion

    #region 顶部控件事件
    // 载入码表方案名称
    private void GetList_button_Click(object sender, RoutedEventArgs e)
    {
      LoadTableNames();

      restor_Default_Button.Visibility    = Visibility.Visible;
      loading_Templates_Button.Visibility = Visibility.Visible;
      set_As_Default_Button.Visibility    = Visibility.Visible;
      apply_Button.Visibility             = Visibility.Visible;
      apply_Save_Button.Visibility        = Visibility.Visible;
      apply_All_Button.Visibility         = Visibility.Visible;
      comboBox.Visibility                 = Visibility.Visible;
      stackPanel4.IsEnabled               = true;
    }

    private void 事件_导入选中方案配置_按钮_Click(object sender, RoutedEventArgs e)
    { 
      if(comboBox.SelectedIndex == -1) return;
      var tempLabelName = comboBox4.SelectedValue as string;
      var tempConfig = GetConfig(tempLabelName);
      _modifiedConfig = Regex.Replace(tempConfig, $"方案：<{tempLabelName}>", $"方案：<.*?>");
      SetControlsValue(_modifiedConfig);
    }
    
    private void LoadTableNames()
    {
      try
      {
        //把所有方案名吐到剪切板,一行一个方案名
        Base.SendMessageTimeout(Base.KwmGetallname);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
      var multiLineString = Clipboard.GetText();

      // 使用StringSplitOptions.RemoveEmptyEntries选项来避免空行被添加
      var lines = multiLineString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

      // 将每行作为一个项添加到ComboBox中
      comboBox.Items.Clear();
      comboBox4.Items.Clear();
      fcfaComboBox1.Items.Clear();
      fcfaComboBox2.Items.Clear();
      lsmbComboBox  .Items.Clear();
      ydmb1ComboBox0.Items.Clear();
      ydmb2ComboBox0.Items.Clear();
      ydmb0ComboBox1.Items.Clear();
      ydmb2ComboBox1.Items.Clear();
      ydmb0ComboBox2.Items.Clear();
      ydmb1ComboBox2.Items.Clear();

      fcfaComboBox1.Items. Add("");
      fcfaComboBox2.Items. Add("");
      lsmbComboBox  .Items.Add("");
      ydmb1ComboBox0.Items.Add("");
      ydmb2ComboBox0.Items.Add("");
      ydmb0ComboBox1.Items.Add("");
      ydmb2ComboBox1.Items.Add("");
      ydmb0ComboBox2.Items.Add("");
      ydmb1ComboBox2.Items.Add("");
      foreach (var line in lines)
      {
        comboBox.Items.Add(line);
        comboBox4.Items.Add(line);
        fcfaComboBox1.Items.Add(line);
        fcfaComboBox2.Items.Add(line);
        lsmbComboBox  .Items.Add(line);
        ydmb1ComboBox0.Items.Add(line);
        ydmb2ComboBox0.Items.Add(line);
        ydmb0ComboBox1.Items.Add(line);
        ydmb2ComboBox1.Items.Add(line);
        ydmb0ComboBox2.Items.Add(line);
        ydmb1ComboBox2.Items.Add(line);
      }
      // comboBox.SelectedIndex = 0;
    }
    private static string GetConfig(string labelName)
    {
      string result;
      try
      {
        Clipboard.SetText(labelName);
        Base.SendMessageTimeout(Base.KwmGetset);
        result = Clipboard.GetText();
      }
      catch (Exception)
      {
        Application.DoEvents();
        result = Clipboard.GetText();
        // MessageBox.Show("操作失败，请重试！");
        // Console.WriteLine(ex.Message);
      }
      return result;
    }

    // 保存内存配置到数据库
    private static void SaveConfig(string labelName)
    {
      try
      {
        Clipboard.SetText(labelName);
        Base.SendMessageTimeout(Base.KwmSavebase);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }


    private void ComboBox_MouseEnter(object sender, MouseEventArgs e)
    {
      comboBox.Focus();
    }

    // 切换方案
    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      _labelName = comboBox.SelectedValue as string;
      _currentConfig = GetConfig(_labelName);
      SetControlsValue(_currentConfig);
    }

    // 加载默认设置
    private void Restor_default_button_Click(object sender, RoutedEventArgs e)
    {
      _currentConfig = GetConfig(_labelName);
      SetControlsValue(_currentConfig);
    }

    // 加载默认模板
    private void Loading_templates_button_Click(object sender, RoutedEventArgs e)
    {
      var result = MessageBox.Show(
      "您确定要从服务端加载默认模板吗？",
      "加载默认模板",
      MessageBoxButton.OKCancel,
      MessageBoxImage.Question);

      if (result != MessageBoxResult.OK)
        return;

      try
      {
        Base.SendMessageTimeout(Base.KwmGetdef);
        var str = Clipboard.GetText();
        _currentConfig = Regex.Replace(str, "方案：<>配置", $"方案：<{_labelName}>配置");
        SetControlsValue(_currentConfig);

        _modifiedConfig = _currentConfig;
        checkBox_Copy25.IsChecked = true;
        checkBox_Copy26.IsChecked = true;
        checkBox1.IsChecked = false;
        checkBox_Copy12.IsChecked = true;
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 设置默认方案
    private void Set_as_default_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        Clipboard.SetText($"《所有进程默认初始方案={_labelName}》");
        Base.SendMessageTimeout(Base.KwmSet2All);
        // Thread.Sleep(200);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 应用修改
    private void Apply_button_Click(object sender, RoutedEventArgs e)
    {
      _modifiedConfig = _currentConfig;
      GetControlsValue(); // 读取所有控件值替换到 modifiedConfig
      // 获取已修改项
      var updataStr = $"方案：<{_labelName}> 配置 \n" + GetDifferences(_modifiedConfig, _currentConfig);

      try
      {

        Clipboard.SetText(updataStr);
        Base.SendMessageTimeout(Base.KwmReset);
        _currentConfig = _modifiedConfig;
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }

    // 保存内存数据
    private void Apply_save_button_Click(object sender, RoutedEventArgs e)
    {
      SaveConfig(_labelName);
    }

    // 更新内存数据
    private void Apply_All_button_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        // 更新内存数据库
        Base.SendMessageTimeout(Base.KwmUpbase);
      }
      catch (Exception ex)
      {
        MessageBox.Show("操作失败，请重试！");
        Console.WriteLine(ex.Message);
      }
    }


    // 获取已修改项
    private static string GetDifferences(string modifiedConfig, string currentConfig)
    {
      var pattern = "《.*?》";
      var matches1 = Regex.Matches(modifiedConfig, pattern);
      var matches2 = Regex.Matches(currentConfig, pattern);
      var modifiedLines = matches1.Cast<Match>().Select(m => m.Value).ToArray();
      var currentLines = matches2.Cast<Match>().Select(m => m.Value).ToArray();
      // 找出不同的行
      var differentLines = modifiedLines.Except(currentLines);
      // 将不同的行追加到新的字符串中
      var newConfig = string.Join(Environment.NewLine, differentLines);
      return newConfig;
    }

    #endregion

    #region 读取配置各项值到控件

    // 读取候选序号
    private void LoadHxFile()
    {
      var file = $"{_appPath}\\configs\\候选序号.txt";
      const string numStr = @"<1=🥑¹sp><2=🍑²sp><3=🍋³sp><4=🍍⁴sp><5=🍈⁵sp><6=🍐⁶sp><7=🍊⁷sp><8=🍑⁸sp><9=🍉⁹sp><10=🍊¹⁰sp>
<1=¹sp><2=²sp><3=³sp><4=⁴sp><5=⁵sp><6=⁶sp><7=⁷sp ><8=⁸sp ><9=⁹sp><10=¹⁰sp>
<1=①sp><2=②sp><3=③sp><4=④sp><5=⑤sp><6=⑥sp><7=⑦sp><8=⑧sp><9=⑨sp><10=⑩sp>
<1=❶sp><2=❷sp><3=❸sp><4=❹sp><5=❺sp><6=❻sp><7=❼sp><8=❽sp><9=❾sp><10=❿sp>
<1=⓵sp><2=⓶sp><3=⓷sp><4=⓸sp><5=⓹sp><6=⓺sp><7=⓻sp><8=⓼sp><9=⓽sp><10=⓾sp>
<1=㊀sp><2=㊁sp><3=㊂sp><4=㊃sp><5=㊄sp><6=㊅sp><7=㊆sp><8=㊇sp><9=㊈sp><10=㊉sp>
<1=㈠sp><2=㈡sp><3=㈢sp><4=㈣sp><5=㈤sp><6=㈥sp><7=㈦sp><8=㈧sp><9=㈨sp><10=㈩sp>
<1=🀇sp><2=🀈sp><3=🀉sp><4=🀊sp><5=🀋sp><6=🀌sp><7=🀍sp><8=🀎sp><9=🀏sp><10=🀄sp>
<1=Ⅰsp><2=Ⅱsp><3=Ⅲsp><4=Ⅳsp><5=Ⅴsp><6=Ⅵsp><7=Ⅶsp><8=Ⅷsp><9=Ⅸsp><10=Ⅹsp>
<1=Ⓐsp><2=Ⓑsp><3=Ⓒsp><4=Ⓓsp><5=Ⓔsp><6=Ⓕsp><7=Ⓖsp><8=Ⓗsp><9=Ⓘsp><10=Ⓙsp>
<1=ⓐsp><2=ⓑsp><3=ⓒsp><4=ⓓsp><5=ⓔsp><6=ⓕsp><7=ⓖsp><8=ⓗsp><9=ⓘsp><10=ⓙsp>";
      if (!File.Exists(file))
        File.WriteAllText(file, numStr);
      using StreamReader sr = new(file);
      while (sr.ReadLine() is { } line)
      {
        ComboBoxItem item = new() { Content = line };
        comboBox3.Items.Add(item);
      }
    }

    // 读取配置值到控件
    private void SetControlsValue(string config)
    {
      var pattern = "《(.*=?.*)=(.*)》";
      var matches = Regex.Matches(config, pattern);
      foreach (Match match in matches)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value)
        {
          case "上屏词条精准匹配key=1*的值进行词语联想吗？": checkBox_Copy8.IsChecked = IsTrueOrFalse(value); break;
          case "精准匹配key=1*的值时要词语模糊联想吗？": checkBox_Copy9.IsChecked = IsTrueOrFalse(value); break;
        }
      }

      pattern = "《(.*?)=(.*)》";
      matches = Regex.Matches(config, pattern);
      foreach (Match match in matches)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value)
        {
          case "背景底色":                     背景底色(value); break;
          case "顶功规则":                     顶功规则(value); break;
          case "D2D字体样式":                  D2D字体样式(value); break;
          case "GDI+字体样式":                 Gdip字体样式(value); break;
          case "候选字体色串":                   SetLabelColor(value); break;
          case "词语联想检索范围":                 词语联想检索范围(value); break;
          case "候选窗口绘制模式":                 候选窗口绘制模式(value); break;
          case "编码或候选嵌入模式":                编码或候选嵌入模式(value); break;
          case "词语联想上屏字符串长度":              词语联想上屏字符串长度(value); break;
          case "候选窗口候选排列方向模式":             候选窗口候选排列方向模式(value); break;
          case "大键盘码元":                    textBox_Copy677.Text = value; break;
          case "小键盘码元":                    textBox_Copy5.Text   = value; break;
          case "键首字根":                     textBox125.Text      = value; break;
          case "字体名称":                     textBox_Copy145.Text = value; break;
          case "码表标签":                     textBox_Copy15.Text  = value; break;
          case "主码表标识":                    textBox_Copy22.Text  = value; break;
          case "副码表标识":                    textBox_Copy23.Text  = value; break;
          case "候选序号":                     设置候选序号(value); break;
          case "顶功小集码元":                   textBox_Copy675.Text = value; break;
          case "码表临时快键":                   textBox_Copy19.Text  = value; break;
          case "D2D回退字体集":                 textBox_Copy10.Text  = value; break;
          case "码表引导快键0":                  textBox_Copy.Text    = value; break;
          case "码表引导快键1":                  textBox_Copy12.Text  = value; break;
          case "码表引导快键2":                  textBox_Copy16.Text  = value; break;
          case "候选快键字符串":                  textBox_Copy66.Text  = value; break;
          case "大小键盘万能码元":                 textBox_Copy6.Text   = value; break;
          case "大键盘中文标点串":                 textBox_Copy68.Text  = value; break;
          case "重复上屏码元字符串":                textBox_Copy1.Text   = value; break;
          case "反查方案名称1":                  设置组合框当前值(fcfaComboBox1, value); break;
          case "反查方案名称2":                  设置组合框当前值(fcfaComboBox2, value); break;
          case "码表临时快键编码名":                设置引导码表设置("码表临时快键编码名"  , value); break;
          case "码表引导快键0编码名0":              设置引导码表设置("码表引导快键0编码名0", value); break;
          case "码表引导快键0编码名1":              设置引导码表设置("码表引导快键0编码名1", value); break;
          case "码表引导快键1编码名0":              设置引导码表设置("码表引导快键1编码名0", value); break;
          case "码表引导快键1编码名1":              设置引导码表设置("码表引导快键1编码名1", value); break;
          case "码表引导快键2编码名0":              设置引导码表设置("码表引导快键2编码名0", value); break;
          case "码表引导快键2编码名1":              设置引导码表设置("码表引导快键2编码名1", value); break;
          case "非编码串首位的大键盘码元":             textBox_Copy7.Text           = value; break;
          case "非编码串首位的小键盘码元":             textBox_Copy8.Text           = value; break;
          case "连续的大键盘码元": textBox_8.Text = value; break;
          case "连续的小键盘码元": textBox_9.Text = value; break;
          case "大键盘按下Shift的中文标点串":         textBox_Copy69.Text          = value; break;
          case "往上翻页大键盘英文符号编码串":           textBox_Copy21.Text          = value; break;
          case "往下翻页大键盘英文符号编码串":           textBox_Copy2.Text           = value; break;
          case "往上翻页小键盘英文符号编码串":           textBox_Copy3.Text           = value; break;
          case "往下翻页小键盘英文符号编码串":           textBox_Copy4.Text           = value; break;
          case "码表标签显示模式":                 comboBox1_Copy.SelectedIndex = int.Parse(value); break;
          case "窗口四个角的圆角半径":               nud11.Value                  = int.Parse(value); break;
          case "选中项四个角的圆角半径":              nud12.Value                  = int.Parse(value); break;
          case "候选窗口边框线宽度":                nud13.Value                  = int.Parse(value); break;
          case "最大码长":                     nud1.Value                   = int.Parse(value); break;
          case "D2D字体加粗权值":                nud14.Value                  = int.Parse(value); break;
          case "候选个数":                     nud15.Value                  = int.Parse(value); break;
          case "1-26候选的横向偏离":              nud16.Value                  = int.Parse(value); break;
          case "候选的高度间距":                  nud17.Value                  = int.Parse(value); break;
          case "候选的宽度间距":                  nud18.Value                  = int.Parse(value); break;
          case "调频权重最小码长":                 nud2.Value                   = int.Parse(value); break;
          case "双检索历史重数":                  nud3.Value                   = int.Parse(value); break;
          case "唯一上屏最小码长":                 nud4.Value                   = int.Parse(value); break;
          case "GDI字体加粗权值":                nud14_Copy.Value             = int.Parse(value); break;
          case "光标色":                      color_Label_2.Background     = RgbStringToColor(value); break;
          case "分隔线色":                     color_Label_3.Background     = RgbStringToColor(value); break;
          case "候选选中色":                    color_Label_6.Background     = RgbStringToColor(value); break;
          case "要码长顶屏吗？":                  checkBox1_Copy111.IsChecked  = IsTrueOrFalse(value); break;
          case "要数字顶屏吗？":                  checkBox1_Copy7.IsChecked    = IsTrueOrFalse(value); break;
          case "要标点顶屏吗？":                  checkBox1_Copy6.IsChecked    = IsTrueOrFalse(value); break;
          case "要唯一上屏吗？":                  checkBox1_Copy5.IsChecked    = IsTrueOrFalse(value); break;
          case "词条后缀_顶屏吗？":               checkBox_5.IsChecked         = IsTrueOrFalse(value); break;
          case "嵌入下划线色":                   color_Label_1.Background     = RgbStringToColor(value); break;
          case "候选窗口边框色":                  color_Label_4.Background     = RgbStringToColor(value); break;
          case "候选选中字体色":                  color_Label_7.Background     = RgbStringToColor(value); break;
          case "要显示背景图吗？":                 checkBox_Copy42.IsChecked    = IsTrueOrFalse(value); break;
          case "要启用双检索吗？":                 checkBox1_Copy3.IsChecked    = IsTrueOrFalse(value); break;
          case "关联中文标点吗？":                 checkBox_Copy31.IsChecked    = IsTrueOrFalse(value); break;
          case "无候选要清屏吗？":                 checkBox_Copy20.IsChecked    = IsTrueOrFalse(value); break;
          case "要使用嵌入模式吗？":                checkBox_Copy44.IsChecked    = IsTrueOrFalse(value); break;
          case "要开启词语联想吗？":                checkBox_Copy4.IsChecked     = IsTrueOrFalse(value); break;
          case "是键首字根码表吗？":                checkBox1_Copy55.IsChecked   = IsTrueOrFalse(value); break;
          case "要显示键首字根吗？":                checkBox_Copy34.IsChecked    = IsTrueOrFalse(value); break;
          case "超过码长要清屏吗？":                checkBox_Copy19.IsChecked    = IsTrueOrFalse(value); break;
          case "要逐码提示检索吗？":                checkBox_Copy.IsChecked      = IsTrueOrFalse(value); break;
          case "要显示逐码提示吗？":                checkBox.IsChecked           = IsTrueOrFalse(value); break;
          case "要显示反查提示吗？":                checkBox1.IsChecked          = IsTrueOrFalse(value); break;
          case "要启用单字模式吗？":                checkBox1_Copy.IsChecked     = IsTrueOrFalse(value); break;
          case "GDI字体要倾斜吗？":               checkBox_Copy314.IsChecked   = IsTrueOrFalse(value); break;
          case "要启用右Ctrl键吗？":              checkBox_Copy16.IsChecked    = IsTrueOrFalse(value); break;
          case "要启用左Ctrl键吗？":              checkBox_Copy15.IsChecked    = IsTrueOrFalse(value); break;
          case "要启用左Shift键吗？":             checkBox_Copy13.IsChecked    = IsTrueOrFalse(value); break;
          case "要启用右Shift键吗？":             checkBox_Copy14.IsChecked    = IsTrueOrFalse(value); break;
          case "GDI+字体要下划线吗？":             checkBox19.IsChecked         = IsTrueOrFalse(value); break;
          case "GDI+字体要删除线吗？":             checkBox20.IsChecked         = IsTrueOrFalse(value); break;
          case "窗口四个角要圆角吗？":               hxc_CheckBox.IsChecked       = IsTrueOrFalse(value); break;
          case "码表标签要左对齐吗？":               checkBox_Copy39.IsChecked    = IsTrueOrFalse(value); break;
          case "过渡态按1要上屏1吗？":              checkBox_Copy30.IsChecked    = IsTrueOrFalse(value); break;
          case "Shift键上屏编码串吗？":            checkBox_Copy23.IsChecked    = IsTrueOrFalse(value); break;
          case "Enter键上屏编码串吗？":            checkBox_Copy26.IsChecked    = IsTrueOrFalse(value); break;
          case "要启用Ctrl+Space键吗？":         checkBox_Copy17.IsChecked    = IsTrueOrFalse(value); break;
          case "要开启Ctrl键清联想吗？":            checkBox_Copy10.IsChecked    = IsTrueOrFalse(value); break;
          case "选中项四个角要圆角吗？":              hxcbj_CheckBox.IsChecked     = IsTrueOrFalse(value); break;
          case "要启用ESC键自动造词吗？":            checkBox_Copy3.IsChecked     = IsTrueOrFalse(value); break;
          case "词语联想只是匹配首位吗？":             checkBox_Copy6.IsChecked     = IsTrueOrFalse(value); break;
          case "高度宽度要完全自动调整吗？":            checkBox_Copy40.IsChecked    = IsTrueOrFalse(value); break;
          case "中英切换要显示提示窗口吗？":            checkBox_Copy11.IsChecked    = IsTrueOrFalse(value); break;
          case "双检索时编码要完全匹配吗？":            checkBox1_Copy4.IsChecked    = IsTrueOrFalse(value); break;
          case "词语联想要显示词语全部吗？":            checkBox_Copy5.IsChecked     = IsTrueOrFalse(value); break;
          case "上屏后候选窗口要立即消失吗？":           checkBox_Copy18.IsChecked    = IsTrueOrFalse(value); break;
          case "要启用最大码长无候选清屏吗？":           checkBox_Copy21.IsChecked    = IsTrueOrFalse(value); break;
          case "无候选敲空格要上屏编码串吗？":           checkBox_Copy22.IsChecked    = IsTrueOrFalse(value); break;
          case "词语联想时标点顶屏要起作用吗？":          checkBox_Copy7.IsChecked     = IsTrueOrFalse(value); break;
          case "候选词条要按码长短优先排序吗？":          checkBox_Copy2.IsChecked     = IsTrueOrFalse(value); break;
          case "要启用上屏自动增加调频权重吗？":          checkBox1_Copy1.IsChecked    = IsTrueOrFalse(value); break;
          case "Space键要上屏临时英文编码串吗？":       checkBox_Copy25.IsChecked    = IsTrueOrFalse(value); break;
          case "Enter键上屏并使首个字母大写吗？":       checkBox_Copy27.IsChecked    = IsTrueOrFalse(value); break;
          case "候选词条要按调频权重检索排序吗？":         checkBox1_Copy2.IsChecked    = IsTrueOrFalse(value); break;
          case "竖向候选窗口选中背景色要等宽吗？":         checkBox_Copy41.IsChecked    = IsTrueOrFalse(value); break;
          case "候选窗口候选从上到下排列要锁定吗？":        checkBox_Copy45.IsChecked    = IsTrueOrFalse(value); break;
          case "无临时快键时,也要显示主码表标识吗？":       checkBox_Copy32.IsChecked    = IsTrueOrFalse(value); break;
          case "从中文切换到英文时,要上屏编码串吗？":       checkBox_Copy12.IsChecked    = IsTrueOrFalse(value); break;
          case "Shift键+字母键要进入临时英文长句态吗？":   checkBox_Copy24.IsChecked    = IsTrueOrFalse(value); break;
          case "要启用上屏自动增加调频权重直接到顶吗？":      checkBox_Copy1.IsChecked     = IsTrueOrFalse(value); break;
          case "Backspace键一次性删除前次上屏的内容吗？": checkBox_Copy28.IsChecked    = IsTrueOrFalse(value); break;
          case "标点或数字顶屏时,若是引导键,要继续引导吗？":   checkBox1_Copy8.IsChecked    = IsTrueOrFalse(value); break;
          case "前次上屏的是数字再上屏句号*要转成点号*吗？":   checkBox_Copy29.IsChecked    = IsTrueOrFalse(value); break;
          case "候选窗口候选排列方向模式>1时要隐藏编码串行吗？"
                                                             :
            checkBox_Copy38.IsChecked = IsTrueOrFalse(value); break;
          case "候选窗口候选从上到下排列锁定的情况下要使编码区离光标最近吗？"
                                                             :
            checkBox_Copy46.IsChecked = IsTrueOrFalse(value); break;
        }
      }
    }

    private void 背景底色(string value)
    {
      if (value == "")
      {
        hxcds_CheckBox.IsChecked = true;
        color_Label_5.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        //_bkColor = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
      }
      else
      {
        hxcds_CheckBox.IsChecked = false;
        color_Label_5.Background = RgbStringToColor(value);
        //_bkColor = RgbStringToColor(value);
      }
    }

    private string 获取引导码表设置(System.Windows.Controls.ComboBox cb)
    {
      return cb.Name switch
      {
        "lsmbComboBox"   => $"{cb.SelectedValue}<1={lssx.Text}><2={lscx.Text}>",
        "ydmb1ComboBox0" => $"{cb.SelectedValue}<1={ydsx00.Text}><2={ydcx00.Text}>",
        "ydmb2ComboBox0" => $"{cb.SelectedValue}<1={ydsx01.Text}><2={ydcx01.Text}>",
        "ydmb0ComboBox1" => $"{cb.SelectedValue}<1={ydsx10.Text}><2={ydcx10.Text}>",
        "ydmb2ComboBox1" => $"{cb.SelectedValue}<1={ydsx11.Text}><2={ydcx11.Text}>",
        "ydmb0ComboBox2" => $"{cb.SelectedValue}<1={ydsx20.Text}><2={ydcx20.Text}>",
        "ydmb1ComboBox2" => $"{cb.SelectedValue}<1={ydsx21.Text}><2={ydcx21.Text}>",
        _                => null
      };
    }

    private void 设置引导码表设置(string cbName, string value)
    {
      const string pattern = "(.*)<1=(.*?)><2=(.*?)>";
      var matches = Regex.Matches(value, pattern);
      if (matches.Count <= 0) return;

      switch (cbName)
      {
        case "码表临时快键编码名":
          设置组合框当前值(lsmbComboBox, matches[0].Groups[1].Value);
          lssx.Text   = matches[0].Groups[2].Value;
          lscx.Text   = matches[0].Groups[3].Value;
          break;
        case "码表引导快键0编码名0":
          设置组合框当前值(ydmb1ComboBox0, matches[0].Groups[1].Value);
          ydsx00.Text = matches[0].Groups[2].Value;
          ydcx00.Text = matches[0].Groups[3].Value;
          break;
        case "码表引导快键0编码名1":
          设置组合框当前值(ydmb2ComboBox0, matches[0].Groups[1].Value);
          ydsx01.Text = matches[0].Groups[2].Value;
          ydcx01.Text = matches[0].Groups[3].Value;
          break;
        case "码表引导快键1编码名0":
          设置组合框当前值(ydmb0ComboBox1, matches[0].Groups[1].Value);
          ydsx10.Text = matches[0].Groups[2].Value;
          ydcx10.Text = matches[0].Groups[3].Value;
          break;
        case "码表引导快键1编码名1":
          设置组合框当前值(ydmb2ComboBox1, matches[0].Groups[1].Value);
          ydsx11.Text = matches[0].Groups[2].Value;
          ydcx11.Text = matches[0].Groups[3].Value;
          break;
        case "码表引导快键2编码名0":
          设置组合框当前值(ydmb0ComboBox2, matches[0].Groups[1].Value);
          ydsx20.Text = matches[0].Groups[2].Value;
          ydcx20.Text = matches[0].Groups[3].Value;
          break;
        case "码表引导快键2编码名1":
          设置组合框当前值(ydmb1ComboBox2, matches[0].Groups[1].Value);
          ydsx21.Text = matches[0].Groups[2].Value;
          ydcx21.Text = matches[0].Groups[3].Value;
          break;
      }
    }

    private static void 设置组合框当前值(Selector cb, string cbName)
    {
      for (var i = 0; i < cb.Items.Count; i++)
      {
        if (cb.Items[i].ToString() != cbName) continue;
        cb.SelectedIndex = i;
        return;
      }
      cb.SelectedIndex = -1;
    }


    private string 获取候选序号()
    {
      var str = $"<1={hxxh1.Text}><2={hxxh2.Text}><3={hxxh3.Text}><4={hxxh4.Text}><5={hxxh5.Text}>";
      str    += $"<6={hxxh6.Text}><7={hxxh7.Text}><8={hxxh8.Text}><9={hxxh9.Text}><10={hxxh0.Text}>";
      return str;
    }
    private void 设置候选序号(string value)
    {
      const string pattern = "<1=(.*?)><2=(.*?)><3=(.*?)><4=(.*?)><5=(.*?)><6=(.*?)><7=(.*?)><8=(.*?)><9=(.*?)><10=(.*?)>";
      var matches = Regex.Matches(value, pattern);
      if (matches.Count <= 0) return;
      hxxh1.Text = matches[0].Groups[1].Value;
      hxxh2.Text = matches[0].Groups[2].Value;
      hxxh3.Text = matches[0].Groups[3].Value;
      hxxh4.Text = matches[0].Groups[4].Value;
      hxxh5.Text = matches[0].Groups[5].Value;
      hxxh6.Text = matches[0].Groups[6].Value;
      hxxh7.Text = matches[0].Groups[7].Value;
      hxxh8.Text = matches[0].Groups[8].Value;
      hxxh9.Text = matches[0].Groups[9].Value;
      hxxh0.Text = matches[0].Groups[10].Value;
    }
    private static bool IsTrueOrFalse(string value)
    {
      return value != "不要" && value != "不是";
    }

    private void 顶功规则(string value)
    {
      switch (value)
      {
        case "1":
          radioButton454.IsChecked = true; break;
        case "2":
          radioButton455.IsChecked = true; break;
        case "3":
          radioButton456.IsChecked = true; break;
      }
    }

    private void 候选窗口绘制模式(string value)
    {
      switch (value)
      {
        case "2":
          radioButton9.IsChecked = true; break;
        case "0":
          radioButton10.IsChecked = true; break;
        case "1":
          radioButton11.IsChecked = true; break;
      }
    }

    private void D2D字体样式(string value)
    {
      switch (value)
      {
        case "0":
          radioButton6.IsChecked = true; break;
        case "1":
          radioButton7.IsChecked = true; break;
      }
    }

    private void Gdip字体样式(string value)
    {
      switch (value)
      {
        case "0":
          radioButton14.IsChecked = true; break;
        case "1":
          radioButton15.IsChecked = true; break;
        case "2":
          radioButton16.IsChecked = true; break;
        case "3":
          radioButton17.IsChecked = true; break;
      }
    }

    private void 候选窗口候选排列方向模式(string value)
    {
      switch (value)
      {
        case "1":
          radioButton8.IsChecked = true; break;
        case "2":
          radioButton12.IsChecked = true; break;
        case "3":
          radioButton13.IsChecked = true; break;
      }
    }

    private void 编码或候选嵌入模式(string value)
    {
      if (value.Length > 1)
        checkBox_Copy33.IsChecked = true;
      comboBox1.SelectedIndex = value switch
      {
        "0" or "10" => 0,
        "1" or "11" => 1,
        "2" or "12" => 2,
        "3" or "13" => 3,
        _ => 4,
      };
    }

    // RGB字符串转换成Color
    private SolidColorBrush RgbStringToColor(string rgbString)
    {
      //候选窗背景色为空时设为对话框背景色
      if (rgbString == "")
      {
        hxcds_CheckBox.IsChecked = true;
        return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
      }
      // 去掉字符串两边的括号并将逗号分隔的字符串转换为整型数组
      var rgbValues = rgbString.Trim('(', ')').Split(',');
      if (rgbValues.Length != 3)
      {
        throw new ArgumentException("Invalid RGB color format.");
      }

      var r = byte.Parse(rgbValues[0]);
      var g = byte.Parse(rgbValues[1]);
      var b = byte.Parse(rgbValues[2]);

      return new SolidColorBrush(Color.FromRgb(r, g, b));
    }

    private void SetLabelColor(string str)
    {
      var pattern = "<(.*?)=(.*?)>";
      var matches2 = Regex.Matches(str, pattern);

      foreach (Match match in matches2)
      {
        var value = match.Groups[2].Value;
        switch (match.Groups[1].Value)
        {
          case "0": //编码字体色
            color_Label_8.Background = RgbStringToColor(value);
            break;
          case "1":
            color_Label_9.Background = RgbStringToColor(value);
            break;
        }
      }
      HXZ_TextBoxText();
    }

    private void 词语联想上屏字符串长度(string value)
    {
      switch (value)
      {
        case "1":
          radioButton.IsChecked = true; break;
        case "2":
          radioButton1.IsChecked = true; break;
        case "3":
          radioButton2.IsChecked = true; break;
      }
    }

    private void 词语联想检索范围(string value)
    {
      switch (value)
      {
        case "1":
          radioButton3.IsChecked = true; break;
        case "2":
          radioButton4.IsChecked = true; break;
        case "3":
          radioButton5.IsChecked = true; break;
      }
    }
    #endregion

    #region 读取控件属性值
    // 正则替换 modifiedConfig
    private void ReplaceConfig(string key, string value)
    {
      try
      {
        _modifiedConfig = Regex.Replace(_modifiedConfig, $"《{key}=.*?》", $"《{key}={value}》");
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        Console.WriteLine($@"《{key}={value}》");
      }
    }
    // 读取控件属性值
    private void GetControlsValue()
    {
      ReplaceConfig("顶功规则",             取顶功规则());
      ReplaceConfig("背景底色",             取背景底色());
      ReplaceConfig("候选字体色串",           _bgString);
      ReplaceConfig("键首字根",             textBox125.Text);
      ReplaceConfig("D2D字体样式",          取d2D字体样式());
      ReplaceConfig("码表标签",             textBox_Copy15.Text);
      ReplaceConfig("候选序号",             获取候选序号());
      ReplaceConfig("小键盘码元",            textBox_Copy5.Text);
      ReplaceConfig("字体名称",             textBox_Copy145.Text);
      ReplaceConfig("最大码长",             nud1.Value.ToString());
      ReplaceConfig("主码表标识",            textBox_Copy22.Text);
      ReplaceConfig("副码表标识",            textBox_Copy23.Text);
      ReplaceConfig("候选个数",             nud15.Value.ToString());
      ReplaceConfig("大键盘码元",            textBox_Copy677.Text);
      ReplaceConfig("反查方案名称1",          fcfaComboBox1.SelectedIndex != -1 ? fcfaComboBox1.SelectedValue.ToString() : "");
      ReplaceConfig("反查方案名称2",          fcfaComboBox2.SelectedIndex != -1 ? fcfaComboBox2.SelectedValue.ToString() : "");
      ReplaceConfig("码表引导快键0",          textBox_Copy.Text);
      ReplaceConfig("码表临时快键",           textBox_Copy19.Text);
      ReplaceConfig("码表引导快键1",          textBox_Copy12.Text);
      ReplaceConfig("D2D回退字体集",         textBox_Copy10.Text);
      ReplaceConfig("顶功小集码元",           textBox_Copy675.Text);
      ReplaceConfig("码表引导快键2",          textBox_Copy16.Text);
      ReplaceConfig("候选快键字符串",          textBox_Copy66.Text);
      ReplaceConfig("大小键盘万能码元",         textBox_Copy6.Text);
      ReplaceConfig("大键盘中文标点串",         textBox_Copy68.Text);
      ReplaceConfig("双检索历史重数",          nud3.Value.ToString());
      ReplaceConfig("候选窗口绘制模式",         取候选窗口绘制模式());
      ReplaceConfig("重复上屏码元字符串",        textBox_Copy1.Text);
      ReplaceConfig("词语联想检索范围",         取词语联想检索范围());
      ReplaceConfig("候选的高度间距",          nud17.Value.ToString());
      ReplaceConfig("候选的宽度间距",          nud18.Value.ToString());
      ReplaceConfig("D2D字体加粗权值",        nud14.Value.ToString());
      ReplaceConfig("调频权重最小码长",         nud2.Value.ToString());
      ReplaceConfig("唯一上屏最小码长",         nud4.Value.ToString());
      ReplaceConfig("码表临时快键编码名",        获取引导码表设置(lsmbComboBox));
      ReplaceConfig("码表引导快键0编码名0",      获取引导码表设置(ydmb1ComboBox0));
      ReplaceConfig("码表引导快键0编码名1",      获取引导码表设置(ydmb2ComboBox0));
      ReplaceConfig("码表引导快键1编码名0",      获取引导码表设置(ydmb0ComboBox1));
      ReplaceConfig("码表引导快键1编码名1",      获取引导码表设置(ydmb2ComboBox1));
      ReplaceConfig("码表引导快键2编码名0",      获取引导码表设置(ydmb0ComboBox2));
      ReplaceConfig("码表引导快键2编码名1",      获取引导码表设置(ydmb1ComboBox2));
      ReplaceConfig("1-26候选的横向偏离",      nud16.Value.ToString());
      ReplaceConfig("编码或候选嵌入模式",        取编码或候选嵌入模式());
      ReplaceConfig("候选窗口边框线宽度",        nud13.Value.ToString());
      ReplaceConfig("GDI字体加粗权值",        nud14_Copy.Value.ToString());
      ReplaceConfig("非编码串首位的大键盘码元",     textBox_Copy7.Text);
      ReplaceConfig("非编码串首位的小键盘码元",     textBox_Copy8.Text);
      ReplaceConfig("连续的大键盘码元", textBox_8.Text);
      ReplaceConfig("连续的小键盘码元", textBox_9.Text);
      ReplaceConfig("窗口四个角的圆角半径",       nud11.Value.ToString());
      ReplaceConfig("选中项四个角的圆角半径",      nud12.Value.ToString());
      ReplaceConfig("大键盘按下Shift的中文标点串", textBox_Copy69.Text);
      ReplaceConfig("往下翻页大键盘英文符号编码串",   textBox_Copy2.Text);
      ReplaceConfig("往上翻页小键盘英文符号编码串",   textBox_Copy3.Text);
      ReplaceConfig("往下翻页小键盘英文符号编码串",   textBox_Copy4.Text);
      ReplaceConfig("往上翻页大键盘英文符号编码串",   textBox_Copy21.Text);
      ReplaceConfig("词语联想上屏字符串长度",      取词语联想上屏字符串长度());
      ReplaceConfig("光标色",              Base.HexToRgb(color_Label_2.Background.ToString()));
      ReplaceConfig("候选窗口候选排列方向模式",     取候选窗口候选排列方向模式());
      ReplaceConfig("要显示逐码提示吗？",        要或不要(checkBox.IsChecked != null && (bool)checkBox.IsChecked));
      ReplaceConfig("分隔线色",             Base.HexToRgb(color_Label_3.Background.ToString()));

      ReplaceConfig("要显示反查提示吗？", 要或不要(checkBox1.IsChecked != null && (bool)checkBox1.IsChecked));
      ReplaceConfig("要数字顶屏吗？", 要或不要(checkBox1_Copy7.IsChecked != null && (bool)checkBox1_Copy7.IsChecked));
      ReplaceConfig("要标点顶屏吗？", 要或不要(checkBox1_Copy6.IsChecked != null && (bool)checkBox1_Copy6.IsChecked));
      ReplaceConfig("要唯一上屏吗？", 要或不要(checkBox1_Copy5.IsChecked != null && (bool)checkBox1_Copy5.IsChecked));
      ReplaceConfig("词条后缀_顶屏吗？", 要或不要(checkBox_5.IsChecked != null && (bool)checkBox_5.IsChecked));
      ReplaceConfig("码表标签显示模式", comboBox1_Copy.SelectedIndex.ToString());
      ReplaceConfig("候选选中色", Base.HexToRgb(color_Label_6.Background.ToString()));
      ReplaceConfig("要逐码提示检索吗？", 要或不要(checkBox_Copy.IsChecked != null && (bool)checkBox_Copy.IsChecked));
      ReplaceConfig("要显示背景图吗？", 要或不要(checkBox_Copy42.IsChecked != null && (bool)checkBox_Copy42.IsChecked));
      ReplaceConfig("无候选要清屏吗？", 要或不要(checkBox_Copy20.IsChecked != null && (bool)checkBox_Copy20.IsChecked));
      ReplaceConfig("要启用双检索吗？", 要或不要(checkBox1_Copy3.IsChecked != null && (bool)checkBox1_Copy3.IsChecked));
      ReplaceConfig("要码长顶屏吗？", 要或不要(checkBox1_Copy111.IsChecked != null && (bool)checkBox1_Copy111.IsChecked));
      ReplaceConfig("嵌入下划线色", Base.HexToRgb(color_Label_1.Background.ToString()));
      ReplaceConfig("关联中文标点吗？", 要或不要(checkBox_Copy31 != null && (bool)checkBox_Copy31.IsChecked));
      ReplaceConfig("要启用单字模式吗？", 要或不要(checkBox1_Copy != null && (bool)checkBox1_Copy.IsChecked));
      ReplaceConfig("窗口四个角要圆角吗？", 要或不要(hxc_CheckBox != null && (bool)hxc_CheckBox.IsChecked));
      ReplaceConfig("要开启词语联想吗？", 要或不要(checkBox_Copy4 != null && (bool)checkBox_Copy4.IsChecked));
      ReplaceConfig("要启用左Ctrl键吗？", 要或不要(checkBox_Copy15 != null && (bool)checkBox_Copy15.IsChecked));
      ReplaceConfig("要启用右Ctrl键吗？", 要或不要(checkBox_Copy16 != null && (bool)checkBox_Copy16.IsChecked));
      ReplaceConfig("要显示键首字根吗？", 要或不要(checkBox_Copy34 != null && (bool)checkBox_Copy34.IsChecked));
      ReplaceConfig("候选窗口边框色", Base.HexToRgb(color_Label_4.Background.ToString()));
      ReplaceConfig("候选选中字体色", Base.HexToRgb(color_Label_7.Background.ToString()));
      ReplaceConfig("超过码长要清屏吗？", 要或不要(checkBox_Copy19 != null && (bool)checkBox_Copy19.IsChecked));
      ReplaceConfig("GDI字体要倾斜吗？", 要或不要(checkBox_Copy314 != null && (bool)checkBox_Copy314.IsChecked));
      ReplaceConfig("要使用嵌入模式吗？", 要或不要(checkBox_Copy44 != null && (bool)checkBox_Copy44.IsChecked));
      ReplaceConfig("要启用左Shift键吗？", 要或不要(checkBox_Copy13 != null && (bool)checkBox_Copy13.IsChecked));
      ReplaceConfig("要启用右Shift键吗？", 要或不要(checkBox_Copy14 != null && (bool)checkBox_Copy14.IsChecked));
      ReplaceConfig("是键首字根码表吗？", 是或不是(checkBox1_Copy55 != null && (bool)checkBox1_Copy55.IsChecked));
      ReplaceConfig("码表标签要左对齐吗？", 要或不要(checkBox_Copy39 != null && (bool)checkBox_Copy39.IsChecked));
      ReplaceConfig("过渡态按1要上屏1吗？", 要或不要(checkBox_Copy30 != null && (bool)checkBox_Copy30.IsChecked));
      ReplaceConfig("Shift键上屏编码串吗？", 要或不要(checkBox_Copy23 != null && (bool)checkBox_Copy23.IsChecked));
      ReplaceConfig("Enter键上屏编码串吗？", 要或不要(checkBox_Copy26 != null && (bool)checkBox_Copy26.IsChecked));
      ReplaceConfig("选中项四个角要圆角吗？", 要或不要(hxcbj_CheckBox.IsChecked != null && (bool)hxcbj_CheckBox.IsChecked));
      ReplaceConfig("要启用ESC键自动造词吗？", 要或不要(checkBox_Copy3 != null && (bool)checkBox_Copy3.IsChecked));
      ReplaceConfig("要开启Ctrl键清联想吗？", 要或不要(checkBox_Copy10 != null && (bool)checkBox_Copy10.IsChecked));
      ReplaceConfig("词语联想只是匹配首位吗？", 是或不是(checkBox_Copy6 != null && (bool)checkBox_Copy6.IsChecked));
      ReplaceConfig("词语联想要显示词语全部吗？", 要或不要(checkBox_Copy5 != null && (bool)checkBox_Copy5.IsChecked));
      ReplaceConfig("中英切换要显示提示窗口吗？", 要或不要(checkBox_Copy11 != null && (bool)checkBox_Copy11.IsChecked));
      ReplaceConfig("高度宽度要完全自动调整吗？", 要或不要(checkBox_Copy40 != null && (bool)checkBox_Copy40.IsChecked));
      ReplaceConfig("双检索时编码要完全匹配吗？", 要或不要(checkBox1_Copy4 != null && (bool)checkBox1_Copy4.IsChecked));
      ReplaceConfig("要启用最大码长无候选清屏吗？", 要或不要(checkBox_Copy21 != null && (bool)checkBox_Copy21.IsChecked));
      ReplaceConfig("上屏后候选窗口要立即消失吗？", 要或不要(checkBox_Copy18 != null && (bool)checkBox_Copy18.IsChecked));
      ReplaceConfig("无候选敲空格要上屏编码串吗？", 要或不要(checkBox_Copy22 != null && (bool)checkBox_Copy22.IsChecked));
      ReplaceConfig("候选词条要按码长短优先排序吗？", 要或不要(checkBox_Copy2.IsChecked != null && (bool)checkBox_Copy2.IsChecked));
      ReplaceConfig("词语联想时标点顶屏要起作用吗？", 要或不要(checkBox_Copy7.IsChecked != null && (bool)checkBox_Copy7.IsChecked));
      ReplaceConfig("要启用上屏自动增加调频权重吗？", 要或不要(checkBox1_Copy1.IsChecked != null && (bool)checkBox1_Copy1.IsChecked));
      ReplaceConfig("Space键要上屏临时英文编码串吗？", 要或不要(checkBox_Copy25.IsChecked != null && (bool)checkBox_Copy25.IsChecked));
      ReplaceConfig("Enter键上屏并使首个字母大写吗？", 要或不要(checkBox_Copy27.IsChecked != null && (bool)checkBox_Copy27.IsChecked));
      ReplaceConfig("竖向候选窗口选中背景色要等宽吗？", 要或不要(checkBox_Copy41.IsChecked != null && (bool)checkBox_Copy41.IsChecked));
      ReplaceConfig("候选词条要按调频权重检索排序吗？", 要或不要(checkBox1_Copy2.IsChecked != null && (bool)checkBox1_Copy2.IsChecked));
      ReplaceConfig("候选窗口候选从上到下排列要锁定吗？", 要或不要(checkBox_Copy45.IsChecked != null && (bool)checkBox_Copy45.IsChecked));
      ReplaceConfig("从中文切换到英文时,要上屏编码串吗？", 要或不要(checkBox_Copy12.IsChecked != null && (bool)checkBox_Copy12.IsChecked));
      ReplaceConfig("要启用上屏自动增加调频权重直接到顶吗？", 要或不要(checkBox_Copy1.IsChecked != null && (bool)checkBox_Copy1.IsChecked));
      ReplaceConfig("Backspace键一次性删除前次上屏的内容吗？", 要或不要(checkBox_Copy28.IsChecked != null && (bool)checkBox_Copy28.IsChecked));
      ReplaceConfig("无临时快键时,也要显示主码表标识吗？", 要或不要(checkBox_Copy32.IsChecked != null && (bool)checkBox_Copy32.IsChecked));
      ReplaceConfig("标点或数字顶屏时,若是引导键,要继续引导吗？", 要或不要(checkBox1_Copy8.IsChecked != null && (bool)checkBox1_Copy8.IsChecked));
      ReplaceConfig("候选窗口候选排列方向模式>1时要隐藏编码串行吗？", 要或不要(checkBox_Copy38.IsChecked != null && (bool)checkBox_Copy38.IsChecked));
      ReplaceConfig("候选窗口候选从上到下排列锁定的情况下要使编码区离光标最近吗？", 要或不要(checkBox_Copy46.IsChecked != null && (bool)checkBox_Copy46.IsChecked));

      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《要启用Ctrl\+Space键吗？=.*?》", $"《要启用Ctrl+Space键吗？={要或不要(checkBox_Copy17.IsChecked != null && (bool)checkBox_Copy17.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《GDI\+字体样式=.*?》", $"《GDI+字体样式={取gdIp字体样式()}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"GDI\+字体要下划线吗？=.*?》", $"GDI+字体要下划线吗？={要或不要(checkBox19.IsChecked != null && (bool)checkBox19.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"GDI\+字体要删除线吗？=.*?》", $"GDI+字体要删除线吗？={要或不要(checkBox20.IsChecked != null && (bool)checkBox20.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《上屏词条精准匹配key=1\*的值进行词语联想吗？=.*?》", $"《上屏词条精准匹配key=1*的值进行词语联想吗？={要或不要(checkBox_Copy8.IsChecked != null && (bool)checkBox_Copy8.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《精准匹配key=1\*的值时要词语模糊联想吗？=.*?》", $"《精准匹配key=1*的值时要词语模糊联想吗？={要或不要(checkBox_Copy9.IsChecked != null && (bool)checkBox_Copy9.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《Shift键\+字母键要进入临时英文长句态吗？=.*?》", $"《Shift键+字母键要进入临时英文长句态吗？={要或不要(checkBox_Copy24.IsChecked != null && (bool)checkBox_Copy24.IsChecked)}》");
      _modifiedConfig = Regex.Replace(_modifiedConfig, @"《前次上屏的是数字再上屏句号\*要转成点号\*吗？=.*?》", $"《前次上屏的是数字再上屏句号*要转成点号*吗？={要或不要(checkBox_Copy29.IsChecked != null && (bool)checkBox_Copy29.IsChecked)}》");
    }
    private string 取编码或候选嵌入模式()
    {
      var selected = comboBox1.SelectedIndex.ToString();
      if (checkBox_Copy33.IsChecked == true)
        selected = "1" + selected;
      return selected;
    }
    private string 取背景底色()
    {
      return hxcds_CheckBox.IsChecked == true ? "" : Base.HexToRgb(color_Label_5.Background.ToString());
    }
    private string 取候选窗口绘制模式()
    {
      if (radioButton10.IsChecked == true) return "0";
      return radioButton11.IsChecked == true ? "1" : "2";
    }
    private string 取d2D字体样式()
    {
      return radioButton7.IsChecked == true ? "1" : "0";
    }
    private string 取gdIp字体样式()
    {
      if (radioButton14.IsChecked == true) return "0";
      if (radioButton15.IsChecked == true) return "1";
      return radioButton16.IsChecked == true ? "2" : "3";
    }
    private string 取候选窗口候选排列方向模式()
    {
      if (radioButton8.IsChecked == true) return "1";
      return radioButton12.IsChecked == true ? "2" : "3";
    }
    private string 取词语联想上屏字符串长度()
    {
      if (radioButton.IsChecked == true) return "1";
      return radioButton1.IsChecked == true ? "2" : "3";
    }
    private string 取词语联想检索范围()
    {
      if (radioButton3.IsChecked == true) return "1";
      return radioButton4.IsChecked == true ? "2" : "3";
    }
    private string 取顶功规则()
    {
      if (radioButton454.IsChecked == true) return "1";
      return radioButton455.IsChecked == true ? "2" : "3";
    }
    private string 是或不是(bool b)
    {
      return b ? "是" : "不是";
    }
    private string 要或不要(bool b)
    {
      return b ? "要" : "不要";
    }



    #endregion

    #region 配色相关
    // 更新对应标签的背景颜色
    private void SetColorLableColor(SolidColorBrush cColor)
    {
      Label[] colorLabels = { color_Label_1, color_Label_2, color_Label_3, color_Label_4, color_Label_5, color_Label_6, color_Label_7, color_Label_8, color_Label_9 };
      // 计算反色
      var currentColor = cColor.Color;
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
      for (var i = 1; i <= colorLabels.Length; i++)
        if (i == _selectColorLabelNum)
        {
          colorLabels[i - 1].BorderBrush = new SolidColorBrush(invertedColor);
          colorLabels[i - 1].Background = cColor;
        }
    }

    private void RGB_ValueChanged(object sender, RoutedPropertyChangedEventArgs<string> e)
    {
      SetColorLableColor(RgbStringToColor(rgbTextBox.RGBText));
    }


    // 读取 Json 文件
    void LoadJson()
    {
      if (File.Exists(_schemeFilePath))
      {
        // 读取整个文件内容,将JSON字符串反序列化为对象
        var jsonString = File.ReadAllText(_schemeFilePath);
        var colorSchemesJson = JsonConvert.DeserializeObject<ColorSchemesCollection>(jsonString);
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
        File.WriteAllText(_schemeFilePath, jsonString);

        colorSchemeListBox.Items.Add("默认");
      }
    }




    // 更新所有候选字色（改为同一个颜色）
    private void HXZ_TextBoxText()
    {
      var rgb1 = Base.HexToRgb(color_Label_8.Background.ToString());
      var rgb2 = Base.HexToRgb(color_Label_9.Background.ToString());

      _bgString = $"<0={rgb1}>";
      for (var i = 1; i <= 26; i++)
        _bgString += $"<{i}={rgb2}>";
    }



    // 显示颜色的 label 鼠标进入事件
    private void Color_label_MouseEnter(object sender, MouseEventArgs e)
    {
      color_Label_001.Visibility = Visibility.Hidden;
      color_Label_002.Visibility = Visibility.Hidden;
      color_Label_003.Visibility = Visibility.Hidden;
      color_Label_004.Visibility = Visibility.Hidden;
      color_Label_005.Visibility = Visibility.Hidden;
      color_Label_006.Visibility = Visibility.Hidden;
      color_Label_007.Visibility = Visibility.Hidden;
      color_Label_008.Visibility = Visibility.Hidden;
      color_Label_009.Visibility = Visibility.Hidden;

      if (sender is not Label lb) return;
      switch (lb.Name)
      {
        case "color_Label_1":
          _selectColorLabelNum       = 1;
          color_Label_001.Visibility = Visibility.Visible;
          break;
        case "color_Label_2":
          _selectColorLabelNum       = 2;
          color_Label_002.Visibility = Visibility.Visible;
          break;
        case "color_Label_3":
          _selectColorLabelNum       = 3;
          color_Label_003.Visibility = Visibility.Visible;
          break;
        case "color_Label_4":
          _selectColorLabelNum       = 4;
          color_Label_004.Visibility = Visibility.Visible;
          break;
        case "color_Label_5":
          _selectColorLabelNum       = 5;
          color_Label_005.Visibility = Visibility.Visible;
          break;
        case "color_Label_6":
          _selectColorLabelNum       = 6;
          color_Label_006.Visibility = Visibility.Visible;
          break;
        case "color_Label_7":
          _selectColorLabelNum       = 7;
          color_Label_007.Visibility = Visibility.Visible;
          break;
        case "color_Label_8":
          _selectColorLabelNum       = 8;
          color_Label_008.Visibility = Visibility.Visible;
          break;
        case "color_Label_9":
          _selectColorLabelNum       = 9;
          color_Label_009.Visibility = Visibility.Visible;
          break;
      }

      var currentColor = ((SolidColorBrush)lb.Background).Color;
      // 计算反色
      var invertedColor = Color.FromArgb(255, (byte)~currentColor.R, (byte)~currentColor.G, (byte)~currentColor.B);
      lb.BorderThickness = new Thickness(3);
      lb.BorderBrush     = new SolidColorBrush(invertedColor);
      var hex = Base.RemoveChars(lb.Background.ToString(), 2);
      var rgb = Base.HexToRgb(hex);
      rgbTextBox.RGBText = rgb;
    }

    // 显示颜色的 label 鼠标离开事件
    private void Color_label_MouseLeave(object sender, MouseEventArgs e)
    {
      if (sender is Label label) label.BorderThickness = new Thickness(2);
    }

    // 候选框圆角、选中项背景圆角 和 候选框边框调节
    private void Nud11_ValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
    {
      if (hxk_Border == null) return;
      hxk_Border.CornerRadius    = hxc_CheckBox.IsChecked   == true ? new CornerRadius(nud11.Value) : new CornerRadius(0);
      hxz_Border.CornerRadius    = hxcbj_CheckBox.IsChecked == true ? new CornerRadius(nud12.Value) : new CornerRadius(0);
      hxk_Border.BorderThickness = new Thickness(nud13.Value);
    }

    // 配色列表双击事件
    private void ColorSchemeListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton != MouseButton.Left || colorSchemeListBox.SelectedItem == null) return;
      var schemeColor = _配色方案[colorSchemeListBox.SelectedIndex];
      checkBox_Copy42.IsChecked = schemeColor.显示背景图;
      hxc_CheckBox.IsChecked    = schemeColor.显示候选窗圆角;
      hxcbj_CheckBox.IsChecked  = schemeColor.显示选中项背景圆角;
      nud11.Value               = schemeColor.候选窗圆角;
      nud12.Value               = schemeColor.选中项圆角;
      nud13.Value               = schemeColor.边框线宽;
      color_Label_1.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.下划线色)!);
      color_Label_2.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.光标色)!);
      color_Label_3.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.分隔线色)!);
      color_Label_4.Background  = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.窗口边框色)!);

      if (schemeColor.窗背景底色 == "")
      {
        color_Label_5.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF")!);
        //_bkColor                 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF")!);
        hxcds_CheckBox.IsChecked = true;
      }
      else
      {
        color_Label_5.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.窗背景底色)!);
        //_bkColor                 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.窗背景底色)!);
        hxcds_CheckBox.IsChecked = false;
      }
      color_Label_6.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.选中背景色)!);
      color_Label_7.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.选中字体色)!);
      color_Label_8.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.编码字体色)!);
      color_Label_9.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(schemeColor.候选字色)!);
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
        File.WriteAllText(_schemeFilePath, jsonString);

        colorSchemeListBox.Items.Remove(name);
        colorSchemeListBox.Items.Refresh();
      }

    }

    // 添加配色
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {

      var name = color_Scheme_Name_TextBox.Text.Trim();
      _colorScheme = new ColorScheme
      {
        名称           = name,
        候选窗圆角     = nud11.Value,
        选中项圆角     = nud12.Value,
        边框线宽       = nud13.Value,
        显示背景图     = checkBox_Copy42.IsChecked != null && (bool)checkBox_Copy42.IsChecked,
        显示候选窗圆角     = hxc_CheckBox.IsChecked    != null && (bool)hxc_CheckBox.IsChecked,
        显示选中项背景圆角 = hxcbj_CheckBox.IsChecked  != null && (bool)hxcbj_CheckBox.IsChecked,
        窗背景底色 = hxcds_CheckBox.IsChecked == true ? "" :
          Base.RemoveChars(color_Label_5.Background.ToString(), 2),
        下划线色   = Base.RemoveChars(color_Label_1.Background.ToString(), 2),
        光标色     = Base.RemoveChars(color_Label_2.Background.ToString(), 2),
        分隔线色   = Base.RemoveChars(color_Label_3.Background.ToString(), 2),
        窗口边框色 = Base.RemoveChars(color_Label_4.Background.ToString(), 2),
        选中背景色 = Base.RemoveChars(color_Label_6.Background.ToString(), 2),
        选中字体色 = Base.RemoveChars(color_Label_7.Background.ToString(), 2),
        编码字体色 = Base.RemoveChars(color_Label_8.Background.ToString(), 2),
        候选字色   = Base.RemoveChars(color_Label_9.Background.ToString(), 2),
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
      File.WriteAllText(_schemeFilePath, jsonString);
    }

    private void Button3_Copy_Click(object sender, RoutedEventArgs e)
    {
      var selectedFontName = SelectFontName();
      if (selectedFontName == null) return;
      if (sender is Button) textBox_Copy145.Text = selectedFontName;
    }

    private static string SelectFontName()
    {
      using var fontDialog = new FontDialog();

      // 设置初始字体选项（可选）
      // fontDialog.Font = new Font("Arial", 12);
      // 显示字体对话框并获取用户的选择结果
      return fontDialog.ShowDialog() == DialogResult.OK ? fontDialog.Font.Name : // 返回用户选择的字体名称
        null;
    }

    private void TextBox_Copy22_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (textBox_Copy22.Text == "")
      {
        textBox_Copy22.Text = @"⁠⁣"; // 有个隐藏符号
      }
    }

    private void TextBox_Copy23_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (textBox_Copy23.Text == "")
      {
        textBox_Copy23.Text = @"⁠⁣"; // 有个隐藏符号
      }
    }

    private void ComboBox3_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      // var text = ((ComboBoxItem)comboBox3.SelectedItem).Content.ToString();
      if(comboBox3.SelectedIndex == -1) return;
      设置候选序号(comboBox3.SelectedValue.ToString());
    }

    private void 删除当前预设_Click(object sender, RoutedEventArgs e)
    {
      var s = comboBox3.SelectedIndex;
      if(s == -1) return;
      comboBox3.SelectedIndex = -1;
      comboBox3.Items.Remove(comboBox3.Items[s]);

      var text     = string.Empty;
      var filePath = $"{_appPath}\\configs\\候选序号.txt";
      text = comboBox3.Items.Cast<object>().Aggregate(text, (current, item) => current + $"\n{(item as ComboBoxItem)?.Content}");

      text = text.Trim('\n');
      File.WriteAllText(filePath, text);
    }

    private void 添加到预设_Click(object sender, RoutedEventArgs e)
    {
      var text     = 获取候选序号();
      var filePath = $"{_appPath}\\configs\\候选序号.txt";

      using var sw = File.AppendText(filePath);
      sw.WriteLine(text);
      comboBox3.Items.Add(text);
      MessageBox.Show("添加新预设成功");
    }

    private void 清空自定义序号_Click(object sender, RoutedEventArgs e)
    {
      hxxh1.Text = string.Empty;
      hxxh2.Text = string.Empty;
      hxxh3.Text = string.Empty;
      hxxh4.Text = string.Empty;
      hxxh5.Text = string.Empty;
      hxxh6.Text = string.Empty;
      hxxh7.Text = string.Empty;
      hxxh8.Text = string.Empty;
      hxxh9.Text = string.Empty;
      hxxh0.Text = string.Empty;
    }



    private void Hxc_checkBox_Checked(object sender, RoutedEventArgs e)
    {
      if (hxk_Border != null)
        hxk_Border.CornerRadius = new CornerRadius(nud11.Value);
    }

    private void Hxc_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
      if (hxk_Border != null)
        hxk_Border.CornerRadius = new CornerRadius(0);
    }

    private void Hxcbj_checkBox_Checked(object sender, RoutedEventArgs e)
    {
      if (hxz_Border != null)
        hxz_Border.CornerRadius = new CornerRadius(nud12.Value);
    }
    private void Hxcbj_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
      if (hxz_Border != null)
        hxz_Border.CornerRadius = new CornerRadius(0);
    }

    private void Hxcds_checkBox_Checked(object sender, RoutedEventArgs e)
    {
      color_Label_5.Visibility = Visibility.Hidden;
      //_bkColor = (SolidColorBrush)color_Label_5.Background;
      color_Label_005.Visibility = Visibility.Hidden;
      //_selectColorLabelNum = 0;
    }

    private void Hxcds_checkBox_Unchecked(object sender, RoutedEventArgs e)
    {
      color_Label_5.Visibility = Visibility.Visible;
      color_Label_005.Visibility = _selectColorLabelNum == 5 ? Visibility.Visible : Visibility.Hidden;
    }

    #endregion

  }
}
