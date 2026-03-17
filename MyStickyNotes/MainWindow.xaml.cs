using System.IO; // 🌟 新增：用来读写文件的工具包
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyStickyNotes
{
    public partial class MainWindow : Window
    {
        string dataFilePath = "memos.txt"; // 存档文件就叫这个名字，保存在软件旁边

        public MainWindow()
        {
            InitializeComponent();
            LoadMemos(); // 🌟 启动时第一件事：读取本地存档！
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // 🌟 读取存档的魔法
        private void LoadMemos()
        {
            if (File.Exists(dataFilePath))
            {
                string[] lines = File.ReadAllLines(dataFilePath);
                foreach (string line in lines)
                {
                    if (line.Length >= 2)
                    {
                        bool isDone = line.StartsWith("1|"); // 1|代表划了删除线
                        string text = line.Substring(2);     // 截取后面的真实文字
                        AddMemo(text, -1, isDone, false);    // false代表读取时先不着急保存
                    }
                }
            }
            // 读取完历史记录后，永远在最下面补一个空行，方便你直接打字
            AddMemo("", -1, false, false);
        }

        // 🌟 保存存档的魔法
        private void SaveMemos()
        {
            try
            {
                List<string> lines = new List<string>();
                foreach (ListBoxItem item in MemoList.Items)
                {
                    Grid row = item.Content as Grid;
                    if (row == null) continue;

                    Grid container = row.Children[1] as Grid;
                    TextBlock display = container.Children[0] as TextBlock;

                    // 如果这一行连一个字都没写，就不当做记录保存它
                    if (string.IsNullOrWhiteSpace(display.Text)) continue;

                    bool isDone = display.TextDecorations == TextDecorations.Strikethrough;
                    string prefix = isDone ? "1|" : "0|"; // 拼个暗号：0|代表没做完，1|代表做完了
                    lines.Add(prefix + display.Text);
                }
                File.WriteAllLines(dataFilePath, lines); // 把暗号和文字统统写进硬盘里！
            }
            catch { } // 防止意外报错
        }

        // 终极完美版“制造备忘录”机器：支持自动换行 + 存档记录
        private void AddMemo(string text, int insertIndex = -1, bool isDone = false, bool autoSave = true)
        {
            ListBoxItem listItem = new ListBoxItem();
            listItem.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            listItem.Background = System.Windows.Media.Brushes.Transparent;
            listItem.Focusable = false;

            // 🌟 核心改进：把横向排列换成 Grid（表格结构），这样超出窗口宽度才会自动换行！
            Grid rowGrid = new Grid();
            rowGrid.Margin = new Thickness(0, 5, 0, 5);
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto }); // 第一列：刚好装下图标
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }); // 第二列：霸占剩下的所有宽度，用来挤压文字换行

            // 图标
            TextBlock icon = new TextBlock();
            icon.Text = "📄 ";
            icon.Foreground = System.Windows.Media.Brushes.LightGreen;
            icon.VerticalAlignment = VerticalAlignment.Top; // 文字换行变多行后，图标靠最上对齐比较好看
            icon.Margin = new Thickness(0, 3, 0, 0); // 稍微往下挪一点点，和第一行文字对齐
            Grid.SetColumn(icon, 0); // 把图标塞进第一列

            // 文字容器
            Grid textContainer = new Grid();
            textContainer.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(textContainer, 1); // 把文字容器塞进第二列

            // 固定文字
            TextBlock displayContent = new TextBlock();
            displayContent.Text = text;
            displayContent.Foreground = System.Windows.Media.Brushes.White;
            displayContent.FontSize = 16;
            displayContent.Margin = new Thickness(2, 0, 0, 0);
            displayContent.TextWrapping = TextWrapping.Wrap; // 🌟 核心改进：允许自动换行！

            // 恢复删除线状态
            if (isDone) displayContent.TextDecorations = TextDecorations.Strikethrough;

            // 输入框
            TextBox editContent = new TextBox();
            editContent.Text = text;
            editContent.Foreground = System.Windows.Media.Brushes.White;
            editContent.FontSize = 16;
            editContent.Background = System.Windows.Media.Brushes.Transparent;
            editContent.BorderThickness = new Thickness(0);
            editContent.TextWrapping = TextWrapping.Wrap; // 🌟 核心改进：允许自动换行！

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

            // 回车事件
            editContent.KeyDown += (s, e) => {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    displayContent.Text = editContent.Text;
                    editContent.Visibility = Visibility.Collapsed;
                    displayContent.Visibility = Visibility.Visible;

                    int currentIndex = MemoList.Items.IndexOf(listItem);
                    AddMemo("", currentIndex + 1); // 裂变新行
                    SaveMemos(); // 🌟 打完字，立刻保存数据！
                }
            };

            // 右键菜单
            ContextMenu menu = new ContextMenu();
            MenuItem modifyMenu = new MenuItem();
            modifyMenu.Header = "修改内容";
            modifyMenu.Click += (s, e) => {
                displayContent.Visibility = Visibility.Collapsed;
                editContent.Visibility = Visibility.Visible;
                editContent.Focus();
                editContent.CaretIndex = editContent.Text.Length;
            };

            MenuItem strikeMenu = new MenuItem();
            strikeMenu.Header = "添加/移除删除线";
            strikeMenu.Click += (s, e) => {
                if (displayContent.TextDecorations == TextDecorations.Strikethrough)
                    displayContent.TextDecorations = null;
                else
                    displayContent.TextDecorations = TextDecorations.Strikethrough;

                SaveMemos(); // 🌟 划线状态改变，立刻保存！
            };

            MenuItem deleteMenu = new MenuItem();
            deleteMenu.Header = "删除(D)";
            deleteMenu.Click += (s, e) => {
                MemoList.Items.Remove(listItem);
                SaveMemos(); // 🌟 删除记录，立刻保存！
            };

            menu.Items.Add(modifyMenu);
            menu.Items.Add(strikeMenu);
            menu.Items.Add(deleteMenu);

            listItem.ContextMenu = menu;
            displayContent.ContextMenu = menu;
            editContent.ContextMenu = menu;

            // 组装最终的一行
            rowGrid.Children.Add(icon);
            rowGrid.Children.Add(textContainer);
            listItem.Content = rowGrid;

            if (insertIndex == -1)
                MemoList.Items.Add(listItem);
            else
                MemoList.Items.Insert(insertIndex, listItem);

            if (editContent.Visibility == Visibility.Visible)
            {
                editContent.Loaded += (s, e) => editContent.Focus();
            }

            if (autoSave) SaveMemos(); // 🌟 如果不是在读取文件，就执行保存
        }
    }
}