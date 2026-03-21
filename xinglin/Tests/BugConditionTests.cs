using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using xinglin.Utils;

namespace xinglin.Tests
{
    /// <summary>
    /// Bug Condition 探索测试。
    /// 
    /// 这些测试编码了期望行为（正确行为），在未修复代码上运行时 EXPECTED TO FAIL。
    /// 失败即证明 bug 存在。修复后这些测试应该通过。
    /// 
    /// Validates: Requirements 1.6, 1.7
    /// </summary>
    [TestClass]
    public class BugConditionTests
    {
        private const double MM_TO_PIXEL = 96.0 / 25.4;
        private const double CANVAS_WIDTH_PX = 794.0;   // A4 宽度像素
        private const double CANVAS_HEIGHT_PX = 1123.0; // A4 高度像素

        // =====================================================================
        // Bug 1.6：Drop 边界计算错误（毫米 vs 像素单位不一致）
        // =====================================================================

        /// <summary>
        /// 测试 1：Drop 边界计算错误（Bug 1.6）具体场景。
        /// 
        /// 场景：Width=100mm 的控件，Drop 到 Canvas X=700px（Canvas 宽 794px）。
        /// 
        /// 当前 Bug 代码：
        ///   if (left + control.Width > EditorCanvas.Width)  // 100mm vs 794px，单位不同
        ///       left = EditorCanvas.Width - control.Width;  // 794 - 100 = 694px
        /// 
        /// 结果：left=694px，但控件实际渲染宽度 = 100 * 3.7795 ≈ 378px
        ///       694 + 378 = 1072px > 794px，控件超出画布！
        /// 
        /// 期望行为：resultX + controlWidthPx ≤ canvasWidthPx
        /// 
        /// **Validates: Requirements 1.6**
        /// </summary>
        [TestMethod]
        [Description("Bug 1.6 具体场景：Width=100mm 控件 Drop 到 X=700px，验证边界约束")]
        public void Drop_Width100mm_At700px_ShouldStayWithinCanvas()
        {
            // Arrange
            double controlWidthMm = 100.0;
            double controlHeightMm = 20.0;
            double dropX = 700.0; // Canvas 像素坐标
            double dropY = 100.0;
            double zoomScale = 1.0;

            double controlWidthPx = controlWidthMm * MM_TO_PIXEL; // ≈ 378px

            // Act：使用当前有 Bug 的计算函数
            Point result = DragDropCoordinateCalculator.CalculateDropPosition_Buggy(
                dropX, dropY,
                controlWidthMm, controlHeightMm,
                CANVAS_WIDTH_PX, CANVAS_HEIGHT_PX,
                zoomScale);

            // Assert：期望控件放置后不超出画布右边界
            // 未修复时：result.X = 794 - 100 = 694，694 + 378 = 1072 > 794，断言失败
            Assert.IsTrue(
                result.X + controlWidthPx <= CANVAS_WIDTH_PX,
                $"Bug 1.6 已触发：控件超出画布右边界。" +
                $"resultX={result.X:F1}px, controlWidthPx={controlWidthPx:F1}px, " +
                $"resultX+controlWidthPx={result.X + controlWidthPx:F1}px > canvasWidth={CANVAS_WIDTH_PX}px。" +
                $"根本原因：边界计算使用了毫米值({controlWidthMm}mm)而非像素值({controlWidthPx:F1}px)。");
        }

