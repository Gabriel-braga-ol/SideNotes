using System;
using System.Collections.Generic;
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
    /// cartas, o deslize da carta escolhida, as transições do baralho e do
    /// botão de nova nota e o atraso de recolhimento.
    /// </summary>
    internal sealed class DeskFanAnimator
    {
        private const string NoteCardName = "NoteCard";

        private const int ExpandStaggerMilliseconds = 45;

        private const int CollapseStaggerMilliseconds = 30;

        private const int CardExpandMilliseconds = 320;

        private const int CardCollapseMilliseconds = 180;

        private const int CardSlideOutMilliseconds = 300;

        private const int CardSlideBackMilliseconds = 220;

        private const int HandleFadeMilliseconds = 140;

        private const int AddButtonFadeMilliseconds = 200;

        private const int ProximityDelayMilliseconds = 90;

        private const int CollapseDelayMilliseconds = 450;

        /// <summary>
        /// Distância extra além da aba para que a carta comece totalmente
        /// escondida atrás da borda da tela.
        /// </summary>
        private const double HiddenOvershoot = 12;

        private const double TileOvershootAmplitude = 0.35;

        private const double SlideOvershootAmplitude = 0.2;

        private const int RestingZIndex = 0;

        private readonly DeskViewModel viewModel;

        private readonly FrameworkElement deskHandle;

        private readonly ItemsControl fanCards;

        private readonly FrameworkElement addNoteButton;

        private readonly Action<double> setDeskWidth;

        private readonly Func<bool> isPointerInsideDesk;

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

        // Enquanto as telhas entram, elas passam sob o ponteiro parado; o
        // deslize só vale depois que todas assentam.
        private bool isFanSettling;

        // Cada carta que sai do baralho fica acima da anterior, inclusive
        // da que ainda está voltando ao lugar.
        private int slideZIndex = RestingZIndex;

        public DeskFanAnimator(
            DeskViewModel viewModel,
            FrameworkElement deskHandle,
            ItemsControl fanCards,
            FrameworkElement addNoteButton,
            Action<double> setDeskWidth,
            Func<bool> isPointerInsideDesk,
            Func<bool> isDragging)
        {
            this.viewModel = viewModel;
            this.deskHandle = deskHandle;
            this.fanCards = fanCards;
            this.addNoteButton = addNoteButton;
            this.setDeskWidth = setDeskWidth;
            this.isPointerInsideDesk = isPointerInsideDesk;
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

            SyncAddButton();

            setDeskWidth(DeskGeometry.CollapsedWidth);
        }

        /// <summary>
        /// Mostra o botão de nova nota com o leque aberto ou quando ainda não
        /// há notas fixadas, já que nesse caso não há baralho para abrir.
        /// </summary>
        public void SyncAddButton()
        {
            bool visible = viewModel.IsExpanded || viewModel.DeskNotes.IsEmpty;

            addNoteButton.BeginAnimation(UIElement.OpacityProperty, null);
            addNoteButton.Opacity = 1;
            addNoteButton.Visibility = visible
                ? Visibility.Visible
                : Visibility.Collapsed;

            ResetTranslate(GetTranslate(addNoteButton));
        }

        /// <summary>
        /// Tira a carta do baralho enquanto ela está sob o ponteiro ou com o
        /// foco do teclado, e a devolve ao lugar quando deixa de estar.
        /// </summary>
        public void UpdateCardSlide(FrameworkElement noteCard)
        {
            if (!viewModel.IsExpanded || isFanSettling || isDragging()) return;

            bool chosen = noteCard.IsMouseOver || noteCard.IsKeyboardFocused;

            SlideCard(noteCard, chosen);
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

            if (!isPointerInsideDesk() || isDragging()) return;

            viewModel.SetExpanded(true);
        }

        private void CollapseTimer_Tick(object? sender, EventArgs e)
        {
            collapseTimer.Stop();

            if (!isPointerInsideDesk() && !isDragging())
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
            if (fanCards.Items.Count == 0)
            {
                CollapseImmediately();
                return;
            }

            AnimateOpacity(deskHandle, 0, HandleFadeMilliseconds, 0, () =>
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
            // Os contêineres só existem depois que o leque fica visível.
            fanCards.UpdateLayout();

            List<FrameworkElement> cards = GetCards();

            double hiddenOffset = GetHiddenOffset();

            var tileEasing = new BackEase
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = TileOvershootAmplitude
            };

            isFanSettling = true;

            // As telhas entram de cima para baixo, uma após a outra.
            for (int i = 0; i < cards.Count; i++)
            {
                ResetCardSlide(cards[i]);

                // A última telha é a última a assentar; ela libera o deslize.
                Action? completed = i == cards.Count - 1
                    ? () => FinishExpand(version)
                    : null;

                StageElement(
                    cards[i],
                    fromX: hiddenOffset,
                    fromOpacity: 0,
                    toX: 0,
                    toOpacity: 1,
                    milliseconds: CardExpandMilliseconds,
                    delayMilliseconds: i * ExpandStaggerMilliseconds,
                    movementEasing: tileEasing,
                    completed: completed);
            }

            if (cards.Count == 0) isFanSettling = false;

            ShowAddButton(cards.Count * ExpandStaggerMilliseconds);
        }

        private void FinishExpand(int version)
        {
            if (version != animationVersion) return;

            isFanSettling = false;

            foreach (FrameworkElement card in GetCards())
            {
                if (FindNoteCard(card) is FrameworkElement noteCard &&
                    (noteCard.IsMouseOver || noteCard.IsKeyboardFocused))
                {
                    SlideCard(noteCard, chosen: true);
                }
            }
        }

        private void ShowAddButton(int delayMilliseconds)
        {
            addNoteButton.Visibility = Visibility.Visible;

            StageElement(
                addNoteButton,
                fromX: GetHiddenOffset() / 2,
                fromOpacity: 0,
                toX: 0,
                toOpacity: 1,
                milliseconds: AddButtonFadeMilliseconds,
                delayMilliseconds: delayMilliseconds,
                movementEasing: new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                });
        }

        private void Collapse(int version)
        {
            deskHandle.Visibility = Visibility.Visible;
            deskHandle.IsHitTestVisible = false;

            AnimateOpacity(deskHandle, 1, HandleFadeMilliseconds, 0);

            fanCards.IsHitTestVisible = false;

            if (!viewModel.DeskNotes.IsEmpty)
            {
                AnimateOpacity(addNoteButton, 0, HandleFadeMilliseconds, 0);
            }

            List<FrameworkElement> cards = GetCards();

            if (cards.Count == 0)
            {
                FinishCollapse(version);
                return;
            }

            double hiddenOffset = GetHiddenOffset();

            var easing = new CubicEase
            {
                EasingMode = EasingMode.EaseIn
            };

            for (int i = 0; i < cards.Count; i++)
            {
                // A primeira carta é a última a terminar; ela fecha o recolhimento.
                Action? completed = i == 0
                    ? () => FinishCollapse(version)
                    : null;

                if (FindNoteCard(cards[i]) is FrameworkElement noteCard)
                {
                    SlideCard(noteCard, chosen: false);
                }

                StageElement(
                    cards[i],
                    fromX: GetTranslate(cards[i]).X,
                    fromOpacity: cards[i].Opacity,
                    toX: hiddenOffset,
                    toOpacity: 0,
                    milliseconds: CardCollapseMilliseconds,
                    delayMilliseconds:
                        (cards.Count - 1 - i) * CollapseStaggerMilliseconds,
                    movementEasing: easing,
                    completed: completed);
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

            SyncAddButton();

            setDeskWidth(DeskGeometry.CollapsedWidth);
        }

        private void SlideCard(FrameworkElement noteCard, bool chosen)
        {
            if (ItemsControl.ContainerFromElement(fanCards, noteCard)
                    is not UIElement container)
            {
                return;
            }

            TranslateTransform translate = GetTranslate(noteCard);

            double destination = chosen
                ? DeskGeometry.ComputeFanSlideOffset(viewModel.IsOnRight)
                : 0;

            DoubleAnimation animation = CreateSlideAnimation(
                translate.X,
                destination,
                chosen);

            if (chosen)
            {
                Panel.SetZIndex(container, ++slideZIndex);
            }
            else
            {
                animation.Completed += (_, _) =>
                    LowerCardWhenReleased(noteCard, container);
            }

            translate.BeginAnimation(
                TranslateTransform.XProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private static DoubleAnimation CreateSlideAnimation(
            double from,
            double to,
            bool chosen)
        {
            // A saída passa um pouco do ponto e assenta, como uma carta puxada;
            // a volta é direta para não esbarrar nas telhas vizinhas.
            IEasingFunction easing = chosen
                ? new BackEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Amplitude = SlideOvershootAmplitude
                }
                : new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                };

            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(chosen
                    ? CardSlideOutMilliseconds
                    : CardSlideBackMilliseconds),
                EasingFunction = easing
            };
        }

        private static void LowerCardWhenReleased(
            FrameworkElement noteCard,
            UIElement container)
        {
            if (noteCard.IsMouseOver || noteCard.IsKeyboardFocused) return;

            Panel.SetZIndex(container, RestingZIndex);
        }

        private double GetHiddenOffset()
        {
            double direction = viewModel.IsOnRight ? 1 : -1;

            return direction * (DeskGeometry.FanTabPeek + HiddenOvershoot);
        }

        private List<FrameworkElement> GetCards()
        {
            var cards = new List<FrameworkElement>(fanCards.Items.Count);

            for (int i = 0; i < fanCards.Items.Count; i++)
            {
                if (GetCard(i) is FrameworkElement card)
                {
                    cards.Add(card);
                }
            }

            return cards;
        }

        private FrameworkElement? GetCard(int index)
        {
            return fanCards.ItemContainerGenerator
                .ContainerFromIndex(index) as FrameworkElement;
        }

        private static FrameworkElement? FindNoteCard(FrameworkElement container)
        {
            if (container is not ContentPresenter presenter) return null;

            return presenter.ContentTemplate?
                .FindName(NoteCardName, presenter) as FrameworkElement;
        }

        private static void StageElement(
            FrameworkElement element,
            double fromX,
            double fromOpacity,
            double toX,
            double toOpacity,
            int milliseconds,
            int delayMilliseconds,
            IEasingFunction movementEasing,
            Action? completed = null)
        {
            TranslateTransform translate = GetTranslate(element);

            element.BeginAnimation(UIElement.OpacityProperty, null);
            translate.BeginAnimation(TranslateTransform.XProperty, null);

            element.Opacity = fromOpacity;
            translate.X = fromX;

            var opacityAnimation = new DoubleAnimation
            {
                From = fromOpacity,
                To = toOpacity,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            if (completed is not null)
            {
                opacityAnimation.Completed += (_, _) => completed();
            }

            element.BeginAnimation(
                UIElement.OpacityProperty,
                opacityAnimation,
                HandoffBehavior.SnapshotAndReplace);

            var movementAnimation = new DoubleAnimation
            {
                From = fromX,
                To = toX,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
                EasingFunction = movementEasing
            };

            translate.BeginAnimation(
                TranslateTransform.XProperty,
                movementAnimation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private static void AnimateOpacity(
            UIElement element,
            double destination,
            int milliseconds,
            int delayMilliseconds,
            Action? completed = null)
        {
            double current = element.Opacity;

            element.BeginAnimation(UIElement.OpacityProperty, null);
            element.Opacity = destination;

            var animation = new DoubleAnimation
            {
                From = current,
                To = destination,
                Duration = TimeSpan.FromMilliseconds(milliseconds),
                BeginTime = TimeSpan.FromMilliseconds(delayMilliseconds),
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

            element.BeginAnimation(
                UIElement.OpacityProperty,
                animation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private static TranslateTransform GetTranslate(
            FrameworkElement element)
        {
            if (element.RenderTransform is TranslateTransform current &&
                !current.IsFrozen)
            {
                return current;
            }

            var translate = new TranslateTransform();

            element.RenderTransform = translate;

            return translate;
        }

        private static void ResetTranslate(TranslateTransform translate)
        {
            translate.BeginAnimation(TranslateTransform.XProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);

            translate.X = 0;
            translate.Y = 0;
        }

        private static void ResetCardSlide(FrameworkElement container)
        {
            Panel.SetZIndex(container, RestingZIndex);

            if (FindNoteCard(container) is FrameworkElement noteCard)
            {
                ResetTranslate(GetTranslate(noteCard));
            }
        }

        private void ResetCardAnimations()
        {
            isFanSettling = false;
            slideZIndex = RestingZIndex;

            foreach (FrameworkElement card in GetCards())
            {
                card.BeginAnimation(UIElement.OpacityProperty, null);
                card.Opacity = 1;

                ResetTranslate(GetTranslate(card));
                ResetCardSlide(card);
            }
        }
    }
}
