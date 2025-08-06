using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DOInventoryManager.Utils
{
    /// <summary>
    /// Provides global smooth scrolling by intercepting mouse wheel events
    /// without modifying any DataGrid properties or functionality
    /// </summary>
    public static class GlobalSmoothScrolling
    {
        private static bool _isEnabled = false;

        /// <summary>
        /// Enables smooth scrolling application-wide
        /// </summary>
        public static void Enable()
        {
            if (_isEnabled) return;
            
            // Hook into the application's global mouse wheel events
            EventManager.RegisterClassHandler(typeof(UIElement), UIElement.PreviewMouseWheelEvent, 
                new MouseWheelEventHandler(OnGlobalPreviewMouseWheel), true);
            
            _isEnabled = true;
        }

        /// <summary>
        /// Disables smooth scrolling application-wide
        /// </summary>
        public static void Disable()
        {
            if (!_isEnabled) return;
            
            // Note: EventManager doesn't provide easy unregistration, 
            // but we can set a flag to disable the behavior
            _isEnabled = false;
        }

        private static void OnGlobalPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_isEnabled) return;

            // Find the ScrollViewer that should handle this scroll
            var scrollViewer = FindScrollViewer(e.OriginalSource as DependencyObject);
            if (scrollViewer == null) return;

            // Only handle if the ScrollViewer can actually scroll
            if (scrollViewer.ScrollableHeight <= 0) return;

            // Calculate smooth scroll amount (much smaller than default)
            double scrollAmount = e.Delta > 0 ? -20 : 20; // Reduced from default ~48px
            
            // Get target position
            double currentOffset = scrollViewer.VerticalOffset;
            double targetOffset = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, currentOffset + scrollAmount));
            
            // Only animate if there's a meaningful change
            if (Math.Abs(targetOffset - currentOffset) > 1)
            {
                // Create simple, fast animation
                var animation = new DoubleAnimation
                {
                    From = currentOffset,
                    To = targetOffset,
                    Duration = TimeSpan.FromMilliseconds(120), // Very fast, subtle animation
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                // Apply animation to ScrollViewer
                scrollViewer.BeginAnimation(ScrollViewerBehavior.VerticalOffsetProperty, animation);
                
                // Mark event as handled to prevent default chunky scrolling
                e.Handled = true;
            }
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is ScrollViewer scrollViewer)
                    return scrollViewer;
                    
                element = System.Windows.Media.VisualTreeHelper.GetParent(element);
            }
            return null;
        }
    }

    /// <summary>
    /// Attached behavior to enable ScrollViewer animation
    /// </summary>
    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBehavior),
                new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        public static double GetVerticalOffset(ScrollViewer scrollViewer)
        {
            return (double)scrollViewer.GetValue(VerticalOffsetProperty);
        }

        public static void SetVerticalOffset(ScrollViewer scrollViewer, double value)
        {
            scrollViewer.SetValue(VerticalOffsetProperty, value);
        }

        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
}