        /// <summary>
        /// 测试 1b：Drop 边界计算错误（Bug 1.6）高度方向。
        /// 
        /// 场景：Height=50mm 的控件，Drop 到 Canvas Y=1100px（Canvas 高 1123px）。
        /// 
        /// **Validates: Requirements 1.6**
        /// </summary>
        [TestMethod]
        [Description("Bug 1.6 高度方向：Height=50mm 控件 Drop 到 Y=1100px，验证垂直边界约束")]
        public void Drop_Height50mm_At1100px_ShouldStayWithinCanvas()
        {
            // Arrange
            double controlWidthMm = 30.0;
            double controlHeightMm = 50.0;
            double dropX = 100.0;
            double dropY = 1100.0;
            double zoomScale = 1.0;

            double controlHeightPx = controlHeightMm * MM_TO_PIXEL; // ≈ 189px

            // Act
            Point result = DragDropCoordinateCalculator.CalculateDropPosition_Buggy(
                dropX, dropY,
                controlWidthMm, controlHeightMm,
                CANVAS_WIDTH_PX, CANVAS_HEIGHT_PX,
                zoomScale);

            // Assert：期望控件放置后不超出画布下边界
            // 未修复时：result.Y = 1123 - 50 = 1073，1073 + 189 = 1262 > 1123，断言失败
            Assert.IsTrue(
                result.Y + controlHeightPx <= CANVAS_HEIGHT_PX,
                $"Bug 1.6 已触发（高度方向）：控件超出画布下边界。" +
                $"resultY={result.Y:F1}px, controlHeightPx={controlHeightPx:F1}px, " +
                $"resultY+controlHeightPx={result.Y + controlHeightPx:F1}px > canvasHeight={CANVAS_HEIGHT_PX}px。");
        }

        /// <summary>
        /// 测试 1c：Drop 边界计算错误（Bug 1.6）属性测试 - 多种控件尺寸。
        /// 
        /// 对多种 Width（10mm~200mm）和超出边界的 dropX，验证边界约束。
        /// 
        /// **Validates: Requirements 1.6**
        /// </summary>
        [TestMethod]
        [Description("Bug 1.6 属性测试：多种控件尺寸在边界附近 Drop，验证边界约束")]
        public void Drop_VariousWidths_NearRightEdge_ShouldAllStayWithinCanvas()
        {
            double[] widthsMm = { 10.0, 30.0, 50.0, 100.0, 150.0, 200.0 };
            double[] dropXValues = { 700.0, 750.0, 780.0, 800.0, 900.0 };

            bool anyFailed = false;
            var failures = new System.Text.StringBuilder();

            foreach (double widthMm in widthsMm)
            {
                double controlWidthPx = widthMm * MM_TO_PIXEL;

                foreach (double dropX in dropXValues)
                {
                    Point result = DragDropCoordinateCalculator.CalculateDropPosition_Buggy(
                        dropX, 100.0,
                        widthMm, 20.0,
                        CANVAS_WIDTH_PX, CANVAS_HEIGHT_PX,
                        1.0);

                    double rightEdge = result.X + controlWidthPx;
                    if (rightEdge > CANVAS_WIDTH_PX)
                    {
                        anyFailed = true;
                        failures.AppendLine(
                            $"  Width={widthMm}mm, dropX={dropX}px → resultX={result.X:F1}px, " +
                            $"rightEdge={rightEdge:F1}px > {CANVAS_WIDTH_PX}px (超出 {rightEdge - CANVAS_WIDTH_PX:F1}px)");
                    }
                }
            }

            Assert.IsFalse(anyFailed,
                $"Bug 1.6 已触发：以下场景中控件超出画布边界（毫米值被当作像素值使用）：\n{failures}");
        }

        // =====================================================================
        // Bug 1.7：MouseMove 重复缩放（GetPosition 返回值再次除以 scale）
        // =====================================================================

