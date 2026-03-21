using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using xinglin.Utils;

namespace xinglin.Tests
{
    /// <summary>
    /// 保持性测试（Preservation Tests）。
    ///
    /// 这些测试验证非 Bug 路径下的行为在修复前后保持不变。
    /// 在未修复代码上运行时 EXPECTED TO PASS（确认基线行为）。
    ///
    /// Validates: Requirements 3.1, 3.2, 3.3, 3.6
    /// </summary>
    [TestClass]
    public class PreservationTests
    {
        public const double MM_TO_PIXEL = 96.0 / 25.4;
        private const double CANVAS_WIDTH_PX = 794.0;
        private const double CANVAS_HEIGHT_PX = 1123.0;

        // =====================================================================
        // 属性 2a：ZoomLevel=100（scale=1.0）时，任意鼠标 delta，控件移动量 == 鼠标移动量
        // =====================================================================

        /// <summary>
        /// 属性 2a：ZoomLevel=100 时，鼠标移动 delta，控件移动量 == 鼠标移动量。
        ///
        /// 原理：scale=1.0 时，/1.0 不改变任何值，Bug 1.7 不触发。
        /// 因此修复前后行为相同，且控件移动量等于鼠标移动量。
        ///
        /// **Validates: Requirements 3.6**
        /// </summary>
        [TestMethod]
        [Description("属性 2a：ZoomLevel=100 时，控件移动量 == 鼠标移动量（/1.0 无影响）")]
        public void Property2a_ZoomLevel100_AnyDelta_ControlDeltaEqualsMouseDelta()
        {
            // Arrange
            double zoomScale = 1.0; // ZoomLevel = 100
            double controlInitialX = 50.0;
            double controlInitialY = 80.0;

            // 多种鼠标 delta 场景
            var testCases = new[]
            {
                (startX: 100.0, startY: 100.0, endX: 150.0, endY: 130.0),
                (startX: 200.0, startY: 300.0, endX: 250.0, endY: 280.0),
                (startX: 50.0,  startY: 50.0,  endX: 10.0,  endY: 20.0),   // 向左上移动
                (startX: 400.0, startY: 500.0, endX: 480.0, endY: 560.0),
            };

            foreach (var (startX, startY, endX, endY) in testCases)
            {
                double expectedDeltaX = endX - startX;
                double expectedDeltaY = endY - startY;

                // 模拟 MouseDown：记录拖拽起始点（Buggy 版本，scale=1.0 时与 Fixed 相同）
                Point dragStart = DragDropCoordinateCalculator.CalculateDragStartPoint_Buggy(
                    startX, startY, zoomScale);

                double dragOffsetX = dragStart.X - controlInitialX;
                double dragOffsetY = dragStart.Y - controlInitialY;

                // Act：模拟 MouseMove（Buggy 版本，scale=1.0 时 /1.0 无影响）
                Point newPos = DragDropCoordinateCalculator.CalculateMouseMovePosition_Buggy(
                    endX, endY, dragOffsetX, dragOffsetY, zoomScale);

                double actualDeltaX = newPos.X - controlInitialX;
                double actualDeltaY = newPos.Y - controlInitialY;

                // Assert：控件移动量 == 鼠标移动量
                Assert.AreEqual(expectedDeltaX, actualDeltaX, 0.001,
                    $"属性 2a 失败（X 轴）：start=({startX},{startY}), end=({endX},{endY}), " +
                    $"期望 delta={expectedDeltaX}，实际 delta={actualDeltaX}");
                Assert.AreEqual(expectedDeltaY, actualDeltaY, 0.001,
                    $"属性 2a 失败（Y 轴）：start=({startX},{startY}), end=({endX},{endY}), " +
                    $"期望 delta={expectedDeltaY}，实际 delta={actualDeltaY}");
            }
        }

