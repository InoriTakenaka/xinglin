using xinglin.Models.CoreEntities;

namespace xinglin.Services.Pdf
{
    /// <summary>
    /// 坐标与单位转换工具类，封装所有 PDF 点坐标转换逻辑。
    /// </summary>
    public static class PdfCoordinateConverter
    {
        /// <summary>
        /// 将像素值（96 dpi）转换为 PDF 点（72 dpi）。
        /// 转换公式：points = pixels × 72.0 / 96.0
        /// </summary>
        public static double PixelsToPoints(double pixels) => pixels * 72.0 / 96.0;

        /// <summary>
        /// 将毫米值转换为 PDF 点。
        /// 转换公式：points = mm × 72.0 / 25.4
        /// </summary>
        public static double MmToPoints(double mm) => mm * 72.0 / 25.4;

        /// <summary>
        /// 根据 LayoutMetadata 返回页面尺寸（单位：点）。
        /// 当 IsLandscape 为 true 时，交换宽高以生成横向页面。
        /// </summary>
        public static (double width, double height) GetPageSize(LayoutMetadata layout)
        {
            double w = MmToPoints(layout.PaperWidth);
            double h = MmToPoints(layout.PaperHeight);
            return layout.IsLandscape ? (h, w) : (w, h);
        }

        /// <summary>
        /// 返回内容区起始点（页边距偏移），单位：点。
        /// </summary>
        public static (double originX, double originY) GetContentOrigin(LayoutMetadata layout)
            => (MmToPoints(layout.MarginLeft), MmToPoints(layout.MarginTop));
    }
}
