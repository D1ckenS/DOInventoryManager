using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DOInventoryManager.Views.Print
{
    public partial class GenericReportPrint : UserControl
    {
        public GenericReportPrint()
        {
            InitializeComponent();
        }

        public void SetReportTitle(string title)
        {
            ReportTitleText.Text = title;
        }

        public void AddSummaryCard(string title, string value, string subtitle = "")
        {
            SummarySection.Visibility = Visibility.Visible;

            var cardPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 20, 10)
            };

            var contentPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 150
            };

            var titleBlock = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var valueBlock = new TextBlock
            {
                Text = value,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0)
            };

            contentPanel.Children.Add(titleBlock);
            contentPanel.Children.Add(valueBlock);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleBlock = new TextBlock
                {
                    Text = subtitle,
                    FontSize = 9,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.Gray
                };
                contentPanel.Children.Add(subtitleBlock);
            }

            cardPanel.Children.Add(contentPanel);
            SummaryCardsPanel.Children.Add(cardPanel);
        }

        public void AddSectionTitle(string title)
        {
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 20, 0, 10),
                Foreground = Brushes.Black
            };

            MainContentPanel.Children.Add(titleBlock);
        }

        public void AddDataGrid(DataGrid sourceGrid, string title = "")
        {
            if (!string.IsNullOrEmpty(title))
            {
                AddSectionTitle(title);
            }

            // Create a new DataGrid for printing
            var printGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Black,
                Background = Brushes.White,
                Foreground = Brushes.Black,
                FontSize = 10,
                Margin = new Thickness(0, 0, 0, 20)
            };

            // Copy columns from source grid
            foreach (var column in sourceGrid.Columns)
            {
                if (column is DataGridTextColumn textColumn)
                {
                    var newColumn = new DataGridTextColumn
                    {
                        Header = textColumn.Header,
                        Binding = textColumn.Binding,
                        Width = textColumn.Width
                    };
                    printGrid.Columns.Add(newColumn);
                }
                else if (column is DataGridTemplateColumn templateColumn)
                {
                    // For template columns, try to extract the binding or convert to text
                    var newColumn = new DataGridTextColumn
                    {
                        Header = templateColumn.Header,
                        Width = templateColumn.Width
                    };
                    printGrid.Columns.Add(newColumn);
                }
            }

            // Copy data source
            printGrid.ItemsSource = sourceGrid.ItemsSource;

            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 20),
                Child = printGrid
            };

            MainContentPanel.Children.Add(border);
        }

        public void AddTextBlock(string text, bool isBold = false, double fontSize = 12)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 5, 0, 5),
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap
            };

            MainContentPanel.Children.Add(textBlock);
        }

        public void AddSeparator()
        {
            var separator = new Border
            {
                Height = 1,
                Background = Brushes.Black,
                Margin = new Thickness(0, 10, 0, 10)
            };

            MainContentPanel.Children.Add(separator);
        }

        public void Clear()
        {
            SummaryCardsPanel.Children.Clear();
            MainContentPanel.Children.Clear();
            SummarySection.Visibility = Visibility.Collapsed;
        }
    }
}