        /// <summary>
        /// 测试 2：MouseMove 重复缩放（Bug 1.7）具体场景。
        /// 
        /// 场景：ZoomLevel=200（scale=2.0），鼠标从 (100,100) 移动到 (200,200)（Canvas 本地坐标）。
        /// 
        /// 当前 Bug 代码：
        ///   Point currentPoint = e.GetPosition(EditorCanvas);  // 返回 (200,200)
        ///   currentPoint.X /= scale;  // 200 / 2 = 100（错误：重复缩放）
        ///   currentPoint.Y /= scale;  // 200 / 2 = 100
        ///   newX = currentPoint.X - dragOffset.X  // 100 - 0 = 100（但起始点也被缩放了）
        /// 
        /// 实际效果：控件移动量 = (50,50) 而非期望的 (100,100)
        /// 
        /// **Validates: Requirements 1.7**
        /// </summary>
        [TestMethod]
        [Description("Bug 1.7 具体场景：ZoomLevel=200，鼠标移动(100,100)，控件应移动(100,100)")]
        public void MouseMove_ZoomLevel200_MoveDelta100_ControlShouldMove100()
        {
            // Arrange
            double zoomScale = 2.0; // ZoomLevel = 200
            double startCanvasX = 100.0;
            double startCanvasY = 100.0;
            double endCanvasX = 200.0;
            double endCanvasY = 200.0;

            // 模拟 MouseDown 时记录的起始点（Bug 版本也会除以 scale）
            Point dragStartBuggy = DragDropCoordinateCalculator.CalculateDragStartPoint_Buggy(
                startCanvasX, startCanvasY, zoomScale);
            // dragStartBuggy = (50, 50)（被错误地除以了 scale）

            // 假设控件初始位置 (0, 0)，dragOffset = dragStart - controlPos = (50,50) - (0,0) = (50,50)
            double controlInitialX = 0.0;
            double controlInitialY = 0.0;
            double dragOffsetX = dragStartBuggy.X - controlInitialX; // 50
            double dragOffsetY = dragStartBuggy.Y - controlInitialY; // 50

            // Act：使用 Bug 版本计算 MouseMove 后的新位置
            Point newPos = DragDropCoordinateCalculator.CalculateMouseMovePosition_Buggy(
                endCanvasX, endCanvasY,
                dragOffsetX, dragOffsetY,
                zoomScale);
            // newPos = (200/2 - 50, 200/2 - 50) = (50, 50)

            double actualDeltaX = newPos.X - controlInitialX;
            double actualDeltaY = newPos.Y - controlInitialY;

            double expectedDeltaX = endCanvasX - startCanvasX; // 100（鼠标移动量）
            double expectedDeltaY = endCanvasY - startCanvasY; // 100

            // Assert：控件移动量应等于鼠标在 Canvas 本地坐标系中的移动量
            // 未修复时：actualDelta = (50,50) ≠ expectedDelta = (100,100)，断言失败
            Assert.AreEqual(expectedDeltaX, actualDeltaX, 0.001,
                $"Bug 1.7 已触发（X 轴）：ZoomLevel=200 时控件移动量错误。" +
                $"期望移动 {expectedDeltaX}px，实际移动 {actualDeltaX}px。" +
                $"根本原因：GetPosition 返回的 Canvas 本地坐标被再次除以 scale={zoomScale}。");

            Assert.AreEqual(expectedDeltaY, actualDeltaY, 0.001,
                $"Bug 1.7 已触发（Y 轴）：ZoomLevel=200 时控件移动量错误。" +
                $"期望移动 {expectedDeltaY}px，实际移动 {actualDeltaY}px。");
        }

