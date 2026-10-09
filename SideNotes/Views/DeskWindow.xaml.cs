using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SideNotes.ViewModels;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Collections.Specialized;
using SideNotes.Models;

namespace SideNotes.Views
{
    public partial class DeskWindow : Window
    {
        private const double CollapsedWidth = 24;
        private const double ExpandedWidth = 256;
        private const double EditorWidth = 576;
        private const double ScreenMargin = 8;

        private readonly MainViewModel viewModel;

        private bool isOnRight = true;
        private bool isExpanded;
        private bool isDragging;
        private int fanAnimationVersion;
        private int editorAnimationVersion;
        private Note? openedNote;

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
            viewModel.DeskNotes.CollectionChanged += DeskNotes_CollectionChanged;
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
            if (openedNote is not null)
            {
                expanded = true;
            }

            if (expanded)
            {
                PillHandle.Opacity = 0;
                PillHandle.IsHitTestVisible = false;

                SetDeskWidth(
                    openedNote is not null
                        ? EditorWidth
                        : ExpandedWidth);
            }

            if (isExpanded == expanded)
            {
                return;
            }

            isExpanded = expanded;

            int version = ++fanAnimationVersion;

            var transform =
                (TranslateTransform)NoteTabs.RenderTransform;

            double hiddenOffset = isOnRight ? 36 : -36;

            if (expanded)
            {
                if (NoteTabs.Visibility != Visibility.Visible)
                {
                    NoteTabs.Opacity = 0;
                    transform.X = hiddenOffset;
                    NoteTabs.Visibility = Visibility.Visible;
                }

                NoteTabs.IsHitTestVisible = true;
                DeskRoot.Background = Brushes.Transparent;

                Animate(
                    transform,
                    TranslateTransform.XProperty,
                    0,
                    280);

                Animate(
                    NoteTabs,
                    UIElement.OpacityProperty,
                    1,
                    180);
            }
            else
            {
                NoteTabs.IsHitTestVisible = false;

                Animate(
                    transform,
                    TranslateTransform.XProperty,
                    hiddenOffset,
                    200);

                Animate(
                    NoteTabs,
                    UIElement.OpacityProperty,
                    0,
                    200,
                    () =>
                    {
                        if (version != fanAnimationVersion ||
                            isExpanded)
                        {
                            return;
                        }

                        NoteTabs.Visibility = Visibility.Collapsed;
                        DeskRoot.Background = null;

                        SetDeskWidth(CollapsedWidth);

                        PillHandle.Opacity = 1;
                        PillHandle.IsHitTestVisible = true;
                    });
            }
        }

        private void Pill_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;        

            e.Handled = true;
            collapseTimer.Stop();
            isDragging = true;

            CloseDeskNote(immediate: true);

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

            NoteEditor.HorizontalAlignment = isOnRight
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            NoteTabs.Margin = new Thickness(0);

            NoteEditor.Margin = isOnRight
                ? new Thickness(0, 8, ExpandedWidth, 8)
                : new Thickness(ExpandedWidth, 8, 0, 8);
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
            ++fanAnimationVersion;
            ++editorAnimationVersion;

            collapseTimer.Stop();
            collapseTimer.Tick -= CollapseTimer_Tick;

            viewModel.DeskNotes.CollectionChanged -= DeskNotes_CollectionChanged;

