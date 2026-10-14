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
using SideNotes.Services;
using System.ComponentModel;

namespace SideNotes.Views
{
    public partial class DeskWindow : Window
    {
        private const double CollapsedWidth = 24;

        private const double ExpandedWidth = 256;

        private const double EditorWidth = 576;

        private const double ScreenMargin = 8;

        private readonly DeskViewModel viewModel;

        private readonly DeskPositionStore positionStore = new();

        private bool isDragging;

        private int fanAnimationVersion;

        private int editorAnimationVersion;

        private Point dragStartMouse;

        private Point dragStartWindow;

        private readonly DispatcherTimer collapseTimer =
            new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(450)
            };


        public DeskWindow(DeskViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            DataContext = viewModel;

            collapseTimer.Tick += CollapseTimer_Tick;
            viewModel.DeskNotes.CollectionChanged += DeskNotes_CollectionChanged;

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Rect workArea = SystemParameters.WorkArea;

            Height = Math.Min(
                Height,
                Math.Max(1, workArea.Height - ScreenMargin * 2));

            DeskPosition position = positionStore.Load();

            viewModel.SetSide(position.IsOnRight);

            double availableHeight = Math.Max(
                0,
                workArea.Height - Height - ScreenMargin * 2);

            Top = workArea.Top + ScreenMargin + availableHeight * position.VerticalPosition;

            UpdateSideLayout();
            SnapToEdge();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            collapseTimer.Stop();     
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

            if (!IsMouseOver && !isDragging) viewModel.SetExpanded(false);
        }

        private void AnimateFan(bool expanded)
        {
            if (expanded)
            {
                PillHandle.Opacity = 0;
                PillHandle.IsHitTestVisible = false;

                SetDeskWidth(
                    viewModel.OpenedNote is not null
                        ? EditorWidth
                        : ExpandedWidth);
            }

            int version = ++fanAnimationVersion;

            var transform =
                (TranslateTransform)NoteTabs.RenderTransform;

            double hiddenOffset = viewModel.IsOnRight ? 36 : -36;

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
                            viewModel.IsExpanded) return;

                        NoteTabs.Visibility = Visibility.Collapsed;
                        DeskRoot.Background = null;

                        SetDeskWidth(CollapsedWidth);