        /// <summary>
        /// 测试 2b：MouseMove 重复缩放（Bug 1.7）ZoomLevel=50 场景。
        /// 
        /// 场景：ZoomLevel=50（scale=0.5），鼠标移动 (100,100)，控件应移动 (100,100)。
        /// 未修复时：控件移动 (200,200)（移动过快）。
        /// 
        /// **Validates: Requirements 1.7**
        /// </summary>
        [TestMethod]
        [Description("Bug 1.7 ZoomLevel=50：鼠标移动100px，控件应移动100px（未修复时移动200px）")]
        public void MouseMove_ZoomLevel50_MoveDelta100_ControlShouldMove100()
        {
            // Arrange
            double zoomScale = 0.5; // ZoomLevel = 50
            double startCanvasX = 200.0;
            double startCanvasY = 200.0;
            double endCanvasX = 300.0;
            double endCanvasY = 300.0;

            Point dragStartBuggy = DragDropCoordinateCalculator.CalculateDragStartPoint_Buggy(
                startCanvasX, startCanvasY, zoomScale);
            // dragStartBuggy = (400, 400)（被错误地除以了 0.5，即乘以 2）

            double controlInitialX = 0.0;
            double controlInitialY = 0.0;
            double dragOffsetX = dragStartBuggy.X - controlInitialX; // 400
            double dragOffsetY = dragStartBuggy.Y - controlInitialY; // 400

            // Act
            Point newPos = DragDropCoordinateCalculator.CalculateMouseMovePosition_Buggy(
                endCanvasX, endCanvasY,
                dragOffsetX, dragOffsetY,
                zoomScale);
            // newPos = (300/0.5 - 400, 300/0.5 - 400) = (600 - 400, 600 - 400) = (200, 200)

            double actualDeltaX = newPos.X - controlInitialX;
            double actualDeltaY = newPos.Y - controlInitialY;

            double expectedDeltaX = endCanvasX - startCanvasX; // 100
            double expectedDeltaY = endCanvasY - startCanvasY; // 100

            // Assert：未修复时 actualDelta = (200,200) ≠ expectedDelta = (100,100)
            Assert.AreEqual(expectedDeltaX, actualDeltaX, 0.001,
                $"Bug 1.7 已触发（ZoomLevel=50，X 轴）：控件移动量错误。" +
                $"期望移动 {expectedDeltaX}px，实际移动 {actualDeltaX}px。");

            Assert.AreEqual(expectedDeltaY, actualDeltaY, 0.001,
                $"Bug 1.7 已触发（ZoomLevel=50，Y 轴）：控件移动量错误。" +
                $"期望移动 {expectedDeltaY}px，实际移动 {actualDeltaY}px。");
        }

        /// <summary>
        /// 测试 2c：MouseMove 重复缩放（Bug 1.7）属性测试 - 多种缩放比例。
        /// 
        /// 对任意 ZoomLevel（排除 100%），验证控件移动量等于鼠标移动量。
        /// ZoomLevel=100 时 scale=1.0，/1.0 无影响，bug 不触发。
        /// 
        /// **Validates: Requirements 1.7**
        /// </summary>
        [TestMethod]
        [Description("Bug 1.7 属性测试：多种缩放比例下，控件移动量应等于鼠标移动量")]
        public void MouseMove_VariousZoomLevels_ExcludeScale1_ControlDeltaShouldEqualMouseDelta()
        {
            // 排除 ZoomLevel=100（scale=1.0），因为 /1.0 不改变值，bug 不触发
            int[] zoomLevels = { 25, 50, 75, 150, 200 };
            double mouseDeltaX = 80.0;
            double mouseDeltaY = 60.0;

            bool anyFailed = false;
            var failures = new System.Text.StringBuilder();

            foreach (int zoomLevel in zoomLevels)
            {
                double scale = zoomLevel / 100.0;
                double startX = 200.0;
                double startY = 200.0;
                double endX = startX + mouseDeltaX;
                double endY = startY + mouseDeltaY;

                // 模拟 MouseDown 记录起始点（Bug 版本）
                Point dragStartBuggy = DragDropCoordinateCalculator.CalculateDragStartPoint_Buggy(
                    startX, startY, scale);

                double controlInitialX = 50.0;
                double controlInitialY = 50.0;
                double dragOffsetX = dragStartBuggy.X - controlInitialX;
                double dragOffsetY = dragStartBuggy.Y - controlInitialY;

                // 模拟 MouseMove（Bug 版本）
                Point newPos = DragDropCoordinateCalculator.CalculateMouseMovePosition_Buggy(
                    endX, endY, dragOffsetX, dragOffsetY, scale);

                double actualDeltaX = newPos.X - controlInitialX;
                double actualDeltaY = newPos.Y - controlInitialY;

                if (System.Math.Abs(actualDeltaX - mouseDeltaX) > 0.001 ||
                    System.Math.Abs(actualDeltaY - mouseDeltaY) > 0.001)
                {
                    anyFailed = true;
                    failures.AppendLine(
                        $"  ZoomLevel={zoomLevel}% (scale={scale}): " +
                        $"期望移动({mouseDeltaX},{mouseDeltaY})，" +
                        $"实际移动({actualDeltaX:F2},{actualDeltaY:F2})");
                }
            }

            Assert.IsFalse(anyFailed,
                $"Bug 1.7 已触发：以下缩放比例下控件移动量与鼠标移动量不一致（重复缩放）：\n{failures}");
        }

