using DOInventoryManager.Utils;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DOInventoryManager.Services
{
    /// <summary>
    /// Service to automatically enable smooth scrolling across the application
    /// without breaking theme styling
    /// </summary>
    public static class SmoothScrollingService
    {
        /// <summary>
        /// Enables smooth scrolling for all DataGrids and ScrollViewers in a container
        /// </summary>
        public static void EnableSmoothScrolling(DependencyObject container)
        {
            if (container == null) return;

            // Process current element
            if (container is DataGrid dataGrid)
            {
                dataGrid.EnableSmoothScrolling();
            }
            else if (container is ScrollViewer scrollViewer)
            {
                scrollViewer.EnableSmoothScrolling(200);
            }

            // Process children
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
            {
                var child = VisualTreeHelper.GetChild(container, i);
                EnableSmoothScrolling(child);
            }
        }

        /// <summary>
        /// Automatically enables smooth scrolling for a UserControl when loaded
        /// </summary>
        public static void AutoEnableSmoothScrolling(UserControl userControl)
        {
            if (userControl.IsLoaded)
            {
                EnableSmoothScrolling(userControl);
            }
            else
            {
                userControl.Loaded += (s, e) => EnableSmoothScrolling(userControl);
            }
        }

        /// <summary>
        /// Enables smooth scrolling for a specific DataGrid immediately
        /// </summary>
        public static void EnableDataGridSmoothScrolling(DataGrid dataGrid)
        {
            dataGrid?.EnableSmoothScrolling();
        }

        /// <summary>
        /// Enables smooth scrolling for a specific ScrollViewer immediately
        /// </summary>
        public static void EnableScrollViewerSmoothScrolling(ScrollViewer scrollViewer)
        {
            scrollViewer?.EnableSmoothScrolling(200);
        }
    }
}