                        PillHandle.Opacity = 1;
                        PillHandle.IsHitTestVisible = true;
                    });
            }
        }

        private void Pill_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (isDragging) return;    

            collapseTimer.Stop();
            viewModel.SetExpanded(true);
        }

        private void UpdateSideLayout()
        {
            PillHandle.HorizontalAlignment = viewModel.IsOnRight
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;

            NoteTabs.HorizontalAlignment = viewModel.IsOnRight
                ? HorizontalAlignment.Right
                : HorizontalAlignment.Left;

            NoteEditor.HorizontalAlignment = viewModel.IsOnRight
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            NoteTabs.Margin = new Thickness(0);

            NoteEditor.Margin = viewModel.IsOnRight
                ? new Thickness(0, 8, ExpandedWidth, 8)
                : new Thickness(ExpandedWidth, 8, 0, 8);
        }

        private void SnapToEdge()
        {
            Rect workArea = SystemParameters.WorkArea;

            Left = viewModel.IsOnRight
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
            viewModel.PropertyChanged -= ViewModel_PropertyChanged;

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

            viewModel.OpenNote(note);
        }

        private void CloseNote_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CloseNote();
        }

        private void ShowEditor()
        {
            ++editorAnimationVersion;

            bool wasVisible =
                NoteEditor.Visibility == Visibility.Visible;

            NoteEditor.IsHitTestVisible = true;

            UpdateSideLayout();
            SetDeskWidth(EditorWidth);

            var transform =
                (TranslateTransform)NoteEditor.RenderTransform;

            if (!wasVisible)
            {
                NoteEditor.Opacity = 0;
                transform.X = viewModel.IsOnRight ? 24 : -24;
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

        private void AnimateEditorClose()
        {
            int version = ++editorAnimationVersion;

            if (NoteEditor.IsKeyboardFocusWithin) Keyboard.ClearFocus();

            NoteEditor.IsHitTestVisible = false;

            var transform =
                (TranslateTransform)NoteEditor.RenderTransform;

            Animate(
                transform,
                TranslateTransform.XProperty,
                viewModel.IsOnRight ? 24 : -24,
                180);

            Animate(
                NoteEditor,
                UIElement.OpacityProperty,
                0,
                180,
                () =>
                {
                    if (version != editorAnimationVersion ||
                        !viewModel.IsClosing) return;

                    viewModel.CompleteCloseNote();
                });
        }

        private void HideEditorImmediately()
        {
            ++editorAnimationVersion;

            if (NoteEditor.IsKeyboardFocusWithin) Keyboard.ClearFocus();

            NoteEditor.IsHitTestVisible = false;
            NoteEditor.Visibility = Visibility.Collapsed;

            NoteEditor.BeginAnimation(
                UIElement.OpacityProperty, null);

            var transform =
                (TranslateTransform)NoteEditor.RenderTransform;

            transform.BeginAnimation(
                TranslateTransform.XProperty, null);
        }

        private void CollapseImmediately()
        {
            ++fanAnimationVersion;

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

            if (viewModel.IsOnRight)
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

        private void SaveDeskPosition()
        {
            Rect workArea = SystemParameters.WorkArea;

            double availableHeight = Math.Max(0, workArea.Height - Height - ScreenMargin * 2);

            double verticalPosition = availableHeight > 0
                ? (Top - workArea.Top - ScreenMargin) / availableHeight
                : 0.5;

            var position = new DeskPosition
            {
                IsOnRight = viewModel.IsOnRight,
                VerticalPosition = Math.Clamp(verticalPosition, 0, 1)
            };

            positionStore.Save(position);
        }


        private void Pill_MouseRightButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (isDragging) return;

            collapseTimer.Stop();
            isDragging = true;

            viewModel.CloseNote(immediate: true);

            HideEditorImmediately();
            CollapseImmediately();

            dragStartMouse = PointToScreen(e.GetPosition(this));

            dragStartWindow = new Point(Left, Top);

            if (!PillHandle.CaptureMouse())
            {
                isDragging = false;
            }
        }

        private void Pill_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!isDragging) return;

            if (e.RightButton != MouseButtonState.Pressed)
            {
                FinishPillDrag();
                return;
            }

            Point currentMouse = PointToScreen(e.GetPosition(this));

            Vector movement = currentMouse - dragStartMouse;

            var source = PresentationSource.FromVisual(this);

            if (source?.CompositionTarget is { } target)
            {
                movement = target.TransformFromDevice.Transform(movement);
            }

            Left = dragStartWindow.X + movement.X;
            Top = dragStartWindow.Y + movement.Y;

            e.Handled = true;
        }

        private void Pill_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging) return;

            e.Handled = true;
            FinishPillDrag();
        }

        private void Pill_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (isDragging && !PillHandle.IsMouseCaptured) FinishPillDrag();
        }

        private void FinishPillDrag()
        {
            if (!isDragging) return;

            isDragging = false;

            if (PillHandle.IsMouseCaptured) PillHandle.ReleaseMouseCapture();

            Rect workArea = SystemParameters.WorkArea;

            double windowCenter = Left + Width / 2;
            double screenCenter = workArea.Left + workArea.Width / 2;

            viewModel.SetSide(windowCenter >= screenCenter);

            UpdateSideLayout();
            SnapToEdge();
            SaveDeskPosition();
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DeskViewModel.OpenedNote):
                    if (viewModel.OpenedNote is null)
                    {
                        HideEditorImmediately();
                    }
                    else
                    {
                        ShowEditor();
                    }
                    break;

                case nameof(DeskViewModel.IsClosing):
                    if (viewModel.IsClosing)
                    {
                        AnimateEditorClose();
                    }
                    else if (viewModel.OpenedNote is not null)
                    {
                        // Restaura o editor se o fechamento foi cancelado.
                        ShowEditor();
                    }
                    break;

                case nameof(DeskViewModel.IsExpanded):
                    if (isDragging)
                    {
                        CollapseImmediately();
                    }
                    else
                    {
                        AnimateFan(viewModel.IsExpanded);
                    }
                    break;

                case nameof(DeskViewModel.IsOnRight):
                    UpdateSideLayout();
                    SnapToEdge();
                    break;
            }
        }
    }
}