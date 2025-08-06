using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DOInventoryManager.Controls
{
    /// <summary>
    /// Custom DataGrid with built-in smooth pixel scrolling
    /// </summary>
    public class SmoothDataGrid : DataGrid
    {
        static SmoothDataGrid()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SmoothDataGrid), 
                new FrameworkPropertyMetadata(typeof(SmoothDataGrid)));
        }

        public SmoothDataGrid()
        {
            // Configure for optimal smooth scrolling
            EnableRowVirtualization = true;
            EnableColumnVirtualization = true;
            VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
            VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
            
            // Essential for smooth scrolling
            VirtualizingPanel.SetIsVirtualizing(this, true);
            VirtualizingPanel.SetIsContainerVirtualizable(this, true);
            
            Loaded += OnSmoothDataGridLoaded;
        }

        private void OnSmoothDataGridLoaded(object sender, RoutedEventArgs e)
        {
            // Find and configure the internal ScrollViewer for pixel-perfect scrolling
            var scrollViewer = GetTemplateChild("DG_ScrollViewer") as ScrollViewer;
            if (scrollViewer != null)
            {
                // Key setting for smooth pixel scrolling
                scrollViewer.CanContentScroll = false;
                
                // Ensure scrollbars are styled correctly
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                
                // Enable smooth mouse wheel scrolling
                scrollViewer.PanningMode = PanningMode.VerticalOnly;
            }
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            // Let the base DataGrid handle the mouse wheel - it will be smooth due to pixel scrolling
            base.OnPreviewMouseWheel(e);
        }
    }
}