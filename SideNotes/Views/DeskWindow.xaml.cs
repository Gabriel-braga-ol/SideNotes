using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SideNotes.ViewModels;

namespace SideNotes.Views
{
    public partial class DeskWindow : Window
    {
        private const double CollapsedWidth = 24;
        private const double ExpandedWidth = 96;
        private const double ScreenMargin = 8;

        private readonly MainViewModel viewModel;

        private bool isOnRight = true;
        private bool isExpanded;
        private bool isDragging;

        private readonly DispatcherTimer collapseTimer =
            new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(450)
            };

        public DeskWindow(MainViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            DataContext = viewModel;

            collapseTimer.Tick += CollapseTimer_Tick;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Rect workArea = SystemParameters.WorkArea;

            Height = Math.Min(
                Height,
                workArea.Height - ScreenMargin * 2);

            Top = workArea.Top + (workArea.Height - Height) / 2;

            UpdateSideLayout();
            SnapToEdge();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            collapseTimer.Stop();

            if (!isDragging && !viewModel.DeskNotes.IsEmpty)
            {
                SetExpanded(true);
            }
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!isDragging)
            {
                collapseTimer.Stop();
                collapseTimer.Start();
            }
        }

        private void CollapseTimer_Tick(object? sender, EventArgs e)
        {
            collapseTimer.Stop();

            if (!IsMouseOver && !isDragging)
            {
                SetExpanded(false);
            }
        }

        private void SetExpanded(bool expanded)
        {
            if (isExpanded == expanded) return;

            // Preserva a posição da borda direita ao mudar a largura.
            double rightEdge = Left + Width;

            isExpanded = expanded;
            Width = expanded ? ExpandedWidth : CollapsedWidth;

            if (isOnRight)
            {
                Left = rightEdge - Width;
            }

            NoteTabs.Visibility = expanded
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Mantém o leque aberto durante a passagem entre as abas.
            DeskRoot.Background = expanded
                ? Brushes.Transparent
                : null;
        }

        private void Pill_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;        

            e.Handled = true;
            collapseTimer.Stop();
            isDragging = true;

            SetExpanded(false);

            try
            {
                DragMove();
            }
            finally
            {
                isDragging = false;

                Rect workArea = SystemParameters.WorkArea;
                double windowCenter = Left + Width / 2;
                double screenCenter = workArea.Left + workArea.Width / 2;

                isOnRight = windowCenter >= screenCenter;

                UpdateSideLayout();
                SnapToEdge();
            }
        }

        private void UpdateSideLayout()
        {
            PillHandle.HorizontalAlignment = isOnRight
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;

            NoteTabs.HorizontalAlignment = isOnRight
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;

            NoteTabs.Margin = isOnRight
                ? new Thickness(0, 0, 24, 28)
                : new Thickness(24, 0, 0, 28);
        }

        private void SnapToEdge()
        {
            Rect workArea = SystemParameters.WorkArea;

            Left = isOnRight
                ? workArea.Right - Width - ScreenMargin
                : workArea.Left + ScreenMargin;

            double minTop = workArea.Top + ScreenMargin;
            double maxTop = Math.Max(
                minTop,
                workArea.Bottom - Height - ScreenMargin);

            Top = Math.Clamp(Top, minTop, maxTop);
        }

        protected override void OnClosed(EventArgs e)
        {
            collapseTimer.Stop();
            collapseTimer.Tick -= CollapseTimer_Tick;

            base.OnClosed(e);
        }
    }
}