        /// <summary>
        /// 属性 2a 扩展：ZoomLevel=100 时，Buggy 与 Fixed 结果完全一致。
        ///
        /// **Validates: Requirements 3.6**
        /// </summary>
        [TestMethod]
        [Description("属性 2a 扩展：ZoomLevel=100 时 Buggy 与 Fixed 的 MouseMove 结果相同（/1.0 无影响）")]
        public void Property2a_ZoomLevel100_BuggyAndFixedResultsAreEqual()
        {
            // Arrange
            double zoomScale = 1.0; // ZoomLevel = 100
            double canvasMouseX = 250.0;
            double canvasMouseY = 180.0;
            double dragOffsetX = 30.0;
            double dragOffsetY = 20.0;

            // Act
            Point buggyResult = DragDropCoordinateCalculator.CalculateMouseMovePosition_Buggy(
                canvasMouseX, canvasMouseY, dragOffsetX, dragOffsetY, zoomScale);

            Point fixedResult = DragDropCoordinateCalculator.CalculateMouseMovePosition_Fixed(
                canvasMouseX, canvasMouseY, dragOffsetX, dragOffsetY, zoomScale);

            // Assert：scale=1.0 时两者结果应完全一致
            Assert.AreEqual(fixedResult.X, buggyResult.X, 0.001,
                $"ZoomLevel=100 时 X 坐标不一致：Buggy={buggyResult.X:F3}, Fixed={fixedResult.X:F3}");
            Assert.AreEqual(fixedResult.Y, buggyResult.Y, 0.001,
                $"ZoomLevel=100 时 Y 坐标不一致：Buggy={buggyResult.Y:F3}, Fixed={fixedResult.Y:F3}");

            // 同时验证实际值正确：newX = mouseX - offsetX
            Assert.AreEqual(canvasMouseX - dragOffsetX, buggyResult.X, 0.001,
                "ZoomLevel=100 时控件 X 坐标应为 mouseX - dragOffsetX");
            Assert.AreEqual(canvasMouseY - dragOffsetY, buggyResult.Y, 0.001,
                "ZoomLevel=100 时控件 Y 坐标应为 mouseY - dragOffsetY");
        }

        // =====================================================================
        // 属性 2b：Drop 到画布中央（不触发边界限制），控件 X 坐标 == dropX
        // =====================================================================

        /// <summary>
        /// 属性 2b：Drop 到画布中央（X=300，Width=50mm，canvas=794px），
        /// 不触发边界限制，控件 X 坐标 == dropX。
        ///
        /// 验证：50mm ≈ 189px，300 + 189 = 489 &lt; 794，不触发右边界限制。
        /// 此时 Buggy 版本的边界判断（300 + 50 = 350 &lt; 794）也不触发，
        /// 因此 resultX == dropX（未被边界限制修改）。
        ///
        /// **Validates: Requirements 3.1**
        /// </summary>
        [TestMethod]
        [Description("属性 2b：Drop 到画布中央（X=300，控件宽50mm），不触发边界限制，控件 X == dropX")]
        public void Property2b_DropCenterCanvas_Width50mm_ControlXEqualsDropX()
        {
            // Arrange
            double controlWidthMm = 50.0;
            double controlHeightMm = 20.0;
            double dropX = 300.0; // 画布中央附近（canvas=794px）
            double dropY = 200.0;
            double zoomScale = 1.0;

            // 验证前提：50mm 不触发 Buggy 边界（300 + 50 = 350 < 794）
            Assert.IsTrue(dropX + controlWidthMm < CANVAS_WIDTH_PX,
                "前提条件：Buggy 边界判断不应触发（毫米值也不超出）");
            // 验证前提：50mm 转像素也不触发 Fixed 边界（300 + 189 ≈ 489 < 794）
            double controlWidthPx = controlWidthMm * MM_TO_PIXEL;
            Assert.IsTrue(dropX + controlWidthPx < CANVAS_WIDTH_PX,
                "前提条件：Fixed 边界判断不应触发（像素值也不超出）");

            // Act：使用 Buggy 版本（修复前）
            Point result = DragDropCoordinateCalculator.CalculateDropPosition_Buggy(
                dropX, dropY, controlWidthMm, controlHeightMm,
                CANVAS_WIDTH_PX, CANVAS_HEIGHT_PX, zoomScale);

            // Assert：不触发边界限制时，控件 X 坐标 == dropX / zoomScale == dropX（scale=1.0）
            Assert.AreEqual(dropX / zoomScale, result.X, 0.001,
                $"属性 2b 失败：Drop 到中央时控件 X 应等于 dropX={dropX}，实际={result.X:F3}");
            Assert.AreEqual(dropY / zoomScale, result.Y, 0.001,
                $"属性 2b 失败：Drop 到中央时控件 Y 应等于 dropY={dropY}，实际={result.Y:F3}");
        }

