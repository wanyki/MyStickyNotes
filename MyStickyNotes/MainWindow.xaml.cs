using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MyStickyNotes
{
    public partial class MainWindow : Window
    {
        string dataFilePath = "memos.txt";
        private System.Windows.Forms.NotifyIcon? trayIcon;
        private bool isLocked = false;

        public MainWindow()
        {
            InitializeComponent();

            // 给整个空白背景加上专属的“新建”右键菜单
            ContextMenu bgMenu = new ContextMenu();
            var bgNewM = new MenuItem { Header = "新建(N)" };
            bgNewM.Click += (s, e) => { AddMemo(""); };
            bgMenu.Items.Add(bgNewM);
            MemoList.ContextMenu = bgMenu;

            // 🌟 核心拦截 1：如果上锁了，直接没收“右键菜单”弹出的权利！
            // 这样就彻底封死了新建、删除、复制、重命名的入口
            MemoList.ContextMenuOpening += (s, e) => {
                if (isLocked) e.Handled = true;
            };

            LoadData();
            SetupTrayIcon();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();

            if (!isLocked && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
                SaveData();
            }
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            isLocked = !isLocked;
            LockButton.Content = isLocked ? "🔒" : "🔓";

            // 🌟 核心拦截 2：上锁时，隐藏缩放手柄，并让标题彻底不可点击
            this.ResizeMode = isLocked ? ResizeMode.NoResize : ResizeMode.CanResizeWithGrip;
            TitleBox.IsHitTestVisible = !isLocked;

            SaveData();
        }

        private void TitleBox_LostFocus(object sender, RoutedEventArgs e) => SaveData();

        private void LoadData()
        {
            if (File.Exists(dataFilePath))
            {
                string[] lines = File.ReadAllLines(dataFilePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("CONFIG|"))
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            TitleBox.Text = parts[1];
                            this.Left = double.Parse(parts[2]);
                            this.Top = double.Parse(parts[3]);
                            isLocked = parts[4] == "1";
                            LockButton.Content = isLocked ? "🔒" : "🔓";

                            // 🌟 核心拦截 3：启动软件时，如果读取到是上锁状态，立刻应用限制
                            this.ResizeMode = isLocked ? ResizeMode.NoResize : ResizeMode.CanResizeWithGrip;
                            TitleBox.IsHitTestVisible = !isLocked;
                        }
                    }
                    else if (line.Length >= 2)
                    {
                        AddMemo(line.Substring(2), -1, line.StartsWith("1|"), false);
                    }
                }
            }

            if (MemoList.Items.Count == 0)
            {
                AddMemo("", -1, false, false);
            }
        }

        private void SaveData()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add($"CONFIG|{TitleBox.Text}|{this.Left}|{this.Top}|{(isLocked ? 1 : 0)}");

                foreach (ListBoxItem item in MemoList.Items)
                {
                    if (item.Content is Border clickBorder && clickBorder.Child is Grid rowGrid)
                    {
                        if (rowGrid.Children.Count > 1 && rowGrid.Children[1] is Grid textContainer)
                        {
                            if (textContainer.Children.Count > 0 && textContainer.Children[0] is TextBlock t)
                            {
                                if (string.IsNullOrWhiteSpace(t.Text)) continue;
                                lines.Add((t.TextDecorations == TextDecorations.Strikethrough ? "1|" : "0|") + t.Text);
                            }
                        }
                    }
                }
                File.WriteAllLines(dataFilePath, lines);
            }
            catch { }
        }

        private void AddMemo(string text, int insertIndex = -1, bool isDone = false, bool autoSave = true)
        {
            ListBoxItem listItem = new ListBoxItem
            {
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch,
                Background = System.Windows.Media.Brushes.Transparent,
                Focusable = false
            };

            var textShadow = new System.Windows.Media.Effects.DropShadowEffect { ShadowDepth = 1, BlurRadius = 2, Color = Colors.Black, Opacity = 0.8 };
            Grid rowGrid = new Grid { Margin = new Thickness(0, 5, 0, 5) };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock icon = new TextBlock { Text = "📄 ", Foreground = System.Windows.Media.Brushes.LightGreen, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 3, 0, 0), Effect = textShadow };
            Grid.SetColumn(icon, 0);

            Grid textContainer = new Grid { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(textContainer, 1);

            TextBlock displayContent = new TextBlock { Text = text, Foreground = System.Windows.Media.Brushes.White, FontSize = 16, TextWrapping = TextWrapping.Wrap, Effect = textShadow };
            if (isDone) displayContent.TextDecorations = TextDecorations.Strikethrough;

            TextBox editContent = new TextBox { Text = text, Foreground = System.Windows.Media.Brushes.White, FontSize = 16, Background = System.Windows.Media.Brushes.Transparent, BorderThickness = new Thickness(0), TextWrapping = TextWrapping.Wrap, Visibility = text == "" ? Visibility.Visible : Visibility.Collapsed };
            displayContent.Visibility = text == "" ? Visibility.Collapsed : Visibility.Visible;

            textContainer.Children.Add(displayContent);
            textContainer.Children.Add(editContent);

            Action commitEdit = () => {
                if (editContent.Visibility != Visibility.Visible) return;

                displayContent.Text = editContent.Text;
                editContent.Visibility = Visibility.Collapsed;
                displayContent.Visibility = Visibility.Visible;

                if (string.IsNullOrWhiteSpace(editContent.Text) && MemoList.Items.Count > 1)
                {
                    MemoList.Items.Remove(listItem);
                }

                SaveData();
            };

            editContent.KeyDown += (s, e) => {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    commitEdit();
                }
            };

            editContent.LostFocus += (s, e) => {
                commitEdit();
            };

            ContextMenu menu = new ContextMenu();

            var newM = new MenuItem { Header = "新建(N)" };
            newM.Click += (s, e) => {
                int currentIndex = MemoList.Items.IndexOf(listItem);
                AddMemo("", currentIndex + 1);
            };

            var copyM = new MenuItem { Header = "复制(C)" };
            copyM.Click += (s, e) => {
                string t = displayContent.Text;
                if (!string.IsNullOrEmpty(t))
                {
                    System.Threading.Thread thread = new System.Threading.Thread(() => {
                        try { Clipboard.SetText(t); } catch { }
                    });
                    thread.SetApartmentState(System.Threading.ApartmentState.STA);
                    thread.IsBackground = true;
                    thread.Start();
                }
            };

            var delM = new MenuItem { Header = "删除(D)" };
            delM.Click += (s, e) => {
                MemoList.Items.Remove(listItem);
                if (MemoList.Items.Count == 0) AddMemo("");
                SaveData();
            };

            var renM = new MenuItem { Header = "重命名(M)" };
            renM.Click += (s, e) => {
                displayContent.Visibility = Visibility.Collapsed;
                editContent.Visibility = Visibility.Visible;
                editContent.Focus();
                editContent.Select(editContent.Text.Length, 0);
            };

            var strikeM = new MenuItem { Header = isDone ? "移除删除线" : "添加删除线" };
            strikeM.Click += (s, e) => {
                displayContent.TextDecorations = (displayContent.TextDecorations == TextDecorations.Strikethrough) ? null : TextDecorations.Strikethrough;
                strikeM.Header = displayContent.TextDecorations == TextDecorations.Strikethrough ? "移除删除线" : "添加删除线";
                SaveData();
            };

            menu.Items.Add(newM);
            menu.Items.Add(new Separator());
            menu.Items.Add(copyM);
            menu.Items.Add(delM);
            menu.Items.Add(renM);
            menu.Items.Add(new Separator());
            menu.Items.Add(strikeM);

            rowGrid.Children.Add(icon);
            rowGrid.Children.Add(textContainer);

            Border clickBorder = new Border { Background = System.Windows.Media.Brushes.Transparent, Child = rowGrid, ContextMenu = menu };
            listItem.Content = clickBorder;

            if (insertIndex == -1) MemoList.Items.Add(listItem);
            else MemoList.Items.Insert(insertIndex, listItem);

            if (editContent.Visibility == Visibility.Visible) editContent.Loaded += (s, e) => editContent.Focus();
            if (autoSave) SaveData();
        }

        private void MemoList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 🌟 核心拦截 4：如果上锁了，直接无视所有的鼠标左键点击，不准进入修改模式！
            if (isLocked) return;

            this.Focus();
        }

        private void SetupTrayIcon()
        {
            trayIcon = new System.Windows.Forms.NotifyIcon();
            try
            {
                trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch { }
            trayIcon.Text = "极简备忘录";
            trayIcon.Visible = true;
            trayIcon.DoubleClick += (s, e) => { if (this.IsVisible) this.Hide(); else { this.Show(); this.Activate(); } };

            System.Windows.Forms.ContextMenuStrip menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("显示 / 隐藏", null, (s, e) => { if (this.IsVisible) this.Hide(); else { this.Show(); this.Activate(); } });
            menu.Items.Add("-");
            menu.Items.Add("彻底退出", null, (s, e) => { trayIcon.Dispose(); System.Windows.Application.Current.Shutdown(); });
            trayIcon.ContextMenuStrip = menu;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}