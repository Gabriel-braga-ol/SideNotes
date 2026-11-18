using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DearNotes.Services;
using DearNotes.ViewModels;

namespace DearNotes.Views
{
    /// <summary>
    /// Conduz a abertura e o recolhimento do leque, as animações das abas,
    /// a largura da janela e o atraso de recolhimento ao sair do Desk.
    /// </summary>
    internal sealed class DeskFanAnimator
    {
        private const double HiddenOffset = 36;

        private const int ExpandedTranslateMilliseconds = 280;

        private const int ExpandedOpacityMilliseconds = 180;

        private const int CollapsedMilliseconds = 200;

        private const int CollapseDelayMilliseconds = 450;

        private readonly DeskViewModel viewModel;

        private readonly Panel deskRoot;

        private readonly FrameworkElement pillHandle;

        private readonly ItemsControl noteTabs;

        private readonly Action<double> setDeskWidth;

        private readonly Func<bool> isMouseOver;

        private readonly Func<bool> isDragging;

        private readonly DispatcherTimer collapseTimer =
            new()
            {
                Interval = TimeSpan.FromMilliseconds(CollapseDelayMilliseconds)
            };

        private int animationVersion;

        public DeskFanAnimator(
            DeskViewModel viewModel,
            Panel deskRoot,
            FrameworkElement pillHandle,
            ItemsControl noteTabs,
            Action<double> setDeskWidth,
            Func<bool> isMouseOver,
            Func<bool> isDragging)
        {
            this.viewModel = viewModel;
            this.deskRoot = deskRoot;
            this.pillHandle = pillHandle;
            this.noteTabs = noteTabs;
            this.setDeskWidth = setDeskWidth;
            this.isMouseOver = isMouseOver;
            this.isDragging = isDragging;

            collapseTimer.Tick += CollapseTimer_Tick;
        }

        public void HandleMouseEnter()
        {
            collapseTimer.Stop();
        }

        public void HandleMouseLeave()
        {
            if (isDragging()) return;

            collapseTimer.Stop();
            collapseTimer.Start();
        }

        public void StopCollapseTimer()
        {
            collapseTimer.Stop();
        }

        public void ApplyExpandedState()
        {
            if (isDragging() ||
                (!viewModel.IsExpanded &&
                 noteTabs.Visibility == Visibility.Collapsed))
            {
                CollapseImmediately();
            }
            else
            {
                AnimateFan(viewModel.IsExpanded);
            }
        }

        public void CollapseImmediately()
        {
            ++animationVersion;

            noteTabs.BeginAnimation(
                UIElement.OpacityProperty,
                null);

            var transform =
                (TranslateTransform)noteTabs.RenderTransform;

            transform.BeginAnimation(
                TranslateTransform.XProperty,
                null);

            noteTabs.Opacity = 0;
            noteTabs.IsHitTestVisible = false;
            noteTabs.Visibility = Visibility.Collapsed;

            deskRoot.Background = null;

            setDeskWidth(DeskGeometry.CollapsedWidth);

            pillHandle.Opacity = 1;
            pillHandle.IsHitTestVisible = true;
        }

        public void Detach()
        {
            collapseTimer.Stop();
            collapseTimer.Tick -= CollapseTimer_Tick;
        }

        private void CollapseTimer_Tick(object? sender, EventArgs e)
        {
            collapseTimer.Stop();

            if (!isMouseOver() && !isDragging())
            {
                viewModel.SetExpanded(false);
            }
        }

        private void AnimateFan(bool expanded)
        {
            if (expanded)
            {
                pillHandle.Opacity = 0;
                pillHandle.IsHitTestVisible = false;

                setDeskWidth(DeskGeometry.ExpandedWidth);
            }

            int version = ++animationVersion;

            var transform =
                (TranslateTransform)noteTabs.RenderTransform;

            double hiddenOffset = viewModel.IsOnRight
                ? HiddenOffset
                : -HiddenOffset;

            if (expanded)
            {
                if (noteTabs.Visibility != Visibility.Visible)
                {
                    noteTabs.Opacity = 0;
                    transform.X = hiddenOffset;
                    noteTabs.Visibility = Visibility.Visible;
                }

                noteTabs.IsHitTestVisible = true;
                deskRoot.Background = Brushes.Transparent;

                Animate(
                    transform,
                    TranslateTransform.XProperty,
                    0,
                    ExpandedTranslateMilliseconds);

                Animate(
                    noteTabs,
                    UIElement.OpacityProperty,
                    1,
                    ExpandedOpacityMilliseconds);
            }
            else
            {
                noteTabs.IsHitTestVisible = false;

                Animate(
                    transform,
                    TranslateTransform.XProperty,
                    hiddenOffset,
                    CollapsedMilliseconds);

                Animate(
                    noteTabs,
                    UIElement.OpacityProperty,
                    0,
                    CollapsedMilliseconds,
                    () =>
                    {
                        if (version != animationVersion ||
                            viewModel.IsExpanded) return;

                        noteTabs.Visibility = Visibility.Collapsed;
                        deskRoot.Background = null;

                        setDeskWidth(DeskGeometry.CollapsedWidth);

                        pillHandle.Opacity = 1;
                        pillHandle.IsHitTestVisible = true;
                    });
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
    }
}