        /// <summary>
        /// 属性 2b 扩展：Drop 到画布中央时，Buggy 与 Fixed 结果完全一致。
        ///
        /// **Validates: Requirements 3.1**
        /// </summary>
        [TestMethod]
        [Description("属性 2b 扩展：Drop 到画布中央（X=300，控件宽50mm），Buggy 与 Fixed 结果相同")]
        public void Property2b_DropCenterCanvas_Width50mm_BuggyAndFixedResultsAreEqual()
        {
            // Arrange
            double controlWidthMm = 50.0;
            double controlHeightMm = 20.0;
            double dropX = 300.0;
            double dropY = 200.0;
            double zoomScale = 1.0;

            // 验证前提：不触发任何边界限制
            double controlWidthPx = controlWidthMm * MM_TO_PIXEL;
            Assert.IsTrue(dropX + controlWidthMm < CANVAS_WIDTH_PX,
                "前提条件：Buggy 边界判断不应触发");
            Assert.IsTrue(dropX + controlWidthPx < CANVAS_WIDTH_PX,
                "前提条件：Fixed 边界判断不应触发");

            // Act
            Point buggyResult = DragDropCoordinateCalculator.CalculateDropPosition_Buggy(
                dropX, dropY, controlWidthMm, controlHeightMm,
                CANVAS_WIDTH_PX, CANVAS_HEIGHT_PX, zoomScale);

            Point fixedResult = DragDropCoordinateCalculator.CalculateDropPosition_Fixed(
                dropX, dropY, controlWidthMm, controlHeightMm,
                CANVAS_WIDTH_PX, CANVAS_HEIGHT_PX, zoomScale);

            // Assert：不触发边界限制时，两者结果应完全一致
            Assert.AreEqual(fixedResult.X, buggyResult.X, 0.001,
                $"Drop 中央时 X 坐标不一致：Buggy={buggyResult.X:F3}, Fixed={fixedResult.X:F3}");
            Assert.AreEqual(fixedResult.Y, buggyResult.Y, 0.001,
                $"Drop 中央时 Y 坐标不一致：Buggy={buggyResult.Y:F3}, Fixed={fixedResult.Y:F3}");
        }

        // =====================================================================
        // 属性 2c：DragOver 虚线框宽度 == control.Width * 96.0/25.4
        // =====================================================================

        /// <summary>
        /// 属性 2c：DragOver 虚线框宽度 = control.Width(mm) * 96.0/25.4。
        ///
        /// DragOver 中已正确使用 MM_TO_PIXEL 转换，此行为在修复前后保持不变。
        /// 验证转换公式和常量的正确性。
        ///
        /// **Validates: Requirements 3.1, 3.2**
        /// </summary>
        [TestMethod]
        [Description("属性 2c：DragOver 虚线框宽度 = control.Width * 96.0/25.4，转换结果正确")]
        public void Property2c_DragOverGhostRectWidth_EqualsControlWidthTimesMMToPixel()
        {
            // Arrange
            double controlWidthMm = 50.0;

            // Act：模拟 DragOver 中的宽度计算（control.Width * 96.0/25.4）
            double ghostRectWidthPx = controlWidthMm * DragDropCoordinateCalculator.MM_TO_PIXEL;

            // Assert：50mm * (96/25.4) ≈ 188.976px
            double expectedPx = controlWidthMm * (96.0 / 25.4);
            Assert.AreEqual(expectedPx, ghostRectWidthPx, 0.001,
                $"属性 2c 失败：{controlWidthMm}mm 应转换为 {expectedPx:F3}px，实际为 {ghostRectWidthPx:F3}px");

            // 验证 MM_TO_PIXEL 常量值正确
            Assert.AreEqual(96.0 / 25.4, DragDropCoordinateCalculator.MM_TO_PIXEL, 0.0001,
                "MM_TO_PIXEL 常量值不正确");
        }

        /// <summary>
        /// 属性 2c 扩展：多种控件宽度的 mm→px 转换均正确。
        ///
        /// **Validates: Requirements 3.1, 3.2**
        /// </summary>
        [TestMethod]
        [Description("属性 2c 扩展：多种控件宽度的 DragOver 虚线框宽度转换正确")]
        public void Property2c_DragOverGhostRectWidth_VariousWidths_AllCorrect()
        {
            double[] widthsMm = { 10.0, 25.0, 50.0, 100.0, 150.0 };

            foreach (double widthMm in widthsMm)
            {
                double expected = widthMm * (96.0 / 25.4);
                double actual = widthMm * DragDropCoordinateCalculator.MM_TO_PIXEL;

                Assert.AreEqual(expected, actual, 0.001,
                    $"属性 2c 失败：宽度 {widthMm}mm 转换错误，期望 {expected:F3}px，实际 {actual:F3}px");
            }
        }
    }
}
