using System;
using System.Windows;

namespace DearNotes.Services
{
    public static class DeskGeometry
    {
        public const double CollapsedWidth = 40;

        public const double ExpandedWidth = 320;

        /// <summary>
        /// Folga vertical entre o Desk e as bordas da área útil.
        /// </summary>
        public const double ScreenMargin = 8;

        /// <summary>
        /// Folga horizontal até a borda da tela. É zero para que as cartas
        /// pareçam sair de trás da borda, e não de uma linha antes dela.
        /// </summary>
        public const double SideMargin = 0;

        public const double FanCardWidth = 296;

        public const double FanCardHeight = 176;

        public const double FanStep = 88;

        public const double FanTabWidth = 34;

        /// <summary>
        /// Largura visível de cada carta em repouso: a aba e uma borda do corpo.
        /// </summary>
        public const double FanTabPeek = 40;

        public const double AddButtonSize = 26;

        public const double AddButtonGap = 12;

        public const double AddButtonReserve = 52;

        public const double HandleEdgeGap = 6;

        public static Rect ComputeSnapBounds(
            Rect workArea,
            DpiScale dpi,
            double logicalWidth,
            double logicalHeight,
            bool isOnRight,
            double? verticalPosition,
            double currentTopPixels)
        {
            double marginX = Math.Round(SideMargin * dpi.DpiScaleX);
            double marginY = Math.Round(ScreenMargin * dpi.DpiScaleY);

            double maxWidth = Math.Max(
                1,
                workArea.Width - marginX * 2);

            double widthPixels = Math.Max(
                1,
                Math.Min(
                    Math.Round(logicalWidth * dpi.DpiScaleX),
                    maxWidth));

            double heightPixels = Math.Max(
                1,
                Math.Min(
                    Math.Round(logicalHeight * dpi.DpiScaleY),
                    workArea.Height - marginY * 2));

            double availableHeight = Math.Max(
                0,
                workArea.Height - heightPixels - marginY * 2);

            double minTop = workArea.Top + marginY;
            double maxTop = Math.Max(
                workArea.Top,
                workArea.Bottom - heightPixels - marginY);

            double top = verticalPosition.HasValue
                ? minTop + availableHeight *
                  Math.Clamp(verticalPosition.Value, 0, 1)
                : Math.Clamp(
                    currentTopPixels,
                    minTop,
                    minTop + availableHeight);

            top = Math.Clamp(
                Math.Round(top),
                Math.Min(minTop, maxTop),
                maxTop);

            double left = isOnRight
                ? workArea.Right - widthPixels - marginX
                : workArea.Left + marginX;

            left = Math.Clamp(
                left,
                workArea.Left,
                Math.Max(workArea.Left, workArea.Right - widthPixels));

            return new Rect(left, top, widthPixels, heightPixels);
        }

        public static double ComputeFanStep(
            int count,
            double cardHeight,
            double availableHeight,
            double preferredStep)
        {
            if (count < 2) return 0;

            double maxStep = Math.Max(
                0,
                (availableHeight - cardHeight) / (count - 1));

            return Math.Clamp(preferredStep, 0, maxStep);
        }

        public static double ComputeFanTop(
            int count,
            double cardHeight,
            double availableHeight,
            double preferredStep)
        {
            if (count <= 0) return 0;

            double step = ComputeFanStep(
                count,
                cardHeight,
                availableHeight,
                preferredStep);

            double stackHeight = cardHeight + (count - 1) * step;

            return Math.Max(0, (availableHeight - stackHeight) / 2);
        }

        /// <summary>
        /// Posição horizontal da carta em repouso: só a aba fica dentro do
        /// painel, e o restante do corpo continua escondido além da borda.
        /// </summary>
        public static double ComputeFanCardLeft(
            bool isOnRight,
            double panelWidth)
        {
            return isOnRight
                ? panelWidth - FanTabPeek
                : FanTabPeek - FanCardWidth;
        }

        /// <summary>
        /// Deslocamento que tira a carta escolhida do baralho e a mostra
        /// inteira, em direção ao centro da tela.
        /// </summary>
        public static double ComputeFanSlideOffset(bool isOnRight)
        {
            double distance = FanCardWidth - FanTabPeek;

            return isOnRight ? -distance : distance;
        }

        /// <summary>
        /// Distância entre a borda do painel e o centro das abas, usada para
        /// alinhar o botão de nova nota com o baralho.
        /// </summary>
        public static double ComputeTabCenterInset()
        {
            return FanTabPeek - FanTabWidth / 2;
        }

