/*

This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using IText.IO;
using IText.IO.Util;
using IText.Logger;

namespace IText.Kernel.Colors {
    /// <summary>
    /// This class is a HashMap that contains the names of colors as a key and the
    /// corresponding RGB color as value.
    /// </summary>
    /// <remarks>
    /// This class is a HashMap that contains the names of colors as a key and the
    /// corresponding RGB color as value. (Source: Wikipedia
    /// http://en.wikipedia.org/wiki/Web_colors )
    /// </remarks>
    public class WebColors : Dictionary<string, int[]> {
        /// <summary>HashMap containing all the names and corresponding color values.</summary>
        public static readonly WebColors NAMES = new WebColors();

        private const double RGB_MAX_VAL = 255.0;

        static WebColors() {
            NAMES.Put("aliceblue", new[] { 0xf0, 0xf8, 0xff, 0xff });
            NAMES.Put("antiquewhite", new[] { 0xfa, 0xeb, 0xd7, 0xff });
            NAMES.Put("aqua", new[] { 0x00, 0xff, 0xff, 0xff });
            NAMES.Put("aquamarine", new[] { 0x7f, 0xff, 0xd4, 0xff });
            NAMES.Put("azure", new[] { 0xf0, 0xff, 0xff, 0xff });
            NAMES.Put("beige", new[] { 0xf5, 0xf5, 0xdc, 0xff });
            NAMES.Put("bisque", new[] { 0xff, 0xe4, 0xc4, 0xff });
            NAMES.Put("black", new[] { 0x00, 0x00, 0x00, 0xff });
            NAMES.Put("blanchedalmond", new[] { 0xff, 0xeb, 0xcd, 0xff });
            NAMES.Put("blue", new[] { 0x00, 0x00, 0xff, 0xff });
            NAMES.Put("blueviolet", new[] { 0x8a, 0x2b, 0xe2, 0xff });
            NAMES.Put("brown", new[] { 0xa5, 0x2a, 0x2a, 0xff });
            NAMES.Put("burlywood", new[] { 0xde, 0xb8, 0x87, 0xff });
            NAMES.Put("cadetblue", new[] { 0x5f, 0x9e, 0xa0, 0xff });
            NAMES.Put("chartreuse", new[] { 0x7f, 0xff, 0x00, 0xff });
            NAMES.Put("chocolate", new[] { 0xd2, 0x69, 0x1e, 0xff });
            NAMES.Put("coral", new[] { 0xff, 0x7f, 0x50, 0xff });
            NAMES.Put("cornflowerblue", new[] { 0x64, 0x95, 0xed, 0xff });
            NAMES.Put("cornsilk", new[] { 0xff, 0xf8, 0xdc, 0xff });
            NAMES.Put("crimson", new[] { 0xdc, 0x14, 0x3c, 0xff });
            NAMES.Put("cyan", new[] { 0x00, 0xff, 0xff, 0xff });
            NAMES.Put("darkblue", new[] { 0x00, 0x00, 0x8b, 0xff });
            NAMES.Put("darkcyan", new[] { 0x00, 0x8b, 0x8b, 0xff });
            NAMES.Put("darkgoldenrod", new[] { 0xb8, 0x86, 0x0b, 0xff });
            NAMES.Put("darkgray", new[] { 0xa9, 0xa9, 0xa9, 0xff });
            NAMES.Put("darkgrey", new[] { 0xa9, 0xa9, 0xa9, 0xff });
            NAMES.Put("darkgreen", new[] { 0x00, 0x64, 0x00, 0xff });
            NAMES.Put("darkkhaki", new[] { 0xbd, 0xb7, 0x6b, 0xff });
            NAMES.Put("darkmagenta", new[] { 0x8b, 0x00, 0x8b, 0xff });
            NAMES.Put("darkolivegreen", new[] { 0x55, 0x6b, 0x2f, 0xff });
            NAMES.Put("darkorange", new[] { 0xff, 0x8c, 0x00, 0xff });
            NAMES.Put("darkorchid", new[] { 0x99, 0x32, 0xcc, 0xff });
            NAMES.Put("darkred", new[] { 0x8b, 0x00, 0x00, 0xff });
            NAMES.Put("darksalmon", new[] { 0xe9, 0x96, 0x7a, 0xff });
            NAMES.Put("darkseagreen", new[] { 0x8f, 0xbc, 0x8f, 0xff });
            NAMES.Put("darkslateblue", new[] { 0x48, 0x3d, 0x8b, 0xff });
            NAMES.Put("darkslategray", new[] { 0x2f, 0x4f, 0x4f, 0xff });
            NAMES.Put("darkslategrey", new[] { 0x2f, 0x4f, 0x4f, 0xff });
            NAMES.Put("darkturquoise", new[] { 0x00, 0xce, 0xd1, 0xff });
            NAMES.Put("darkviolet", new[] { 0x94, 0x00, 0xd3, 0xff });
            NAMES.Put("deeppink", new[] { 0xff, 0x14, 0x93, 0xff });
            NAMES.Put("deepskyblue", new[] { 0x00, 0xbf, 0xff, 0xff });
            NAMES.Put("dimgray", new[] { 0x69, 0x69, 0x69, 0xff });
            NAMES.Put("dimgrey", new[] { 0x69, 0x69, 0x69, 0xff });
            NAMES.Put("dodgerblue", new[] { 0x1e, 0x90, 0xff, 0xff });
            NAMES.Put("firebrick", new[] { 0xb2, 0x22, 0x22, 0xff });
            NAMES.Put("floralwhite", new[] { 0xff, 0xfa, 0xf0, 0xff });
            NAMES.Put("forestgreen", new[] { 0x22, 0x8b, 0x22, 0xff });
            NAMES.Put("fuchsia", new[] { 0xff, 0x00, 0xff, 0xff });
            NAMES.Put("gainsboro", new[] { 0xdc, 0xdc, 0xdc, 0xff });
            NAMES.Put("ghostwhite", new[] { 0xf8, 0xf8, 0xff, 0xff });
            NAMES.Put("gold", new[] { 0xff, 0xd7, 0x00, 0xff });
            NAMES.Put("goldenrod", new[] { 0xda, 0xa5, 0x20, 0xff });
            NAMES.Put("gray", new[] { 0x80, 0x80, 0x80, 0xff });
            NAMES.Put("grey", new[] { 0x80, 0x80, 0x80, 0xff });
            NAMES.Put("green", new[] { 0x00, 0x80, 0x00, 0xff });
            NAMES.Put("greenyellow", new[] { 0xad, 0xff, 0x2f, 0xff });
            NAMES.Put("honeydew", new[] { 0xf0, 0xff, 0xf0, 0xff });
            NAMES.Put("hotpink", new[] { 0xff, 0x69, 0xb4, 0xff });
            NAMES.Put("indianred", new[] { 0xcd, 0x5c, 0x5c, 0xff });
            NAMES.Put("indigo", new[] { 0x4b, 0x00, 0x82, 0xff });
            NAMES.Put("ivory", new[] { 0xff, 0xff, 0xf0, 0xff });
            NAMES.Put("khaki", new[] { 0xf0, 0xe6, 0x8c, 0xff });
            NAMES.Put("lavender", new[] { 0xe6, 0xe6, 0xfa, 0xff });
            NAMES.Put("lavenderblush", new[] { 0xff, 0xf0, 0xf5, 0xff });
            NAMES.Put("lawngreen", new[] { 0x7c, 0xfc, 0x00, 0xff });
            NAMES.Put("lemonchiffon", new[] { 0xff, 0xfa, 0xcd, 0xff });
            NAMES.Put("lightblue", new[] { 0xad, 0xd8, 0xe6, 0xff });
            NAMES.Put("lightcoral", new[] { 0xf0, 0x80, 0x80, 0xff });
            NAMES.Put("lightcyan", new[] { 0xe0, 0xff, 0xff, 0xff });
            NAMES.Put("lightgoldenrodyellow", new[] { 0xfa, 0xfa, 0xd2, 0xff });
            NAMES.Put("lightgreen", new[] { 0x90, 0xee, 0x90, 0xff });
            NAMES.Put("lightgray", new[] { 0xd3, 0xd3, 0xd3, 0xff });
            NAMES.Put("lightgrey", new[] { 0xd3, 0xd3, 0xd3, 0xff });
            NAMES.Put("lightpink", new[] { 0xff, 0xb6, 0xc1, 0xff });
            NAMES.Put("lightsalmon", new[] { 0xff, 0xa0, 0x7a, 0xff });
            NAMES.Put("lightseagreen", new[] { 0x20, 0xb2, 0xaa, 0xff });
            NAMES.Put("lightskyblue", new[] { 0x87, 0xce, 0xfa, 0xff });
            NAMES.Put("lightslategray", new[] { 0x77, 0x88, 0x99, 0xff });
            NAMES.Put("lightslategrey", new[] { 0x77, 0x88, 0x99, 0xff });
            NAMES.Put("lightsteelblue", new[] { 0xb0, 0xc4, 0xde, 0xff });
            NAMES.Put("lightyellow", new[] { 0xff, 0xff, 0xe0, 0xff });
            NAMES.Put("lime", new[] { 0x00, 0xff, 0x00, 0xff });
            NAMES.Put("limegreen", new[] { 0x32, 0xcd, 0x32, 0xff });
            NAMES.Put("linen", new[] { 0xfa, 0xf0, 0xe6, 0xff });
            NAMES.Put("magenta", new[] { 0xff, 0x00, 0xff, 0xff });
            NAMES.Put("maroon", new[] { 0x80, 0x00, 0x00, 0xff });
            NAMES.Put("mediumaquamarine", new[] { 0x66, 0xcd, 0xaa, 0xff });
            NAMES.Put("mediumblue", new[] { 0x00, 0x00, 0xcd, 0xff });
            NAMES.Put("mediumorchid", new[] { 0xba, 0x55, 0xd3, 0xff });
            NAMES.Put("mediumpurple", new[] { 0x93, 0x70, 0xdb, 0xff });
            NAMES.Put("mediumseagreen", new[] { 0x3c, 0xb3, 0x71, 0xff });
            NAMES.Put("mediumslateblue", new[] { 0x7b, 0x68, 0xee, 0xff });
            NAMES.Put("mediumspringgreen", new[] { 0x00, 0xfa, 0x9a, 0xff });
            NAMES.Put("mediumturquoise", new[] { 0x48, 0xd1, 0xcc, 0xff });
            NAMES.Put("mediumvioletred", new[] { 0xc7, 0x15, 0x85, 0xff });
            NAMES.Put("midnightblue", new[] { 0x19, 0x19, 0x70, 0xff });
            NAMES.Put("mintcream", new[] { 0xf5, 0xff, 0xfa, 0xff });
            NAMES.Put("mistyrose", new[] { 0xff, 0xe4, 0xe1, 0xff });
            NAMES.Put("moccasin", new[] { 0xff, 0xe4, 0xb5, 0xff });
            NAMES.Put("navajowhite", new[] { 0xff, 0xde, 0xad, 0xff });
            NAMES.Put("navy", new[] { 0x00, 0x00, 0x80, 0xff });
            NAMES.Put("oldlace", new[] { 0xfd, 0xf5, 0xe6, 0xff });
            NAMES.Put("olive", new[] { 0x80, 0x80, 0x00, 0xff });
            NAMES.Put("olivedrab", new[] { 0x6b, 0x8e, 0x23, 0xff });
            NAMES.Put("orange", new[] { 0xff, 0xa5, 0x00, 0xff });
            NAMES.Put("orangered", new[] { 0xff, 0x45, 0x00, 0xff });
            NAMES.Put("orchid", new[] { 0xda, 0x70, 0xd6, 0xff });
            NAMES.Put("palegoldenrod", new[] { 0xee, 0xe8, 0xaa, 0xff });
            NAMES.Put("palegreen", new[] { 0x98, 0xfb, 0x98, 0xff });
            NAMES.Put("paleturquoise", new[] { 0xaf, 0xee, 0xee, 0xff });
            NAMES.Put("palevioletred", new[] { 0xdb, 0x70, 0x93, 0xff });
            NAMES.Put("papayawhip", new[] { 0xff, 0xef, 0xd5, 0xff });
            NAMES.Put("peachpuff", new[] { 0xff, 0xda, 0xb9, 0xff });
            NAMES.Put("peru", new[] { 0xcd, 0x85, 0x3f, 0xff });
            NAMES.Put("pink", new[] { 0xff, 0xc0, 0xcb, 0xff });
            NAMES.Put("plum", new[] { 0xdd, 0xa0, 0xdd, 0xff });
            NAMES.Put("powderblue", new[] { 0xb0, 0xe0, 0xe6, 0xff });
            NAMES.Put("purple", new[] { 0x80, 0x00, 0x80, 0xff });
            NAMES.Put("red", new[] { 0xff, 0x00, 0x00, 0xff });
            NAMES.Put("rosybrown", new[] { 0xbc, 0x8f, 0x8f, 0xff });
            NAMES.Put("royalblue", new[] { 0x41, 0x69, 0xe1, 0xff });
            NAMES.Put("saddlebrown", new[] { 0x8b, 0x45, 0x13, 0xff });
            NAMES.Put("salmon", new[] { 0xfa, 0x80, 0x72, 0xff });
            NAMES.Put("sandybrown", new[] { 0xf4, 0xa4, 0x60, 0xff });
            NAMES.Put("seagreen", new[] { 0x2e, 0x8b, 0x57, 0xff });
            NAMES.Put("seashell", new[] { 0xff, 0xf5, 0xee, 0xff });
            NAMES.Put("sienna", new[] { 0xa0, 0x52, 0x2d, 0xff });
            NAMES.Put("silver", new[] { 0xc0, 0xc0, 0xc0, 0xff });
            NAMES.Put("skyblue", new[] { 0x87, 0xce, 0xeb, 0xff });
            NAMES.Put("slateblue", new[] { 0x6a, 0x5a, 0xcd, 0xff });
            NAMES.Put("slategray", new[] { 0x70, 0x80, 0x90, 0xff });
            NAMES.Put("slategrey", new[] { 0x70, 0x80, 0x90, 0xff });
            NAMES.Put("snow", new[] { 0xff, 0xfa, 0xfa, 0xff });
            NAMES.Put("springgreen", new[] { 0x00, 0xff, 0x7f, 0xff });
            NAMES.Put("steelblue", new[] { 0x46, 0x82, 0xb4, 0xff });
            NAMES.Put("tan", new[] { 0xd2, 0xb4, 0x8c, 0xff });
            NAMES.Put("teal", new[] { 0x00, 0x80, 0x80, 0xff });
            NAMES.Put("thistle", new[] { 0xd8, 0xbf, 0xd8, 0xff });
            NAMES.Put("tomato", new[] { 0xff, 0x63, 0x47, 0xff });
            NAMES.Put("transparent", new[] { 0xff, 0xff, 0xff, 0x00 });
            NAMES.Put("turquoise", new[] { 0x40, 0xe0, 0xd0, 0xff });
            NAMES.Put("violet", new[] { 0xee, 0x82, 0xee, 0xff });
            NAMES.Put("wheat", new[] { 0xf5, 0xde, 0xb3, 0xff });
            NAMES.Put("white", new[] { 0xff, 0xff, 0xff, 0xff });
            NAMES.Put("whitesmoke", new[] { 0xf5, 0xf5, 0xf5, 0xff });
            NAMES.Put("yellow", new[] { 0xff, 0xff, 0x00, 0xff });
            NAMES.Put("yellowgreen", new[] { 0x9a, 0xcd, 0x32, 0xff });
        }

        /// <summary>Gives you a DeviceRgb based on a name.</summary>
        /// <param name="name">
        /// a name such as black, violet, cornflowerblue or #RGB or
        /// #RRGGBB or RGB or RRGGBB or rgb(R,G,B)
        /// </param>
        /// <returns>the corresponding DeviceRgb object. Never returns null.</returns>
        public static DeviceRgb GetRGBColor(string name) {
            var rgbaColor = GetRGBAColor(name);
            if (rgbaColor == null) {
                return new DeviceRgb(0, 0, 0);
            }

            return new DeviceRgb(rgbaColor[0], rgbaColor[1], rgbaColor[2]);
        }

        /// <summary>Gives an array of four floats that contain RGBA values, each value is between 0 and 1.</summary>
        /// <param name="name">
        /// a name such as black, violet, cornflowerblue or #RGB or
        /// #RRGGBB or RGB or RRGGBB or rgb(R,G,B) or rgb(R,G,B,A)
        /// </param>
        /// <returns>the corresponding array of four floats, or <c>null</c> if parsing failed.</returns>
        public static float[] GetRGBAColor(string name) {
            float[] color = null;
            try {
                var colorName = name.ToLowerInvariant();
                var colorStrWithoutHash = MissingHashColorFormat(colorName);
                if (colorName.StartsWith("#") || colorStrWithoutHash) {
                    if (!colorStrWithoutHash) {
                        // lop off the # to unify hex parsing.
                        colorName = colorName.Substring(1);
                    }
                    if (colorName.Length == 3) {
                        var red = colorName.JSubstring(0, 1);
                        color = new float[] { 0, 0, 0, 1 };
                        color[0] = (float)(Convert.ToInt32(red + red, 16) / RGB_MAX_VAL);
                        var green = colorName.JSubstring(1, 2);
                        color[1] = (float)(Convert.ToInt32(green + green, 16) / RGB_MAX_VAL);
                        var blue = colorName.Substring(2);
                        color[2] = (float)(Convert.ToInt32(blue + blue, 16) / RGB_MAX_VAL);
                    }
                    else {
                        if (colorName.Length == 6) {
                            color = new float[] { 0, 0, 0, 1 };
                            color[0] = (float)(Convert.ToInt32(colorName.JSubstring(0, 2), 16) / RGB_MAX_VAL);
                            color[1] = (float)(Convert.ToInt32(colorName.JSubstring(2, 4), 16) / RGB_MAX_VAL);
                            color[2] = (float)(Convert.ToInt32(colorName.Substring(4), 16) / RGB_MAX_VAL);
                        }
                        else {
                            var logger = LogManager.GetLogger(typeof(WebColors));
                            logger.Error(LogMessageConstant.UNKNOWN_COLOR_FORMAT_MUST_BE_RGB_OR_RRGGBB);
                        }
                    }
                }
                else {
                    if (colorName.StartsWith("rgb(")) {
                        var delim = "rgb(), \t\r\n\f";
                        var tok = new StringTokenizer(colorName, delim);
                        color = new float[] { 0, 0, 0, 1 };
                        ParseRGBColors(color, tok);
                    }
                    else {
                        if (colorName.StartsWith("rgba(")) {
                            var delim = "rgba(), \t\r\n\f";
                            var tok = new StringTokenizer(colorName, delim);
                            color = new float[] { 0, 0, 0, 1 };
                            ParseRGBColors(color, tok);
                            if (tok.HasMoreTokens()) {
                                color[3] = GetAlphaChannelValue(tok.NextToken());
                            }
                        }
                        else {
                            if (NAMES.Contains(colorName)) {
                                var intColor = NAMES.Get(colorName);
                                color = new float[] { 0, 0, 0, 1 };
                                color[0] = (float)(intColor[0] / RGB_MAX_VAL);
                                color[1] = (float)(intColor[1] / RGB_MAX_VAL);
                                color[2] = (float)(intColor[2] / RGB_MAX_VAL);
                            }
                        }
                    }
                }
            }
            catch (Exception) {
                // Will just return null in this case
                color = null;
            }
            return color;
        }

        private static void ParseRGBColors(float[] color, StringTokenizer tok) {
            for (var k = 0; k < 3; ++k) {
                if (tok.HasMoreTokens()) {
                    color[k] = GetRGBChannelValue(tok.NextToken());
                    color[k] = Math.Max(0, color[k]);
                    color[k] = Math.Min(1f, color[k]);
                }
            }
        }

        /// <summary>
        /// A web color string without the leading # will be 3 or 6 characters long
        /// and all those characters will be hex digits.
        /// </summary>
        /// <remarks>
        /// A web color string without the leading # will be 3 or 6 characters long
        /// and all those characters will be hex digits. NOTE: colStr must be all
        /// lower case or the current hex letter test will fail.
        /// </remarks>
        /// <param name="colStr">
        /// A non-null, lower case string that might describe an RGB color
        /// in hex.
        /// </param>
        /// <returns>Is this a web color hex string without the leading #?</returns>
        private static bool MissingHashColorFormat(string colStr) {
            var len = colStr.Length;
            if (len == 3 || len == 6) {
                // and it just contains hex chars 0-9, a-f, A-F
                var match = "[0-9a-f]{" + len + "}";
                return colStr.Matches(match);
            }
            return false;
        }

        private static float GetRGBChannelValue(string rgbChannel)
        {
	        if (rgbChannel.EndsWith("%")) {
                return ParsePercentValue(rgbChannel);
            }

	        return (float)(Convert.ToInt32(rgbChannel, CultureInfo.InvariantCulture) / RGB_MAX_VAL
		        );
        }

        private static float GetAlphaChannelValue(string rgbChannel) {
            float alpha;
            if (rgbChannel.EndsWith("%")) {
                alpha = ParsePercentValue(rgbChannel);
            }
            else {
                alpha = float.Parse(rgbChannel, CultureInfo.InvariantCulture);
            }
            alpha = Math.Max(0, alpha);
            alpha = Math.Min(1f, alpha);
            return alpha;
        }

        private static float ParsePercentValue(string rgbChannel) {
            return (float)(float.Parse(rgbChannel.JSubstring(0, rgbChannel.Length - 1), CultureInfo.InvariantCulture
                ) / 100.0);
        }
    }
}
