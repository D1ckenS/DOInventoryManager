using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;

namespace DOInventoryManager.Utils
{
    /// <summary>
    /// Provides smooth scrolling behavior for ScrollViewer controls
    /// </summary>
    public static class SmoothScrollBehavior
    {
        #region Attached Properties

        /// <summary>
        /// Gets or sets whether smooth scrolling is enabled for a ScrollViewer
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>
        /// Gets or sets the smooth scroll duration in milliseconds
        /// </summary>
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached("Duration", typeof(int), typeof(SmoothScrollBehavior),
                new PropertyMetadata(300));

        /// <summary>
        /// Gets or sets the smooth scroll easing function
        /// </summary>
        public static readonly DependencyProperty EasingProperty =
            DependencyProperty.RegisterAttached("Easing", typeof(IEasingFunction), typeof(SmoothScrollBehavior),
                new PropertyMetadata(new CubicEase { EasingMode = EasingMode.EaseOut }));

        #endregion

        #region Property Accessors

        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        public static int GetDuration(DependencyObject obj)
        {
            return (int)obj.GetValue(DurationProperty);
        }

        public static void SetDuration(DependencyObject obj, int value)
        {
            obj.SetValue(DurationProperty, value);
        }

        public static IEasingFunction GetEasing(DependencyObject obj)
        {
            return (IEasingFunction)obj.GetValue(EasingProperty);
        }

        public static void SetEasing(DependencyObject obj, IEasingFunction value)
        {
            obj.SetValue(EasingProperty, value);
        }

        #endregion

        #region Event Handlers

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                if ((bool)e.NewValue)
                {
                    scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
                    scrollViewer.Loaded += ScrollViewer_Loaded;
                }
                else
                {
                    scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
                    scrollViewer.Loaded -= ScrollViewer_Loaded;
                }
            }
        }

        private static void ScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Find and configure any nested DataGrids for smooth scrolling
                ConfigureDataGridSmoothing(scrollViewer);
            }
        }

        private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer && !e.Handled)
            {
                // Calculate scroll amount (default WPF uses 48 pixels per wheel delta unit)
                double scrollAmount = SystemParameters.WheelScrollLines * 16; // Reduced from default for smoother feel
                
                if (e.Delta < 0)
                    scrollAmount = -scrollAmount;

                // Get current position and calculate target
                double currentOffset = scrollViewer.VerticalOffset;
                double targetOffset = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, currentOffset - scrollAmount));

                // Only animate if there's a meaningful change
                if (Math.Abs(targetOffset - currentOffset) > 1)
                {
                    AnimateScroll(scrollViewer, targetOffset);
                    e.Handled = true;
                }
            }
        }

        #endregion

        #region Animation Methods

        private static void AnimateScroll(ScrollViewer scrollViewer, double targetOffset)
        {
            try
            {
                var duration = GetDuration(scrollViewer);
                var easing = GetEasing(scrollViewer);

                // Create smooth scroll animation
                var animation = new DoubleAnimation
                {
                    From = scrollViewer.VerticalOffset,
                    To = targetOffset,
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = easing,
                    FillBehavior = FillBehavior.Stop
                };

                // Use a custom animation timeline to control ScrollViewer offset
                var storyboard = new Storyboard();
                storyboard.Children.Add(animation);

                // Create a dummy DependencyObject to animate
                var animationTarget = new DummyAnimationTarget();
                Storyboard.SetTarget(animation, animationTarget);
                Storyboard.SetTargetProperty(animation, new PropertyPath(DummyAnimationTarget.ValueProperty));

                // Handle animation value changes
                EventHandler timeHandler = null;
                timeHandler = (s, e) =>
                {
                    var currentValue = animationTarget.Value;
                    scrollViewer.ScrollToVerticalOffset(currentValue);
                };
                
                animation.CurrentTimeInvalidated += timeHandler;

                // Start animation
                storyboard.Begin();
            }
            catch
            {
                // Fallback to instant scroll if animation fails
                scrollViewer.ScrollToVerticalOffset(targetOffset);
            }
        }

        private static void ConfigureDataGridSmoothing(DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is DataGrid dataGrid)
                {
                    // Enable smooth scrolling for DataGrid
                    dataGrid.EnableRowVirtualization = true;
                    dataGrid.EnableColumnVirtualization = true;
                    VirtualizingPanel.SetVirtualizationMode(dataGrid, VirtualizationMode.Recycling);
                    VirtualizingPanel.SetScrollUnit(dataGrid, ScrollUnit.Pixel);
                }
                
                ConfigureDataGridSmoothing(child);
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Dummy class to provide animation target for scroll offset
        /// </summary>
        private class DummyAnimationTarget : DependencyObject
        {
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register("Value", typeof(double), typeof(DummyAnimationTarget));

            public double Value
            {
                get => (double)GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for easy smooth scrolling setup
    /// </summary>
    public static class SmoothScrollExtensions
    {
        /// <summary>
        /// Enables smooth scrolling with default settings
        /// </summary>
        public static void EnableSmoothScrolling(this ScrollViewer scrollViewer)
        {
            SmoothScrollBehavior.SetIsEnabled(scrollViewer, true);
        }

        /// <summary>
        /// Enables smooth scrolling with custom duration
        /// </summary>
        public static void EnableSmoothScrolling(this ScrollViewer scrollViewer, int durationMs)
        {
            SmoothScrollBehavior.SetIsEnabled(scrollViewer, true);
            SmoothScrollBehavior.SetDuration(scrollViewer, durationMs);
        }

        /// <summary>
        /// Enables smooth scrolling with custom duration and easing
        /// </summary>
        public static void EnableSmoothScrolling(this ScrollViewer scrollViewer, int durationMs, IEasingFunction easing)
        {
            SmoothScrollBehavior.SetIsEnabled(scrollViewer, true);
            SmoothScrollBehavior.SetDuration(scrollViewer, durationMs);
            SmoothScrollBehavior.SetEasing(scrollViewer, easing);
        }

        /// <summary>
        /// Disables smooth scrolling
        /// </summary>
        public static void DisableSmoothScrolling(this ScrollViewer scrollViewer)
        {
            SmoothScrollBehavior.SetIsEnabled(scrollViewer, false);
        }
    }
}