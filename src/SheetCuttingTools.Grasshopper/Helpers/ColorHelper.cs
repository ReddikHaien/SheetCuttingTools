using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetCuttingTools.Grasshopper.Helpers
{
    internal static class ColorHelper
    {
        private static readonly IReadOnlyList<Color> colors =
        [
            Color.FromArgb(0, 0, 0),
            Color.FromArgb(1, 0, 103),
            Color.FromArgb(213, 255, 0),
            Color.FromArgb(255, 0, 86),
            Color.FromArgb(158, 0, 142),
            Color.FromArgb(14, 76, 161),
            Color.FromArgb(255, 229, 2),
            Color.FromArgb(0, 95, 57),
            Color.FromArgb(0, 255, 0),
            Color.FromArgb(149, 0, 58),
            Color.FromArgb(255, 147, 126),
            Color.FromArgb(164, 36, 0),
            Color.FromArgb(0, 21, 68),
            Color.FromArgb(145, 208, 203),
            Color.FromArgb(98, 14, 0),
            Color.FromArgb(107, 104, 130),
            Color.FromArgb(0, 0, 255),
            Color.FromArgb(0, 125, 181),
            Color.FromArgb(106, 130, 108),
            Color.FromArgb(0, 174, 126),
            Color.FromArgb(194, 140, 159),
            Color.FromArgb(190, 153, 112),
            Color.FromArgb(0, 143, 156),
            Color.FromArgb(95, 173, 78),
            Color.FromArgb(255, 0, 0),
            Color.FromArgb(255, 0, 246),
            Color.FromArgb(255, 2, 157),
            Color.FromArgb(104, 61, 59),
            Color.FromArgb(255, 116, 163),
            Color.FromArgb(150, 138, 232),
            Color.FromArgb(152, 255, 82),
            Color.FromArgb(167, 87, 64),
            Color.FromArgb(1, 255, 254),
            Color.FromArgb(255, 238, 232),
            Color.FromArgb(254, 137, 0),
            Color.FromArgb(189, 198, 255),
            Color.FromArgb(1, 208, 255),
            Color.FromArgb(187, 136, 0),
            Color.FromArgb(117, 68, 177),
            Color.FromArgb(165, 255, 210),
            Color.FromArgb(255, 166, 254),
            Color.FromArgb(119, 77, 0),
            Color.FromArgb(122, 71, 130),
            Color.FromArgb(38, 52, 0),
            Color.FromArgb(0, 71, 84),
            Color.FromArgb(67, 0, 44),
            Color.FromArgb(181, 0, 255),
            Color.FromArgb(255, 177, 103),
            Color.FromArgb(255, 219, 102),
            Color.FromArgb(144, 251, 146),
            Color.FromArgb(126, 45, 210),
            Color.FromArgb(189, 211, 147),
            Color.FromArgb(229, 111, 254),
            Color.FromArgb(222, 255, 116),
            Color.FromArgb(0, 255, 120),
            Color.FromArgb(0, 155, 255),
            Color.FromArgb(0, 100, 1),
            Color.FromArgb(0, 118, 255),
            Color.FromArgb(133, 169, 0),
            Color.FromArgb(0, 185, 23),
            Color.FromArgb(120, 130, 49),
            Color.FromArgb(0, 255, 198),
            Color.FromArgb(255, 110, 65),
            Color.FromArgb(232, 94, 190),
        ];

        public static Color GetColor(int i)
            => colors[(int)((((uint)i) * 17 + 13) % colors.Count)];
    }
}
