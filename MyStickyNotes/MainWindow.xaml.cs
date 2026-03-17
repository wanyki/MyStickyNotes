using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyStickyNotes
{
    public partial class MainWindow : Window
    {
        string dataFilePath = "memos.txt";
        // 🌟 召唤托盘图标的变量
        private System.Windows.Forms.NotifyIcon trayIcon;
        public MainWindow()
        {
            InitializeComponent();
            LoadMemos();
            // 🌟 启动时，配置并显示右下角的托盘图标
            SetupTrayIcon();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void LoadMemos()
        {
            if (File.Exists(dataFilePath))
            {
                string[] lines = File.ReadAllLines(dataFilePath);
                foreach (string line in lines)
                {
                    if (line.Length >= 2)
                    {
                        bool isDone = line.StartsWith("1|");
                        string text = line.Substring(2);
                        AddMemo(text, -1, isDone, false);
                    }
                }
            }
            AddMemo("", -1, false, false);
        }

        // 🌟 修复了黄色警告：更新了查找文字的正确路径
        private void SaveMemos()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (ListBoxItem item in MemoList.Items)
                {
                    // 严谨地逐层拨开我们做的“俄罗斯套娃”，寻找文字
                    if (item.Content as Border is Border border &&
                        border.Child as Grid is Grid rowGrid &&
                        rowGrid.Children.Count > 1 &&
                        rowGrid.Children[1] as Grid is Grid container &&
                        container.Children.Count > 0 &&
                        container.Children[0] as TextBlock is TextBlock display)
                    {
                        if (string.IsNullOrWhiteSpace(display.Text)) continue;

                        bool isDone = display.TextDecorations == TextDecorations.Strikethrough;
                        string prefix = isDone ? "1|" : "0|";
                        lines.Add(prefix + display.Text);
                    }
                }
                File.WriteAllLines(dataFilePath, lines);
            }
            catch { }
        }

        private void AddMemo(string text, int insertIndex = -1, bool isDone = false, bool autoSave = true)
        {
            ListBoxItem listItem = new ListBoxItem();
            listItem.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
            listItem.Background = System.Windows.Media.Brushes.Transparent;
            listItem.Focusable = false;

            // 创建黑色阴影
            System.Windows.Media.Effects.DropShadowEffect textShadow = new System.Windows.Media.Effects.DropShadowEffect();
            textShadow.ShadowDepth = 1;
            textShadow.BlurRadius = 2;
            textShadow.Color = Colors.Black;
            textShadow.Opacity = 0.8;

            Grid rowGrid = new Grid();
            rowGrid.Margin = new Thickness(0, 5, 0, 5);
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock icon = new TextBlock();
            icon.Text = "📄 ";
            icon.Foreground = System.Windows.Media.Brushes.LightGreen;
            icon.VerticalAlignment = VerticalAlignment.Top;
            icon.Margin = new Thickness(0, 3, 0, 0);
            icon.Effect = textShadow;
            Grid.SetColumn(icon, 0);

            Grid textContainer = new Grid();
            textContainer.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(textContainer, 1);

            TextBlock displayContent = new TextBlock();
            displayContent.Text = text;
            displayContent.Foreground = System.Windows.Media.Brushes.White;
            displayContent.FontSize = 16;
            displayContent.Margin = new Thickness(2, 0, 0, 0);
            displayContent.TextWrapping = TextWrapping.Wrap;
            displayContent.Effect = textShadow;

            if (isDone) displayContent.TextDecorations = TextDecorations.Strikethrough;

            TextBox editContent = new TextBox();
            editContent.Text = text;
            editContent.Foreground = System.Windows.Media.Brushes.White;
            editContent.FontSize = 16;
            editContent.Background = System.Windows.Media.Brushes.Transparent;
            editContent.BorderThickness = new Thickness(0);
            editContent.TextWrapping = TextWrapping.Wrap;

            if (text == "")
            {
                displayContent.Visibility = Visibility.Collapsed;
                editContent.Visibility = Visibility.Visible;
            }
            else
            {
                displayContent.Visibility = Visibility.Visible;
                editContent.Visibility = Visibility.Collapsed;
            }

            textContainer.Children.Add(displayContent);
            textContainer.Children.Add(editContent);

            editContent.KeyDown += (s, e) => {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    displayContent.Text = editContent.Text;
                    editContent.Visibility = Visibility.Collapsed;
                    displayContent.Visibility = Visibility.Visible;

                    int currentIndex = MemoList.Items.IndexOf(listItem);
                    AddMemo("", currentIndex + 1);
                    SaveMemos();
                }
            };

            // 🌟 核心改进：完美复刻你的右键菜单
            ContextMenu menu = new ContextMenu();

            // 1. 复制 (终极核武器：多线程并行处理)
            MenuItem copyMenu = new MenuItem();
            copyMenu.Header = "复制(C)";
            copyMenu.Click += (s, e) => {
                // 🌟 第一步：先在主界面瞬间把文字“抄”下来
                string textToCopy = displayContent.Text;

                if (!string.IsNullOrEmpty(textToCopy))
                {
                    // 🌟 第二步：召唤一个隐形的“后台替身”（独立线程）去处理剪贴板
                    System.Threading.Thread thread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            Clipboard.SetText(textToCopy);
                        }
                        catch { } // 就算替身被系统卡死，也绝对牵连不到主界面
                    });

                    // Windows 规定：动剪贴板的替身，必须挂上这个“STA”专属牌照
                    thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    thread.IsBackground = true; // 随叫随到，用完即焚
                    thread.Start(); // 替身出发！主界面直接拍拍屁股走人！
                }
            };

            // 2. 删除
            MenuItem deleteMenu = new MenuItem();
            deleteMenu.Header = "删除(D)";
            deleteMenu.Click += (s, e) => {
                MemoList.Items.Remove(listItem);
                SaveMemos();
            };

            // 3. 重命名 (也就是我们之前的修改内容)
            MenuItem renameMenu = new MenuItem();
            renameMenu.Header = "重命名(M)";
            renameMenu.Click += (s, e) => {
                displayContent.Visibility = Visibility.Collapsed;
                editContent.Visibility = Visibility.Visible;
                editContent.Focus();
                editContent.CaretIndex = editContent.Text.Length;
            };

            // 分割线
            Separator separator = new Separator();

            // 4. 删除线 (动态改变名字)
            MenuItem strikeMenu = new MenuItem();
            strikeMenu.Header = isDone ? "移除删除线" : "添加删除线";
            strikeMenu.Click += (s, e) => {
                if (displayContent.TextDecorations == TextDecorations.Strikethrough)
                {
                    displayContent.TextDecorations = null;
                    strikeMenu.Header = "添加删除线"; // 改变菜单文字
                }
                else
                {
                    displayContent.TextDecorations = TextDecorations.Strikethrough;
                    strikeMenu.Header = "移除删除线"; // 改变菜单文字
                }
                SaveMemos();
            };

            // 按你截图的顺序组装菜单
            menu.Items.Add(copyMenu);
            menu.Items.Add(deleteMenu);
            menu.Items.Add(renameMenu);
            menu.Items.Add(separator);
            menu.Items.Add(strikeMenu);

            listItem.ContextMenu = menu;
            displayContent.ContextMenu = menu;
            editContent.ContextMenu = menu;

            rowGrid.Children.Add(icon);
            rowGrid.Children.Add(textContainer);

            Border clickBorder = new Border();
            clickBorder.Background = System.Windows.Media.Brushes.Transparent;

            // 🌟 修复了红色报错：是 Child 不是 Content！
            clickBorder.Child = rowGrid;
            clickBorder.ContextMenu = menu;

            listItem.Content = clickBorder;

            if (insertIndex == -1)
                MemoList.Items.Add(listItem);
            else
                MemoList.Items.Insert(insertIndex, listItem);

            if (editContent.Visibility == Visibility.Visible)
            {
                editContent.Loaded += (s, e) => editContent.Focus();
            }

            if (autoSave) SaveMemos();
        }
        // 🌟 配置右下角托盘图标的具体逻辑
        private void SetupTrayIcon()
        {
            trayIcon = new System.Windows.Forms.NotifyIcon();
            // 自动提取软件本身的图标
            trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            trayIcon.Text = "极简备忘录"; // 鼠标悬停时显示的文字
            trayIcon.Visible = true;

            // 🌟 左键双击图标：隐藏或显示便签
            trayIcon.DoubleClick += (s, e) =>
            {
                if (this.IsVisible) this.Hide();
                else this.Show();
            };

            // 🌟 右键菜单
            System.Windows.Forms.ContextMenuStrip menu = new System.Windows.Forms.ContextMenuStrip();

            menu.Items.Add("显示 / 隐藏", null, (s, e) => {
                if (this.IsVisible) this.Hide();
                else this.Show();
            });

            menu.Items.Add("-"); // 一条分割线

            menu.Items.Add("彻底退出", null, (s, e) => {
                trayIcon.Dispose(); // 退出前把图标销毁，不然会残留在右下角
                System.Windows.Application.Current.Shutdown();
            });

            trayIcon.ContextMenuStrip = menu;
        }

        // 🌟 防止按 Alt+F4 时软件关掉，改成隐藏
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // 拦截关闭事件
            this.Hide();     // 只是隐藏起来
        }
    }
}