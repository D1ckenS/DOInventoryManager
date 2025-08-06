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

        private Grid? summaryGrid;
        private int cardColumnIndex = 0;

        public void AddSummaryCard(string title, string value, string subtitle = "")
        {
            SummarySection.Visibility = Visibility.Visible;

            // Create horizontal grid layout like MonthlySummaryPrint (first card creates the grid)
            if (summaryGrid == null)
            {
                summaryGrid = new Grid
                {
                    Margin = new Thickness(8)
                };
                
                // Create columns for horizontal layout (max 5 cards per row)
                for (int i = 0; i < 5; i++)
                {
                    summaryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
                
                // Clear any existing content and add the grid
                SummaryCardsPanel.Children.Clear();
                SummaryCardsPanel.Children.Add(summaryGrid);
                cardColumnIndex = 0;
            }

            // Create card exactly like MonthlySummaryPrint structure
            var cardPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var titleBlock = new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                FontSize = 9, // Match MonthlySummaryPrint font size
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var valueBlock = new TextBlock
            {
                Text = value,
                FontSize = 11, // Match MonthlySummaryPrint font size  
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0) // Match MonthlySummaryPrint margin
            };

            cardPanel.Children.Add(titleBlock);
            cardPanel.Children.Add(valueBlock);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleBlock = new TextBlock
                {
                    Text = subtitle,
                    FontSize = 8,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.Gray
                };
                cardPanel.Children.Add(subtitleBlock);
            }

            // Place card in current column
            Grid.SetColumn(cardPanel, cardColumnIndex);
            summaryGrid.Children.Add(cardPanel);
            
            // Move to next column for next card
            cardColumnIndex++;
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

        public void AddDataGrid(DataGrid sourceGrid, string title = "", bool useOptimizedLayout = true)
        {
            if (!string.IsNullOrEmpty(title))
            {
                AddSectionTitle(title);
            }

            // Create container StackPanel exactly like MonthlySummaryPrint
            var containerPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Create DataGrid exactly like working MonthlySummaryPrint - NO WIDTH CONSTRAINT
            var printGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                BorderThickness = new Thickness(0), // No border on grid itself
                Background = Brushes.White,
                Foreground = Brushes.Black,
                FontSize = 7, // Match working print view
                MaxHeight = 150 // Prevent footer overflow - reduced from working 180px
            };

            // Add hierarchical scroll handling to the dynamically created DataGrid
            printGrid.PreviewMouseWheel += DataGrid_PreviewMouseWheel;

            if (useOptimizedLayout)
            {
                // Create optimized columns for printing
                AddOptimizedColumns(printGrid, sourceGrid);
            }
            else
            {
                // Use original column approach for simple grids
                AddOriginalColumns(printGrid, sourceGrid);
            }

            // Copy data source
            printGrid.ItemsSource = sourceGrid.ItemsSource;

            // Create simple border exactly like MonthlySummaryPrint - NO WIDTH CONSTRAINT
            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Child = printGrid
            };

            containerPanel.Children.Add(border);
            MainContentPanel.Children.Add(containerPanel);
        }

        private void AddOptimizedColumns(DataGrid printGrid, DataGrid sourceGrid)
        {
            var totalColumns = sourceGrid.Columns.Count;
            var columnIndex = 0;

            foreach (var column in sourceGrid.Columns)
            {
                if (column is DataGridTextColumn textColumn)
                {
                    var newColumn = new DataGridTextColumn
                    {
                        Header = textColumn.Header,
                        Binding = textColumn.Binding,
                        Width = GetOptimizedColumnWidth(textColumn.Header?.ToString() ?? "", columnIndex, totalColumns)
                    };
                    printGrid.Columns.Add(newColumn);
                }
                else if (column is DataGridTemplateColumn templateColumn)
                {
                    // For template columns, create text column with appropriate binding
                    var newColumn = new DataGridTextColumn
                    {
                        Header = templateColumn.Header,
                        Width = GetOptimizedColumnWidth(templateColumn.Header?.ToString() ?? "", columnIndex, totalColumns)
                    };
                    
                    // Try to extract binding from template column if possible
                    // This is a simplified approach - may need enhancement for complex templates
                    printGrid.Columns.Add(newColumn);
                }
                columnIndex++;
            }
        }

        private void AddOriginalColumns(DataGrid printGrid, DataGrid sourceGrid)
        {
            // Copy columns with original approach for simple cases
            foreach (var column in sourceGrid.Columns)
            {
                if (column is DataGridTextColumn textColumn)
                {
                    var newColumn = new DataGridTextColumn
                    {
                        Header = textColumn.Header,
                        Binding = textColumn.Binding,
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star) // Use star sizing
                    };
                    printGrid.Columns.Add(newColumn);
                }
                else if (column is DataGridTemplateColumn templateColumn)
                {
                    var newColumn = new DataGridTextColumn
                    {
                        Header = templateColumn.Header,
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                    };
                    printGrid.Columns.Add(newColumn);
                }
            }
        }

        private DataGridLength GetOptimizedColumnWidth(string headerText, int columnIndex, int totalColumns)
        {
            // Optimize column widths based on header content and position
            var header = headerText.ToLower();
            
            // ID columns and short status columns get smaller width
            if (header.Contains("id") || header.Contains("#") || header.Equals("type") || 
                header.Equals("status") || header.Contains("legs") || header.Contains("count"))
            {
                return new DataGridLength(0.6, DataGridLengthUnitType.Star);
            }
            
            // Name and description columns get more width
            if (header.Contains("name") || header.Contains("description") || header.Contains("supplier") ||
                header.Contains("vessel") || header.Contains("invoice") || header.Contains("reference"))
            {
                return new DataGridLength(1.5, DataGridLengthUnitType.Star);
            }
            
            // Date columns get medium width
            if (header.Contains("date") || header.Contains("due") || header.Contains("payment"))
            {
                return new DataGridLength(1.0, DataGridLengthUnitType.Star);
            }
            
            // Currency and value columns get appropriate width
            if (header.Contains("value") || header.Contains("cost") || header.Contains("price") || 
                header.Contains("amount") || header.Contains("$") || header.Contains("usd"))
            {
                return new DataGridLength(1.1, DataGridLengthUnitType.Star);
            }
            
            // Quantity columns
            if (header.Contains("quantity") || header.Contains("liter") || header.Contains("ton") ||
                header.Contains("consumption") || header.Contains("purchase"))
            {
                return new DataGridLength(1.2, DataGridLengthUnitType.Star);
            }
            
            // Default width for other columns
            return new DataGridLength(1.0, DataGridLengthUnitType.Star);
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

        public void AddCompactDataGrid(DataGrid sourceGrid, string title = "", int maxColumns = 8)
        {
            if (!string.IsNullOrEmpty(title))
            {
                AddSectionTitle(title);
            }

            // Create container StackPanel exactly like MonthlySummaryPrint
            var containerPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Create compact DataGrid with NO WIDTH CONSTRAINT - let it auto-fit
            var printGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                BorderThickness = new Thickness(0),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                FontSize = 6, // Even smaller font for wide tables
                MaxHeight = 120 // More restrictive height for compact display
            };

            // Add only the most important columns, up to maxColumns
            var columnCount = 0;
            foreach (var column in sourceGrid.Columns)
            {
                if (columnCount >= maxColumns) break;
                
                if (column is DataGridTextColumn textColumn)
                {
                    var header = textColumn.Header?.ToString() ?? "";
                    
                    // Skip less important columns for compact view
                    if (IsImportantColumn(header))
                    {
                        var newColumn = new DataGridTextColumn
                        {
                            Header = ShortenHeaderText(header),
                            Binding = textColumn.Binding,
                            Width = GetCompactColumnWidth(header, columnCount)
                        };
                        printGrid.Columns.Add(newColumn);
                        columnCount++;
                    }
                }
            }

            printGrid.ItemsSource = sourceGrid.ItemsSource;

            // Create simple border exactly like MonthlySummaryPrint - NO WIDTH CONSTRAINT
            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                Child = printGrid
            };

            containerPanel.Children.Add(border);
            MainContentPanel.Children.Add(containerPanel);
        }

        private bool IsImportantColumn(string header)
        {
            var h = header.ToLower();
            
            // Always include these essential columns
            if (h.Contains("name") || h.Contains("supplier") || h.Contains("vessel") ||
                h.Contains("date") || h.Contains("invoice") || h.Contains("reference") ||
                h.Contains("quantity") || h.Contains("value") || h.Contains("amount") ||
                h.Contains("status") || h.Contains("due") || h.Contains("balance"))
            {
                return true;
            }
            
            // Skip detailed calculation columns in compact view
            if (h.Contains("calculation") || h.Contains("detail") || h.Contains("breakdown") ||
                h.Contains("intermediate") || h.Contains("temp"))
            {
                return false;
            }
            
            return true; // Include other columns if space allows
        }

        private string ShortenHeaderText(string header)
        {
            // Abbreviate common long headers for compact printing
            return header
                .Replace("Quantity", "Qty")
                .Replace("Purchase", "Purch")
                .Replace("Consumption", "Consump")
                .Replace("Remaining", "Rem")
                .Replace("Beginning", "Beg")
                .Replace("Ending", "End")
                .Replace("Original", "Orig")
                .Replace("Supplier", "Supp")
                .Replace("Reference", "Ref")
                .Replace("Invoice", "Inv")
                .Replace("Payment", "Pay")
                .Replace("Outstanding", "Outst");
        }

        private DataGridLength GetCompactColumnWidth(string headerText, int columnIndex)
        {
            var header = headerText.ToLower();
            
            // More aggressive width optimization for compact view
            if (header.Contains("id") || header.Contains("#") || header.Contains("count") || header.Contains("legs"))
            {
                return new DataGridLength(0.4, DataGridLengthUnitType.Star);
            }
            
            if (header.Contains("name") || header.Contains("supplier") || header.Contains("vessel"))
            {
                return new DataGridLength(1.3, DataGridLengthUnitType.Star);
            }
            
            if (header.Contains("date"))
            {
                return new DataGridLength(0.8, DataGridLengthUnitType.Star);
            }
            
            if (header.Contains("value") || header.Contains("amount") || header.Contains("cost"))
            {
                return new DataGridLength(0.9, DataGridLengthUnitType.Star);
            }
            
            return new DataGridLength(0.7, DataGridLengthUnitType.Star);
        }

        public void Clear()
        {
            SummaryCardsPanel.Children.Clear();
            MainContentPanel.Children.Clear();
            SummarySection.Visibility = Visibility.Collapsed;
            
            // Reset summary grid state for next usage
            summaryGrid = null;
            cardColumnIndex = 0;
        }

        private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid == null) return;

            // Check if DataGrid needs internal scrolling first
            var dataGridScrollViewer = FindChild<ScrollViewer>(dataGrid);
            if (dataGridScrollViewer != null)
            {
                // If scrolling down and can scroll down, let DataGrid handle it
                if (e.Delta < 0 && dataGridScrollViewer.VerticalOffset < dataGridScrollViewer.ScrollableHeight)
                    return;

                // If scrolling up and can scroll up, let DataGrid handle it  
                if (e.Delta > 0 && dataGridScrollViewer.VerticalOffset > 0)
                    return;
            }

            // DataGrid doesn't need scrolling, bubble up through ScrollViewer hierarchy
            e.Handled = true;
            BubbleScrollToParentScrollViewers(dataGrid, e.Delta, e.MouseDevice, e.Timestamp);
        }

        private void BubbleScrollToParentScrollViewers(DependencyObject startElement, int delta, System.Windows.Input.MouseDevice mouseDevice, int timestamp)
        {
            var currentElement = startElement;
            
            // Find all parent ScrollViewers and try scrolling them in order
            while (currentElement != null)
            {
                var parentScrollViewer = FindParent<ScrollViewer>(currentElement);
                if (parentScrollViewer == null)
                    break;

                // Check if this ScrollViewer can handle the scroll
                if (CanScrollViewerHandle(parentScrollViewer, delta))
                {
                    // Found a ScrollViewer that can handle the scroll, send event to it
                    var newEvent = new System.Windows.Input.MouseWheelEventArgs(mouseDevice, timestamp, delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent
                    };
                    parentScrollViewer.RaiseEvent(newEvent);
                    return; // Successfully handled, stop bubbling
                }

                // This ScrollViewer can't handle it, continue to its parent
                currentElement = parentScrollViewer;
            }
        }

        private bool CanScrollViewerHandle(ScrollViewer scrollViewer, int delta)
        {
            if (scrollViewer == null) return false;

            // Check if ScrollViewer can scroll in the requested direction
            if (delta < 0) // Scrolling down
            {
                return scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;
            }
            else // Scrolling up
            {
                return scrollViewer.VerticalOffset > 0;
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null && parent is not T)
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childResult = FindChild<T>(child);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }
    }
}