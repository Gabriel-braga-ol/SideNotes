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
    /// Conduz a abertura e o recolhimento do leque, o escalonamento das
    /// cartas, as transições do baralho e o atraso de recolhimento.
    /// </summary>
    internal sealed class DeskFanAnimator
    {
        private const int ExpandStaggerMilliseconds = 45;

        private const int CollapseStaggerMilliseconds = 30;

        private const int CardExpandMilliseconds = 300;

        private const int CardCollapseMilliseconds = 180;

        private const int HandleFadeMilliseconds = 140;

        private const int ProximityDelayMilliseconds = 160;

        private const int CollapseDelayMilliseconds = 450;

        private const double HiddenOffset = 42;

        private const double HiddenRise = 16;

        private readonly DeskViewModel viewModel;

        private readonly FrameworkElement deskHandle;

        private readonly ItemsControl fanCards;

        private readonly Action<double> setDeskWidth;

        private readonly Func<bool> isMouseOver;

        private readonly Func<bool> isDragging;

        private readonly DispatcherTimer proximityTimer =
            new()
            {
                Interval = TimeSpan.FromMilliseconds(ProximityDelayMilliseconds)
            };

        private readonly DispatcherTimer collapseTimer =
            new()
            {
                Interval = TimeSpan.FromMilliseconds(CollapseDelayMilliseconds)
            };

        private int animationVersion;

        public DeskFanAnimator(
            DeskViewModel viewModel,
            FrameworkElement deskHandle,
            ItemsControl fanCards,
            Action<double> setDeskWidth,
            Func<bool> isMouseOver,
            Func<bool> isDragging)
        {
            this.viewModel = viewModel;
            this.deskHandle = deskHandle;
            this.fanCards = fanCards;
            this.setDeskWidth = setDeskWidth;
            this.isMouseOver = isMouseOver;
            this.isDragging = isDragging;

            proximityTimer.Tick += ProximityTimer_Tick;
            collapseTimer.Tick += CollapseTimer_Tick;
        }

        public void HandleMouseEnter()
        {
            proximityTimer.Stop();
            collapseTimer.Stop();

            if (viewModel.IsExpanded || isDragging()) return;

            if (viewModel.DeskNotes.IsEmpty) return;

            proximityTimer.Start();
        }

        public void HandleMouseLeave()
        {
            proximityTimer.Stop();

            if (isDragging()) return;

            if (!viewModel.IsExpanded &&
                fanCards.Visibility == Visibility.Collapsed)
            {
                return;
            }

            collapseTimer.Stop();
            collapseTimer.Start();
        }

        public void StopCollapseTimer()
        {
            proximityTimer.Stop();
            collapseTimer.Stop();
        }

        public void ApplyExpandedState()
        {
            proximityTimer.Stop();

            if (isDragging())
            {
                CollapseImmediately();
                return;
            }

            if (!viewModel.IsExpanded &&
                fanCards.Visibility == Visibility.Collapsed)
            {
                CollapseImmediately();
                return;
            }

            AnimateFan(viewModel.IsExpanded);
        }

        public void CollapseImmediately()
        {
            ++animationVersion;

            StopCollapseTimer();

            ResetCardAnimations();

            fanCards.BeginAnimation(UIElement.OpacityProperty, null);
            fanCards.Visibility = Visibility.Collapsed;
            fanCards.IsHitTestVisible = false;

            deskHandle.BeginAnimation(UIElement.OpacityProperty, null);
            deskHandle.Opacity = 1;
            deskHandle.Visibility = Visibility.Visible;
            deskHandle.IsHitTestVisible = true;

            setDeskWidth(DeskGeometry.CollapsedWidth);
        }

        public void Detach()
        {
            proximityTimer.Stop();
            collapseTimer.Stop();

            proximityTimer.Tick -= ProximityTimer_Tick;
            collapseTimer.Tick -= CollapseTimer_Tick;
        }

        private void ProximityTimer_Tick(object? sender, EventArgs e)
        {
            proximityTimer.Stop();

            if (!isMouseOver() || isDragging()) return;

            viewModel.SetExpanded(true);
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
            int version = ++animationVersion;

            if (expanded)
            {
                Expand(version);
            }
            else
            {
                Collapse(version);
            }
        }

        private void Expand(int version)
        {
            int count = fanCards.Items.Count;

            if (count == 0)
            {
                CollapseImmediately();
                return;
            }

            AnimateHandleOpacity(0, HandleFadeMilliseconds, () =>
            {
                if (version == animationVersion)
                {
                    deskHandle.Visibility = Visibility.Collapsed;
                }
            });

            deskHandle.IsHitTestVisible = false;

            setDeskWidth(DeskGeometry.ExpandedWidth);

            fanCards.Visibility = Visibility.Visible;
            fanCards.IsHitTestVisible = true;
            fanCards.UpdateLayout();

            double direction = viewModel.IsOnRight ? 1 : -1;

            for (int i = 0; i < count; i++)
            {
                if (GetCard(i) is not FrameworkElement card) continue;

                StageCard(
                    card,
                    fromX: direction * HiddenOffset,
                    fromY: HiddenRise,
                    fromOpacity: 0,
                    toX: 0,
                    toY: 0,
                    toOpacity: 1,
                    milliseconds: CardExpandMilliseconds,
                    delayMilliseconds: i * ExpandStaggerMilliseconds);
            }
        }

        private void Collapse(int version)
        {
            int count = fanCards.Items.Count;

            deskHandle.Visibility = Visibility.Visible;
            deskHandle.IsHitTestVisible = false;

            AnimateHandleOpacity(1, HandleFadeMilliseconds);

            fanCards.IsHitTestVisible = false;

            double direction = viewModel.IsOnRight ? 1 : -1;

            bool staged = false;

            for (int i = 0; i < count; i++)
            {
                if (GetCard(i) is not FrameworkElement card) continue;

                staged = true;

                // A primeira carta é a última a terminar; ela fecha o recolhimento.
                Action? completed = i == 0
                    ? () => FinishCollapse(version)
                    : null;

                StageCard(
                    card,
                    fromX: 0,
                    fromY: 0,
                    fromOpacity: 1,
                    toX: direction * HiddenOffset,
                    toY: HiddenRise / 2,
                    toOpacity: 0,
                    milliseconds: CardCollapseMilliseconds,
                    delayMilliseconds:
                        (count - 1 - i) * CollapseStaggerMilliseconds,
                    completed: completed);
            }

            if (!staged)
            {
                FinishCollapse(version);
            }
        }

        private void FinishCollapse(int version)
        {
            if (version != animationVersion || viewModel.IsExpanded) return;

            ResetCardAnimations();

            fanCards.BeginAnimation(UIElement.OpacityProperty, null);
            fanCards.Visibility = Visibility.Collapsed;

            deskHandle.Opacity = 1;
            deskHandle.Visibility = Visibility.Visible;
            deskHandle.IsHitTestVisible = true;

            setDeskWidth(DeskGeometry.CollapsedWidth);
        }

        private FrameworkElement? GetCard(int index)
        {
            return fanCards.ItemContainerGenerator
                .ContainerFromIndex(index) as FrameworkElement;
        }

        private static void StageCard(
            FrameworkElement card,
            double fromX,
            double fromY,
            double fromOpacity,
            double toX,
            double toY,
            double toOpacity,
            int milliseconds,
            int delayMilliseconds,
            Action? completed = null)
        {
            var translate = GetTranslate(card);

            card.BeginAnimation(UIElement.OpacityProperty, null);
            translate.BeginAnimation(TranslateTransform.XProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);

            card.Opacity = fromOpacity;
            translate.X = fromX;
            translate.Y = fromY;

            var easing = new CubicEase
            {
                EasingMode = EasingMode.EaseOut
            };

            var opacityAnimation = new DoubleAnimation
            {
                From = fromOpacity,
                To = toOpacity,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                EasingFunction = easing
            };

            if (completed is not null)
            {
                opacityAnimation.Completed += (_, _) => completed();
            }

            card.BeginAnimation(
                UIElement.OpacityProperty,
                opacityAnimation,
                HandoffBehavior.SnapshotAndReplace);

            AnimateTransform(
                translate,
                TranslateTransform.XProperty,
                toX,
                milliseconds,
                delayMilliseconds,
                easing);

            AnimateTransform(
                translate,
                TranslateTransform.YProperty,
                toY,
                milliseconds,
                delayMilliseconds,
                easing);
        }

        private static void AnimateTransform(
            TranslateTransform transform,
            DependencyProperty property,
            double destination,
            int milliseconds,
            int delayMilliseconds,
            IEasingFunction easing)
        {
            var animation = new DoubleAnimation
            {
                From = (double)transform.GetValue(property),
                To = destination,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                EasingFunction = easing
            };

            transform.BeginAnimation(
                property,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private void AnimateHandleOpacity(
            double destination,
            int milliseconds,
            Action? completed = null)
        {
            deskHandle.BeginAnimation(UIElement.OpacityProperty, null);

            double current = deskHandle.Opacity;

            deskHandle.Opacity = destination;

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

            deskHandle.BeginAnimation(
                UIElement.OpacityProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private static TranslateTransform GetTranslate(
            FrameworkElement card)
        {
            if (card.RenderTransform is TranslateTransform current)
            {
                return current;
            }

            var translate = new TranslateTransform();

            card.RenderTransform = translate;

            return translate;
        }

        private void ResetCardAnimations()
        {
            int count = fanCards.Items.Count;

            for (int i = 0; i < count; i++)
            {
                if (GetCard(i) is not FrameworkElement card) continue;

                card.BeginAnimation(UIElement.OpacityProperty, null);
                card.Opacity = 1;

                var translate = GetTranslate(card);

                translate.BeginAnimation(
                    TranslateTransform.XProperty,
                    null);

                translate.BeginAnimation(
                    TranslateTransform.YProperty,
                    null);

                translate.X = 0;
                translate.Y = 0;
            }
        }
    }
}
