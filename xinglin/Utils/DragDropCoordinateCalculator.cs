using System.Windows;

namespace xinglin.Utils
{
    /// <summary>
    /// 拖拽坐标计算辅助类。
    /// 将 TemplateEditorView 中的坐标计算逻辑提取为纯函数，便于单元测试。
    /// </summary>
    public static class DragDropCoordinateCalculator
    {
        public const double MM_TO_PIXEL = 96.0 / 25.4;

        /// <summary>
        /// 计算从工具箱 Drop 到画布后控件的最终位置（当前有 Bug 的版本）。
        /// Bug 1.6：直接用毫米单位的 control.Width/Height 与像素单位的 canvasWidth/Height 比较。
        /// </summary>
        public static Point CalculateDropPosition_Buggy(
            double dropX, double dropY,
            double controlWidthMm, double controlHeightMm,
            double canvasWidthPx, double canvasHeightPx,
            double zoomScale)
        {
            // 模拟当前代码：先除以 scale（这里 Drop 中也有此操作）
            double left = dropX / zoomScale;
            double top = dropY / zoomScale;

            if (left < 0) left = 0;
            if (top < 0) top = 0;

            // Bug：control.Width 是毫米，canvasWidthPx 是像素，单位不同
            if (left + controlWidthMm > canvasWidthPx)
                left = canvasWidthPx - controlWidthMm;
            if (top + controlHeightMm > canvasHeightPx)
                top = canvasHeightPx - controlHeightMm;

            return new Point(left, top);
        }

        /// <summary>
        /// 计算从工具箱 Drop 到画布后控件的最终位置（修复后的版本）。
        /// Fix 1.6：将毫米转换为像素后再做边界限制。
        /// </summary>
        public static Point CalculateDropPosition_Fixed(
            double dropX, double dropY,
            double controlWidthMm, double controlHeightMm,
            double canvasWidthPx, double canvasHeightPx,
            double zoomScale)
        {
            double left = dropX / zoomScale;
            double top = dropY / zoomScale;

            double controlWidthPx = controlWidthMm * MM_TO_PIXEL;
            double controlHeightPx = controlHeightMm * MM_TO_PIXEL;

            if (left < 0) left = 0;
            if (top < 0) top = 0;
            if (left + controlWidthPx > canvasWidthPx)
                left = canvasWidthPx - controlWidthPx;
            if (top + controlHeightPx > canvasHeightPx)
                top = canvasHeightPx - controlHeightPx;

            return new Point(left, top);
        }

        /// <summary>
        /// 计算画布内拖动控件时的新位置（当前有 Bug 的版本）。
        /// Bug 1.7：对 GetPosition 返回的 Canvas 本地坐标再次除以 scale。
        /// </summary>
        public static Point CalculateMouseMovePosition_Buggy(
            double canvasMouseX, double canvasMouseY,
            double dragOffsetX, double dragOffsetY,
            double zoomScale)
        {
            // 模拟当前代码：对 Canvas 本地坐标再次除以 scale（重复缩放）
            double adjustedX = canvasMouseX / zoomScale;
            double adjustedY = canvasMouseY / zoomScale;

            double newX = adjustedX - dragOffsetX;
            double newY = adjustedY - dragOffsetY;

            return new Point(newX, newY);
        }

        /// <summary>
        /// 计算画布内拖动控件时的新位置（修复后的版本）。
        /// Fix 1.7：直接使用 Canvas 本地坐标，不再除以 scale。
        /// </summary>
        public static Point CalculateMouseMovePosition_Fixed(
            double canvasMouseX, double canvasMouseY,
            double dragOffsetX, double dragOffsetY,
            double zoomScale)
        {
            // 修复：直接使用 Canvas 本地坐标
            double newX = canvasMouseX - dragOffsetX;
            double newY = canvasMouseY - dragOffsetY;

            return new Point(newX, newY);
        }

        /// <summary>
        /// 计算 MouseDown 时记录的拖拽起始点（当前有 Bug 的版本）。
        /// Bug 1.7：对 Canvas 本地坐标再次除以 scale。
        /// </summary>
        public static Point CalculateDragStartPoint_Buggy(
            double canvasMouseX, double canvasMouseY,
            double zoomScale)
        {
            return new Point(canvasMouseX / zoomScale, canvasMouseY / zoomScale);
        }

        /// <summary>
        /// 计算 MouseDown 时记录的拖拽起始点（修复后的版本）。
        /// Fix 1.7：直接使用 Canvas 本地坐标。
        /// </summary>
        public static Point CalculateDragStartPoint_Fixed(
            double canvasMouseX, double canvasMouseY,
            double zoomScale)
        {
            return new Point(canvasMouseX, canvasMouseY);
        }
    }
}
