using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using Microsoft.Win32;

namespace DOInventoryManager.Services
{
    public class PrintService
    {
        public class PrintResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? FilePath { get; set; }
        }

        /// <summary>
        /// Shows print preview dialog for any UserControl
        /// </summary>
        public PrintResult ShowPrintPreview(UserControl printContent, string documentTitle = "Report")
        {
            try
            {
                // Create print dialog with landscape default
                PrintDialog printDialog = new PrintDialog();

                // Set default to landscape
                if (printDialog.PrintTicket != null)
                {
                    printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                }

                // Create document
                var document = CreatePrintDocument(printContent, documentTitle);

                // Show preview dialog
                var previewWindow = new PrintPreviewWindow(document, printDialog, documentTitle);
                previewWindow.Owner = Application.Current.MainWindow;

                bool? result = previewWindow.ShowDialog();

                if (result == true)
                {
                    return new PrintResult
                    {
                        Success = true,
                        Message = "Document printed successfully."
                    };
                }
                else
                {
                    return new PrintResult
                    {
                        Success = false,
                        Message = "Print operation cancelled."
                    };
                }
            }
            catch (Exception ex)
            {
                return new PrintResult
                {
                    Success = false,
                    Message = $"Print error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Exports report directly to PDF
        /// </summary>
        public PrintResult ExportToPdf(UserControl printContent, string documentTitle = "Report")
        {
            try
            {
                // Show save dialog
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = $"{documentTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Create XPS document first
                    string tempXpsPath = Path.GetTempFileName() + ".xps";

                    try
                    {
                        // Create XPS document
                        var document = CreatePrintDocument(printContent, documentTitle);

                        using (XpsDocument xpsDoc = new XpsDocument(tempXpsPath, FileAccess.Write))
                        {
                            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                            writer.Write(document);
                        }

                        // Convert XPS to PDF (this requires additional implementation)
                        // For now, we'll save as XPS and inform user
                        string xpsOutputPath = saveDialog.FileName.Replace(".pdf", ".xps");
                        File.Copy(tempXpsPath, xpsOutputPath, true);

                        return new PrintResult
                        {
                            Success = true,
                            Message = $"Document exported successfully to: {xpsOutputPath}\n\nNote: Saved as XPS format. You can convert to PDF using Microsoft Print to PDF or online converters.",
                            FilePath = xpsOutputPath
                        };
                    }
                    finally
                    {
                        // Clean up temp file
                        if (File.Exists(tempXpsPath))
                            File.Delete(tempXpsPath);
                    }
                }
                else
                {
                    return new PrintResult
                    {
                        Success = false,
                        Message = "Export cancelled."
                    };
                }
            }
            catch (Exception ex)
            {
                return new PrintResult
                {
                    Success = false,
                    Message = $"Export error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Creates a print-ready document from UserControl
        /// </summary>
        private FixedDocument CreatePrintDocument(UserControl content, string title)
        {
            // Use the original content directly
            var printContent = content;

            // Set print-specific properties for LANDSCAPE orientation
            printContent.Width = 10.5 * 96; // Landscape width minus margins
            printContent.Height = 7.5 * 96; // Landscape height minus margins
            printContent.Margin = new Thickness(10); // Small margins

            // Create page content
            var pageContent = new PageContent();
            var fixedPage = new FixedPage
            {
                Width = 11 * 96,   // Standard landscape width
                Height = 8.5 * 96, // Standard landscape height
                Background = Brushes.White
            };

            // Add header
            var header = CreateHeader(title);
            FixedPage.SetLeft(header, 0.25 * 96);
            FixedPage.SetTop(header, 0.25 * 96);
            fixedPage.Children.Add(header);

            // Add main content
            FixedPage.SetLeft(printContent, 0.25 * 96);
            FixedPage.SetTop(printContent, 0.75 * 96); // Below header
            fixedPage.Children.Add(printContent);

            // Add footer
            var footer = CreateFooter();
            FixedPage.SetLeft(footer, 0.25 * 96);
            FixedPage.SetTop(footer, 7.75 * 96); // Near bottom
            fixedPage.Children.Add(footer);

            ((IAddChild)pageContent).AddChild(fixedPage);

            // Create document
            var document = new FixedDocument();
            document.Pages.Add(pageContent);

            return document;
        }

        /// <summary>
        /// Creates print header
        /// </summary>
        private UIElement CreateHeader(string title)
        {
            var headerPanel = new StackPanel
            {
                Width = 10 * 96, // Landscape width minus margins
                Orientation = Orientation.Vertical
            };

            // Company title
            var companyTitle = new TextBlock
            {
                Text = "DO INVENTORY MANAGER",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Report title
            var reportTitle = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Separator line
            var separator = new Border
            {
                Height = 1,
                Background = Brushes.Black,
                Margin = new Thickness(0, 0, 0, 10)
            };

            headerPanel.Children.Add(companyTitle);
            headerPanel.Children.Add(reportTitle);
            headerPanel.Children.Add(separator);

            return headerPanel;
        }

        /// <summary>
        /// Creates print footer
        /// </summary>
        private UIElement CreateFooter()
        {
            var footerPanel = new Grid
            {
                Width = 10 * 96 // Landscape width minus margins
            };

            footerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Print date
            var printDate = new TextBlock
            {
                Text = $"Printed: {DateTime.Now:dd/MM/yyyy HH:mm}",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Page number
            var pageNumber = new TextBlock
            {
                Text = "Page 1 of 1",
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(printDate, 0);
            Grid.SetColumn(pageNumber, 1);

            footerPanel.Children.Add(printDate);
            footerPanel.Children.Add(pageNumber);

            return footerPanel;
        }
    }

    /// <summary>
    /// Print Preview Window
    /// </summary>
    public partial class PrintPreviewWindow : Window
    {
        private readonly FixedDocument _document;
        private readonly PrintDialog _printDialog;

        public PrintPreviewWindow(FixedDocument document, PrintDialog printDialog, string title)
        {
            _document = document;
            _printDialog = printDialog;

            InitializeComponent(title);
        }

        private void InitializeComponent(string title)
        {
            Title = $"Print Preview - {title}";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Toolbar
            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(248, 249, 250))
            };

            var printButton = new Button
            {
                Content = "🖨️ Print",
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 123, 255)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold
            };
            printButton.Click += PrintButton_Click;

            var closeButton = new Button
            {
                Content = "❌ Close",
                Padding = new Thickness(15, 8, 15, 8),
                Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold
            };
            closeButton.Click += CloseButton_Click;

            toolbar.Children.Add(printButton);
            toolbar.Children.Add(closeButton);

            // Document viewer
            var viewer = new DocumentViewer
            {
                Document = _document,
                Margin = new Thickness(10)
            };

            Grid.SetRow(toolbar, 0);
            Grid.SetRow(viewer, 1);

            grid.Children.Add(toolbar);
            grid.Children.Add(viewer);

            Content = grid;
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_printDialog.ShowDialog() == true)
                {
                    _printDialog.PrintDocument(_document.DocumentPaginator, "Report");
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print error: {ex.Message}", "Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}