using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DOInventoryManager.Utils
{
    /// <summary>
    /// Simple helper to enable pixel-perfect scrolling without complex animations
    /// </summary>
    public static class PixelScrollingHelper
    {
        /// <summary>
        /// Enables pixel-perfect scrolling for a DataGrid (no animations, just smooth pixel scrolling)
        /// </summary>
        public static void EnablePixelScrolling(DataGrid dataGrid)
        {
            if (dataGrid == null) return;

            // Configure for pixel scrolling
            dataGrid.EnableRowVirtualization = true;
            dataGrid.EnableColumnVirtualization = true;
            VirtualizingPanel.SetVirtualizationMode(dataGrid, VirtualizationMode.Recycling);
            VirtualizingPanel.SetScrollUnit(dataGrid, ScrollUnit.Pixel);

            // The key setting that makes scrolling smooth - disable content scrolling
            if (dataGrid.IsLoaded)
            {
                ConfigureScrollViewer(dataGrid);
            }
            else
            {
                dataGrid.Loaded += (s, e) => ConfigureScrollViewer(dataGrid);
            }
        }

        /// <summary>
        /// Enables pixel scrolling for all DataGrids in a container
        /// </summary>
        public static void EnablePixelScrollingForContainer(DependencyObject container)
        {
            if (container == null) return;

            // Process current element
            if (container is DataGrid dataGrid)
            {
                EnablePixelScrolling(dataGrid);
            }

            // Process children
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
            {
                var child = VisualTreeHelper.GetChild(container, i);
                EnablePixelScrollingForContainer(child);
            }
        }

        private static void ConfigureScrollViewer(DataGrid dataGrid)
        {
            var scrollViewer = FindScrollViewer(dataGrid);
            if (scrollViewer != null)
            {
                // This is the key setting for smooth pixel scrolling
                scrollViewer.CanContentScroll = false;
            }
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is ScrollViewer scrollViewer)
                    return scrollViewer;
                
                var foundScrollViewer = FindScrollViewer(child);
                if (foundScrollViewer != null)
                    return foundScrollViewer;
            }
            return null;
        }
    }
}