            base.OnClosed(e);
        }

        private void NoteTab_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element ||
                element.DataContext is not Note note) return;   

            e.Handled = true;
            collapseTimer.Stop();

            ++editorAnimationVersion;

            bool wasVisible =
                NoteEditor.Visibility == Visibility.Visible;

            openedNote = note;
            NoteEditor.DataContext = note;
            NoteEditor.IsHitTestVisible = true;

            UpdateSideLayout();
            SetExpanded(true);

            var transform =
                (TranslateTransform)NoteEditor.RenderTransform;

            if (!wasVisible)
            {
                NoteEditor.Opacity = 0;
                transform.X = isOnRight ? 24 : -24;
            }

            NoteEditor.Visibility = Visibility.Visible;

            Animate(
                transform,
                TranslateTransform.XProperty,
                0,
                280);

            Animate(
                NoteEditor,
                UIElement.OpacityProperty,
                1,
                180);
        }

        private void CloseNote_Click(object sender, RoutedEventArgs e)
        {
            CloseDeskNote();
        }
        private void CloseDeskNote( bool immediate = false)
        {
            int version = ++editorAnimationVersion;

            if (NoteEditor.IsKeyboardFocusWithin)
            {
                Keyboard.ClearFocus();
            }

            NoteEditor.IsHitTestVisible = false;

            if (immediate)
            {
                openedNote = null;
                NoteEditor.Visibility = Visibility.Collapsed;
                NoteEditor.DataContext = null;

                NoteEditor.BeginAnimation(
                    UIElement.OpacityProperty, null);

                var transform =
                    (TranslateTransform)NoteEditor.RenderTransform;

                transform.BeginAnimation(
                    TranslateTransform.XProperty, null);

                CollapseImmediately();
                return;
            }

            if (openedNote is null)
            {
                SetExpanded(false);
                return;
            }

            var editorTransform =
                (TranslateTransform)NoteEditor.RenderTransform;

            Animate(
                editorTransform,
                TranslateTransform.XProperty,
                isOnRight ? 24 : -24,
                180);

            Animate(
                NoteEditor,
                UIElement.OpacityProperty,
                0,
                180,
                () =>
                {
                    if (version != editorAnimationVersion) return;
                    
                    openedNote = null;
                    NoteEditor.Visibility = Visibility.Collapsed;
                    NoteEditor.DataContext = null;
                    
                    SetExpanded(false);
                });
        }

        private void CollapseImmediately()
        {
            ++fanAnimationVersion;

            isExpanded = false;

            NoteTabs.BeginAnimation(
                UIElement.OpacityProperty, null);

            var transform =
                (TranslateTransform)NoteTabs.RenderTransform;

            transform.BeginAnimation(
                TranslateTransform.XProperty, null);

            NoteTabs.Opacity = 0;
            NoteTabs.IsHitTestVisible = false;
            NoteTabs.Visibility = Visibility.Collapsed;

            DeskRoot.Background = null;

            SetDeskWidth(CollapsedWidth);

            PillHandle.Opacity = 1;
            PillHandle.IsHitTestVisible = true;
        }

        private void DeskNotes_CollectionChanged(
            object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(PositionNoteCards));

            if (openedNote is not null &&
                (!openedNote.IsPinnedToDesk ||
                 !viewModel.Notes.Contains(openedNote)))
            {
                CloseDeskNote();
            }
            else if (viewModel.DeskNotes.IsEmpty)
            {
                SetExpanded(false);
            }
        }

        private static void Animate(
            DependencyObject target,
            DependencyProperty property,
            double destination,
            int milliseconds,
            Action? completed = null)
        {
            double current = (double)target.GetValue(property);

            target.SetValue(property, destination);

            var animation = new DoubleAnimation
            {
                From = current,
                To = destination,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                FillBehavior = FillBehavior.Stop,
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            if (completed is not null)
            {
                animation.Completed += (_, _) => completed();
            }

            if (target is UIElement element)
            {
                element.BeginAnimation(
                    property,
                    animation,
                    HandoffBehavior.SnapshotAndReplace);
            }
            else if (target is Animatable animatable)
            {
                animatable.BeginAnimation(
                    property,
                    animation,
                    HandoffBehavior.SnapshotAndReplace);
            }
        }

        private void SetDeskWidth(double width)
        {
            double rightEdge = Left + Width;

            Width = width;

            if (isOnRight)
            {
                Left = rightEdge - width;
            }
        }

        private void NoteContainer_Loaded(object sender, RoutedEventArgs e)
        {
            PositionNoteCards();
        }

        private void PositionNoteCards()
        {
            const double spacing = 54;
            const double cardHeight = 160;
            const double padding = 12;

            int count = NoteTabs.Items.Count;

            NoteTabs.Height =
                Math.Max(0, count - 1) * spacing
                + cardHeight
                + padding * 2;

            for (int i = 0; i < count; i++)
            {
                var container =
                    NoteTabs.ItemContainerGenerator
                        .ContainerFromIndex(i) as ContentPresenter;

                if (container is null) continue;

                Canvas.SetLeft(container, 4);
                Canvas.SetTop(container, padding + i * spacing);
            }
        }
    }
}