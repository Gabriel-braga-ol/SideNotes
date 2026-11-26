using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DearNotes.Services;
using DearNotes.ViewModels;

namespace DearNotes.Views
{
    public partial class NoteEditorWindow : Window
    {
        private const double EditorMargin = 12;

        private const double EntranceSlide = 90;

        private const double EntranceScale = 0.96;

        private const int EntranceMilliseconds = 280;

        private readonly string monitorDeviceName;

        private readonly IScreenService screenService;

        private readonly bool deskIsOnRight;

        private bool initialPositionApplied;

        public NoteEditorWindow(
            DeskViewModel viewModel,
            DeskScreen screen,
            IScreenService screenService,
            bool deskIsOnRight)
        {
            InitializeComponent();

            DataContext = viewModel;
            monitorDeviceName = screen.DeviceName;
            this.screenService = screenService;
            this.deskIsOnRight = deskIsOnRight;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Coloca a janela no monitor correto antes de exibi-la.
            DeskScreen screen =
                screenService.FindOrPrimary(monitorDeviceName);

            DpiScale dpi = screen.Dpi;
            Rect bounds = screenService.GetWindowBounds(this);

            Rect target = DeskGeometry.ComputeEditorBounds(
                screen.WorkArea,
                new Size(bounds.Width, bounds.Height),
                dpi,
                marginInDips: 0);

            screenService.TryMoveWindow(this, target.Left, target.Top);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (initialPositionApplied) return;

            initialPositionApplied = true;

            DeskScreen screen =
                screenService.FindOrPrimary(monitorDeviceName);

            DpiScale dpi = screen.Dpi;

            Rect target = DeskGeometry.ComputeEditorBounds(
                screen.WorkArea,
                new Size(Width, Height),
                dpi,
                EditorMargin);

            screenService.TrySetWindowBounds(
                this,
                target.Left,
                target.Top,
                target.Width,
                target.Height);

            PlayEntranceAnimation();

            ContentBox.Focus();
        }

        private void Window_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key != Key.Escape) return;

            e.Handled = true;
            Close();
        }

        private void Header_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            e.Handled = true;
            DragMove();
        }

        private void PlayEntranceAnimation()
        {
            // A carta sai do baralho na direção do Desk e desliza até o centro.
            double direction = deskIsOnRight ? 1 : -1;

            Opacity = 1;

            EditorCard.Opacity = 1;
            EditorSlide.X = 0;
            EditorScale.ScaleX = 1;
            EditorScale.ScaleY = 1;

            var easing = new CubicEase
            {
                EasingMode = EasingMode.EaseOut
            };

            var duration = TimeSpan.FromMilliseconds(
                EntranceMilliseconds);

            Animate(
                EditorCard,
                UIElement.OpacityProperty,
                from: 0,
                to: 1,
                duration,
                easing);

            Animate(
                EditorSlide,
                TranslateTransform.XProperty,
                from: direction * EntranceSlide,
                to: 0,
                duration,
                easing);

            Animate(
                EditorScale,
                ScaleTransform.ScaleXProperty,
                from: EntranceScale,
                to: 1,
                duration,
                easing);

            Animate(
                EditorScale,
                ScaleTransform.ScaleYProperty,
                from: EntranceScale,
                to: 1,
                duration,
                easing);
        }

        private static void Animate(
            DependencyObject target,
            DependencyProperty property,
            double from,
            double to,
            TimeSpan duration,
            IEasingFunction easing)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                FillBehavior = FillBehavior.Stop,
                EasingFunction = easing
            };

            if (target is UIElement element)
            {
                element.BeginAnimation(property, animation);
            }
            else if (target is Animatable animatable)
            {
                animatable.BeginAnimation(property, animation);
            }
        }
    }
}