        // =====================================================================
        // 验证修复版本的正确性（用于对比，确认测试逻辑本身正确）
        // =====================================================================

        /// <summary>
        /// 验证：修复版本的 Drop 边界计算正确（用于确认测试逻辑）。
        /// 此测试应该通过，证明修复方案正确。
        /// </summary>
        [TestMethod]
        [Description("验证修复版本：Drop 边界计算使用像素单位，结果正确")]
        public void Drop_Fixed_Width100mm_At700px_ShouldStayWithinCanvas()
        {
            double controlWidthMm = 100.0;
            double controlHeightMm = 20.0;
            double dropX = 700.0;
            double dropY = 100.0;
            double zoomScale = 1.0;

            double controlWidthPx = controlWidthMm * MM_TO_PIXEL;

            Point result = DragDropCoordinateCalculator.CalculateDropPosition_Fixed(
                dropX, dropY,
                controlWidthMm, controlHeightMm,
                CANVAS_WIDTH_PX, CANVAS_HEIGHT_PX,
                zoomScale);

            Assert.IsTrue(result.X + controlWidthPx <= CANVAS_WIDTH_PX,
                $"修复版本仍然超出边界：resultX={result.X:F1}, rightEdge={result.X + controlWidthPx:F1}");
            Assert.IsTrue(result.X >= 0, "修复版本 X 坐标不应为负");
        }

        /// <summary>
        /// 验证：修复版本的 MouseMove 坐标计算正确（用于确认测试逻辑）。
        /// 此测试应该通过，证明修复方案正确。
        /// </summary>
        [TestMethod]
        [Description("验证修复版本：ZoomLevel=200 时控件移动量等于鼠标移动量")]
        public void MouseMove_Fixed_ZoomLevel200_MoveDelta100_ControlShouldMove100()
        {
            double zoomScale = 2.0;
            double startCanvasX = 100.0;
            double startCanvasY = 100.0;
            double endCanvasX = 200.0;
            double endCanvasY = 200.0;

            // 修复版本：直接使用 Canvas 本地坐标
            Point dragStartFixed = DragDropCoordinateCalculator.CalculateDragStartPoint_Fixed(
                startCanvasX, startCanvasY, zoomScale);

            double controlInitialX = 0.0;
            double controlInitialY = 0.0;
            double dragOffsetX = dragStartFixed.X - controlInitialX;
            double dragOffsetY = dragStartFixed.Y - controlInitialY;

            Point newPos = DragDropCoordinateCalculator.CalculateMouseMovePosition_Fixed(
                endCanvasX, endCanvasY,
                dragOffsetX, dragOffsetY,
                zoomScale);

            double actualDeltaX = newPos.X - controlInitialX;
            double actualDeltaY = newPos.Y - controlInitialY;

            double expectedDeltaX = endCanvasX - startCanvasX; // 100
            double expectedDeltaY = endCanvasY - startCanvasY; // 100

            Assert.AreEqual(expectedDeltaX, actualDeltaX, 0.001,
                $"修复版本 X 轴移动量错误：期望 {expectedDeltaX}，实际 {actualDeltaX}");
            Assert.AreEqual(expectedDeltaY, actualDeltaY, 0.001,
                $"修复版本 Y 轴移动量错误：期望 {expectedDeltaY}，实际 {actualDeltaY}");
        }
    }
}