        /// <summary>
        /// Escolhe a carta a partir de uma posição fixa do ponteiro, e não da
        /// carta sob ele: as cartas se movem e trocariam de dono a cada quadro.
        /// Na coluna das abas, cada carta responde pela própria faixa; fora
        /// dela, a carta já aberta continua escolhida enquanto contém o ponteiro.
        /// </summary>
        /// <returns>O índice da carta escolhida ou -1.</returns>
        public static int ComputeChosenCardIndex(
            Point pointerPosition,
            int count,
            double availableHeight,
            bool isOnRight,
            double panelWidth,
            int currentIndex)
        {
            if (count <= 0) return -1;

            double step = ComputeFanStep(
                count,
                FanCardHeight,
                availableHeight,
                FanStep);

            double top = ComputeFanTop(
                count,
                FanCardHeight,
                availableHeight,
                FanStep);

            if (IsInTabColumn(pointerPosition.X, isOnRight, panelWidth))
            {
                return ComputeTabBandIndex(pointerPosition.Y, count, top, step);
            }

            if (currentIndex < 0 || currentIndex >= count) return -1;

            var revealedCard = new Rect(
                ComputeFanCardLeft(isOnRight, panelWidth) +
                    ComputeFanSlideOffset(isOnRight),
                top + currentIndex * step,
                FanCardWidth,
                FanCardHeight);

            return revealedCard.Contains(pointerPosition) ? currentIndex : -1;
        }

        private static bool IsInTabColumn(
            double x,
            bool isOnRight,
            double panelWidth)
        {
            return isOnRight
                ? x >= panelWidth - FanTabPeek && x <= panelWidth
                : x >= 0 && x <= FanTabPeek;
        }

        private static int ComputeTabBandIndex(
            double y,
            int count,
            double top,
            double step)
        {
            double offset = y - top;
            double stackHeight = FanCardHeight + (count - 1) * step;

            if (offset < 0 || offset >= stackHeight) return -1;

            // Sem passo, as cartas se empilham e só a última aparece.
            if (step <= 0) return count - 1;

            return Math.Min(count - 1, (int)(offset / step));
        }

        public static double ComputeAddButtonTop(
            int count,
            double cardHeight,
            double availableHeight,
            double preferredStep)
        {
            if (count <= 0)
            {
                return Math.Max(0, (availableHeight - AddButtonSize) / 2);
            }

            double step = ComputeFanStep(
                count,
                cardHeight,
                availableHeight,
                preferredStep);

            double top = ComputeFanTop(
                count,
                cardHeight,
                availableHeight,
                preferredStep);

            return top + cardHeight + (count - 1) * step + AddButtonGap;
        }

        public static double ComputeNormalizedTop(
            Rect workArea,
            Rect windowBounds,
            DpiScale dpi,
            double fallback)
        {
            double marginY = Math.Round(ScreenMargin * dpi.DpiScaleY);

            double availableHeight = Math.Max(
                0,
                workArea.Height - windowBounds.Height - marginY * 2);

            if (availableHeight <= 0)
            {
                return Math.Clamp(fallback, 0, 1);
            }

            double normalizedTop =
                (windowBounds.Top - workArea.Top - marginY) /
                availableHeight;

            return Math.Clamp(normalizedTop, 0, 1);
        }

        public static bool IsOnRightSide(
            Rect workArea,
            double mouseX)
        {
            double screenCenter = workArea.Left + workArea.Width / 2;

            return mouseX >= screenCenter;
        }

        public static Rect ComputeEditorBounds(
            Rect workArea,
            Size windowSize,
            DpiScale dpi,
            double marginInDips)
        {
            double marginX = marginInDips * dpi.DpiScaleX;
            double marginY = marginInDips * dpi.DpiScaleY;

            double widthPixels = Math.Max(
                1,
                Math.Min(
                    windowSize.Width * dpi.DpiScaleX,
                    workArea.Width - marginX * 2));

            double heightPixels = Math.Max(
                1,
                Math.Min(
                    windowSize.Height * dpi.DpiScaleY,
                    workArea.Height - marginY * 2));

            return new Rect(
                workArea.Left + (workArea.Width - widthPixels) / 2,
                workArea.Top + (workArea.Height - heightPixels) / 2,
                widthPixels,
                heightPixels);
        }

        public static Point ComputeDragPosition(
            Point mousePosition,
            Rect windowBounds,
            Point dragAnchor)
        {
            return new Point(
                mousePosition.X - windowBounds.Width * dragAnchor.X,
                mousePosition.Y - windowBounds.Height * dragAnchor.Y);
        }
    }
}
