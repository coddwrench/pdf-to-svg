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
using System.IO;
using IText.IO.Font;
using IText.IO.Image;
using IText.IO.Util;
using IText.Kernel.Colors;
using IText.Kernel.Font;
using IText.Kernel.Geom;
using IText.Kernel.Pdf.Xobject;

namespace IText.Kernel.Pdf.Canvas.Wmf {
    /// <summary>A class to process WMF files.</summary>
    /// <remarks>
    /// A class to process WMF files. Used internally by
    /// <see cref="WmfImageHelper"/>.
    /// </remarks>
    public class MetaDo {
        public const int META_SETBKCOLOR = 0x0201;

        public const int META_SETBKMODE = 0x0102;

        public const int META_SETMAPMODE = 0x0103;

        public const int META_SETROP2 = 0x0104;

        public const int META_SETRELABS = 0x0105;

        public const int META_SETPOLYFILLMODE = 0x0106;

        public const int META_SETSTRETCHBLTMODE = 0x0107;

        public const int META_SETTEXTCHAREXTRA = 0x0108;

        public const int META_SETTEXTCOLOR = 0x0209;

        public const int META_SETTEXTJUSTIFICATION = 0x020A;

        public const int META_SETWINDOWORG = 0x020B;

        public const int META_SETWINDOWEXT = 0x020C;

        public const int META_SETVIEWPORTORG = 0x020D;

        public const int META_SETVIEWPORTEXT = 0x020E;

        public const int META_OFFSETWINDOWORG = 0x020F;

        public const int META_SCALEWINDOWEXT = 0x0410;

        public const int META_OFFSETVIEWPORTORG = 0x0211;

        public const int META_SCALEVIEWPORTEXT = 0x0412;

        public const int META_LINETO = 0x0213;

        public const int META_MOVETO = 0x0214;

        public const int META_EXCLUDECLIPRECT = 0x0415;

        public const int META_INTERSECTCLIPRECT = 0x0416;

        public const int META_ARC = 0x0817;

        public const int META_ELLIPSE = 0x0418;

        public const int META_FLOODFILL = 0x0419;

        public const int META_PIE = 0x081A;

        public const int META_RECTANGLE = 0x041B;

        public const int META_ROUNDRECT = 0x061C;

        public const int META_PATBLT = 0x061D;

        public const int META_SAVEDC = 0x001E;

        public const int META_SETPIXEL = 0x041F;

        public const int META_OFFSETCLIPRGN = 0x0220;

        public const int META_TEXTOUT = 0x0521;

        public const int META_BITBLT = 0x0922;

        public const int META_STRETCHBLT = 0x0B23;

        public const int META_POLYGON = 0x0324;

        public const int META_POLYLINE = 0x0325;

        public const int META_ESCAPE = 0x0626;

        public const int META_RESTOREDC = 0x0127;

        public const int META_FILLREGION = 0x0228;

        public const int META_FRAMEREGION = 0x0429;

        public const int META_INVERTREGION = 0x012A;

        public const int META_PAINTREGION = 0x012B;

        public const int META_SELECTCLIPREGION = 0x012C;

        public const int META_SELECTOBJECT = 0x012D;

        public const int META_SETTEXTALIGN = 0x012E;

        public const int META_CHORD = 0x0830;

        public const int META_SETMAPPERFLAGS = 0x0231;

        public const int META_EXTTEXTOUT = 0x0a32;

        public const int META_SETDIBTODEV = 0x0d33;

        public const int META_SELECTPALETTE = 0x0234;

        public const int META_REALIZEPALETTE = 0x0035;

        public const int META_ANIMATEPALETTE = 0x0436;

        public const int META_SETPALENTRIES = 0x0037;

        public const int META_POLYPOLYGON = 0x0538;

        public const int META_RESIZEPALETTE = 0x0139;

        public const int META_DIBBITBLT = 0x0940;

        public const int META_DIBSTRETCHBLT = 0x0b41;

        public const int META_DIBCREATEPATTERNBRUSH = 0x0142;

        public const int META_STRETCHDIB = 0x0f43;

        public const int META_EXTFLOODFILL = 0x0548;

        public const int META_DELETEOBJECT = 0x01f0;

        public const int META_CREATEPALETTE = 0x00f7;

        public const int META_CREATEPATTERNBRUSH = 0x01F9;

        public const int META_CREATEPENINDIRECT = 0x02FA;

        public const int META_CREATEFONTINDIRECT = 0x02FB;

        public const int META_CREATEBRUSHINDIRECT = 0x02FC;

        public const int META_CREATEREGION = 0x06FF;

        /// <summary>PdfCanvas of the MetaDo object.</summary>
        public PdfCanvas cb;

        /// <summary>The InputMeta instance containing the data.</summary>
        public InputMeta @in;

        internal int left;

        internal int top;

        internal int right;

        internal int bottom;

        internal int inch;

        internal MetaState state = new MetaState();

        /// <summary>Creates a MetaDo instance.</summary>
        /// <param name="in">inputstream containing the data</param>
        /// <param name="cb">PdfCanvas</param>
        public MetaDo(Stream @in, PdfCanvas cb) {
            this.cb = cb;
            this.@in = new InputMeta(@in);
        }

        /// <summary>Reads and processes all the data of the InputMeta.</summary>
        public virtual void ReadAll() {
            if (@in.ReadInt() != unchecked((int)(0x9AC6CDD7))) {
                throw new PdfException(PdfException.NotAPlaceableWindowsMetafile);
            }
            @in.ReadWord();
            left = @in.ReadShort();
            top = @in.ReadShort();
            right = @in.ReadShort();
            bottom = @in.ReadShort();
            inch = @in.ReadWord();
            state.SetScalingX((right - left) / (float)inch * 72f);
            state.SetScalingY((bottom - top) / (float)inch * 72f);
            state.SetOffsetWx(left);
            state.SetOffsetWy(top);
            state.SetExtentWx(right - left);
            state.SetExtentWy(bottom - top);
            @in.ReadInt();
            @in.ReadWord();
            @in.Skip(18);
            int tsize;
            int function;
            cb.SetLineCapStyle(PdfCanvasConstants.LineCapStyle.ROUND);
            cb.SetLineJoinStyle(PdfCanvasConstants.LineJoinStyle.ROUND);
            for (; ; ) {
                var lenMarker = @in.GetLength();
                tsize = @in.ReadInt();
                if (tsize < 3) {
                    break;
                }
                function = @in.ReadWord();
                switch (function) {
                    case 0: {
                        break;
                    }

                    case META_CREATEPALETTE:
                    case META_CREATEREGION:
                    case META_DIBCREATEPATTERNBRUSH: {
                        state.AddMetaObject(new MetaObject());
                        break;
                    }

                    case META_CREATEPENINDIRECT: {
                        var pen = new MetaPen();
                        pen.Init(@in);
                        state.AddMetaObject(pen);
                        break;
                    }

                    case META_CREATEBRUSHINDIRECT: {
                        var brush = new MetaBrush();
                        brush.Init(@in);
                        state.AddMetaObject(brush);
                        break;
                    }

                    case META_CREATEFONTINDIRECT: {
                        var font = new MetaFont();
                        font.Init(@in);
                        state.AddMetaObject(font);
                        break;
                    }

                    case META_SELECTOBJECT: {
                        var idx = @in.ReadWord();
                        state.SelectMetaObject(idx, cb);
                        break;
                    }

                    case META_DELETEOBJECT: {
                        var idx = @in.ReadWord();
                        state.DeleteMetaObject(idx);
                        break;
                    }

                    case META_SAVEDC: {
                        state.SaveState(cb);
                        break;
                    }

                    case META_RESTOREDC: {
                        var idx = @in.ReadShort();
                        state.RestoreState(idx, cb);
                        break;
                    }

                    case META_SETWINDOWORG: {
                        state.SetOffsetWy(@in.ReadShort());
                        state.SetOffsetWx(@in.ReadShort());
                        break;
                    }

                    case META_SETWINDOWEXT: {
                        state.SetExtentWy(@in.ReadShort());
                        state.SetExtentWx(@in.ReadShort());
                        break;
                    }

                    case META_MOVETO: {
                        var y = @in.ReadShort();
                        var p = new Point(@in.ReadShort(), y);
                        state.SetCurrentPoint(p);
                        break;
                    }

                    case META_LINETO: {
                        var y = @in.ReadShort();
                        var x = @in.ReadShort();
                        var p = state.GetCurrentPoint();
                        cb.MoveTo(state.TransformX((int)p.GetX()), state.TransformY((int)p.GetY()));
                        cb.LineTo(state.TransformX(x), state.TransformY(y));
                        cb.Stroke();
                        state.SetCurrentPoint(new Point(x, y));
                        break;
                    }

                    case META_POLYLINE: {
                        state.SetLineJoinPolygon(cb);
                        var len = @in.ReadWord();
                        var x = @in.ReadShort();
                        var y = @in.ReadShort();
                        cb.MoveTo(state.TransformX(x), state.TransformY(y));
                        for (var k = 1; k < len; ++k) {
                            x = @in.ReadShort();
                            y = @in.ReadShort();
                            cb.LineTo(state.TransformX(x), state.TransformY(y));
                        }
                        cb.Stroke();
                        break;
                    }

                    case META_POLYGON: {
                        if (IsNullStrokeFill(false)) {
                            break;
                        }
                        var len = @in.ReadWord();
                        var sx = @in.ReadShort();
                        var sy = @in.ReadShort();
                        cb.MoveTo(state.TransformX(sx), state.TransformY(sy));
                        for (var k = 1; k < len; ++k) {
                            var x = @in.ReadShort();
                            var y = @in.ReadShort();
                            cb.LineTo(state.TransformX(x), state.TransformY(y));
                        }
                        cb.LineTo(state.TransformX(sx), state.TransformY(sy));
                        StrokeAndFill();
                        break;
                    }

                    case META_POLYPOLYGON: {
                        if (IsNullStrokeFill(false)) {
                            break;
                        }
                        var numPoly = @in.ReadWord();
                        var lens = new int[numPoly];
                        for (var k = 0; k < lens.Length; ++k) {
                            lens[k] = @in.ReadWord();
                        }
                        for (var j = 0; j < lens.Length; ++j) {
                            var len = lens[j];
                            var sx = @in.ReadShort();
                            var sy = @in.ReadShort();
                            cb.MoveTo(state.TransformX(sx), state.TransformY(sy));
                            for (var k = 1; k < len; ++k) {
                                var x = @in.ReadShort();
                                var y = @in.ReadShort();
                                cb.LineTo(state.TransformX(x), state.TransformY(y));
                            }
                            cb.LineTo(state.TransformX(sx), state.TransformY(sy));
                        }
                        StrokeAndFill();
                        break;
                    }

                    case META_ELLIPSE: {
                        if (IsNullStrokeFill(state.GetLineNeutral())) {
                            break;
                        }
                        var b = @in.ReadShort();
                        var r = @in.ReadShort();
                        var t = @in.ReadShort();
                        var l = @in.ReadShort();
                        cb.Arc(state.TransformX(l), state.TransformY(b), state.TransformX(r), state.TransformY(t), 0, 360);
                        StrokeAndFill();
                        break;
                    }

                    case META_ARC: {
                        if (IsNullStrokeFill(state.GetLineNeutral())) {
                            break;
                        }
                        var yend = state.TransformY(@in.ReadShort());
                        var xend = state.TransformX(@in.ReadShort());
                        var ystart = state.TransformY(@in.ReadShort());
                        var xstart = state.TransformX(@in.ReadShort());
                        var b = state.TransformY(@in.ReadShort());
                        var r = state.TransformX(@in.ReadShort());
                        var t = state.TransformY(@in.ReadShort());
                        var l = state.TransformX(@in.ReadShort());
                        var cx = (r + l) / 2;
                        var cy = (t + b) / 2;
                        var arc1 = GetArc(cx, cy, xstart, ystart);
                        var arc2 = GetArc(cx, cy, xend, yend);
                        arc2 -= arc1;
                        if (arc2 <= 0) {
                            arc2 += 360;
                        }
                        cb.Arc(l, b, r, t, arc1, arc2);
                        cb.Stroke();
                        break;
                    }

                    case META_PIE: {
                        if (IsNullStrokeFill(state.GetLineNeutral())) {
                            break;
                        }
                        var yend = state.TransformY(@in.ReadShort());
                        var xend = state.TransformX(@in.ReadShort());
                        var ystart = state.TransformY(@in.ReadShort());
                        var xstart = state.TransformX(@in.ReadShort());
                        var b = state.TransformY(@in.ReadShort());
                        var r = state.TransformX(@in.ReadShort());
                        var t = state.TransformY(@in.ReadShort());
                        var l = state.TransformX(@in.ReadShort());
                        var cx = (r + l) / 2;
                        var cy = (t + b) / 2;
                        var arc1 = GetArc(cx, cy, xstart, ystart);
                        var arc2 = GetArc(cx, cy, xend, yend);
                        arc2 -= arc1;
                        if (arc2 <= 0) {
                            arc2 += 360;
                        }
                        var ar = PdfCanvas.BezierArc(l, b, r, t, arc1, arc2);
                        if (ar.Count == 0) {
                            break;
                        }
                        var pt = ar[0];
                        cb.MoveTo(cx, cy);
                        cb.LineTo(pt[0], pt[1]);
                        for (var k = 0; k < ar.Count; ++k) {
                            pt = ar[k];
                            cb.CurveTo(pt[2], pt[3], pt[4], pt[5], pt[6], pt[7]);
                        }
                        cb.LineTo(cx, cy);
                        StrokeAndFill();
                        break;
                    }

                    case META_CHORD: {
                        if (IsNullStrokeFill(state.GetLineNeutral())) {
                            break;
                        }
                        var yend = state.TransformY(@in.ReadShort());
                        var xend = state.TransformX(@in.ReadShort());
                        var ystart = state.TransformY(@in.ReadShort());
                        var xstart = state.TransformX(@in.ReadShort());
                        var b = state.TransformY(@in.ReadShort());
                        var r = state.TransformX(@in.ReadShort());
                        var t = state.TransformY(@in.ReadShort());
                        var l = state.TransformX(@in.ReadShort());
                        var cx = (r + l) / 2;
                        var cy = (t + b) / 2;
                        var arc1 = GetArc(cx, cy, xstart, ystart);
                        var arc2 = GetArc(cx, cy, xend, yend);
                        arc2 -= arc1;
                        if (arc2 <= 0) {
                            arc2 += 360;
                        }
                        var ar = PdfCanvas.BezierArc(l, b, r, t, arc1, arc2);
                        if (ar.Count == 0) {
                            break;
                        }
                        var pt = ar[0];
                        cx = (float)pt[0];
                        cy = (float)pt[1];
                        cb.MoveTo(cx, cy);
                        for (var k = 0; k < ar.Count; ++k) {
                            pt = ar[k];
                            cb.CurveTo(pt[2], pt[3], pt[4], pt[5], pt[6], pt[7]);
                        }
                        cb.LineTo(cx, cy);
                        StrokeAndFill();
                        break;
                    }

                    case META_RECTANGLE: {
                        if (IsNullStrokeFill(true)) {
                            break;
                        }
                        var b = state.TransformY(@in.ReadShort());
                        var r = state.TransformX(@in.ReadShort());
                        var t = state.TransformY(@in.ReadShort());
                        var l = state.TransformX(@in.ReadShort());
                        cb.Rectangle(l, b, r - l, t - b);
                        StrokeAndFill();
                        break;
                    }

                    case META_ROUNDRECT: {
                        if (IsNullStrokeFill(true)) {
                            break;
                        }
                        var h = state.TransformY(0) - state.TransformY(@in.ReadShort());
                        var w = state.TransformX(@in.ReadShort()) - state.TransformX(0);
                        var b = state.TransformY(@in.ReadShort());
                        var r = state.TransformX(@in.ReadShort());
                        var t = state.TransformY(@in.ReadShort());
                        var l = state.TransformX(@in.ReadShort());
                        cb.RoundRectangle(l, b, r - l, t - b, (h + w) / 4);
                        StrokeAndFill();
                        break;
                    }

                    case META_INTERSECTCLIPRECT: {
                        var b = state.TransformY(@in.ReadShort());
                        var r = state.TransformX(@in.ReadShort());
                        var t = state.TransformY(@in.ReadShort());
                        var l = state.TransformX(@in.ReadShort());
                        cb.Rectangle(l, b, r - l, t - b);
                        cb.EoClip();
                        cb.EndPath();
                        break;
                    }

                    case META_EXTTEXTOUT: {
                        var y = @in.ReadShort();
                        var x = @in.ReadShort();
                        var count = @in.ReadWord();
                        var flag = @in.ReadWord();
                        var x1 = 0;
                        var y1 = 0;
                        var x2 = 0;
                        var y2 = 0;
                        if ((flag & (MetaFont.ETO_CLIPPED | MetaFont.ETO_OPAQUE)) != 0) {
                            x1 = @in.ReadShort();
                            y1 = @in.ReadShort();
                            x2 = @in.ReadShort();
                            y2 = @in.ReadShort();
                        }
                        var text = new byte[count];
                        int k;
                        for (k = 0; k < count; ++k) {
                            var c = (byte)@in.ReadByte();
                            if (c == 0) {
                                break;
                            }
                            text[k] = c;
                        }
                        string s;
                        try {
                            s = JavaUtil.GetStringForBytes(text, 0, k, "Cp1252");
                        }
                        catch (ArgumentException) {
                            s = JavaUtil.GetStringForBytes(text, 0, k);
                        }
                        OutputText(x, y, flag, x1, y1, x2, y2, s);
                        break;
                    }

                    case META_TEXTOUT: {
                        var count = @in.ReadWord();
                        var text = new byte[count];
                        int k;
                        for (k = 0; k < count; ++k) {
                            var c = (byte)@in.ReadByte();
                            if (c == 0) {
                                break;
                            }
                            text[k] = c;
                        }
                        string s;
                        try {
                            s = JavaUtil.GetStringForBytes(text, 0, k, "Cp1252");
                        }
                        catch (ArgumentException) {
                            s = JavaUtil.GetStringForBytes(text, 0, k);
                        }
                        count = count + 1 & 0xfffe;
                        @in.Skip(count - k);
                        var y = @in.ReadShort();
                        var x = @in.ReadShort();
                        OutputText(x, y, 0, 0, 0, 0, 0, s);
                        break;
                    }

                    case META_SETBKCOLOR: {
                        state.SetCurrentBackgroundColor(@in.ReadColor());
                        break;
                    }

                    case META_SETTEXTCOLOR: {
                        state.SetCurrentTextColor(@in.ReadColor());
                        break;
                    }

                    case META_SETTEXTALIGN: {
                        state.SetTextAlign(@in.ReadWord());
                        break;
                    }

                    case META_SETBKMODE: {
                        state.SetBackgroundMode(@in.ReadWord());
                        break;
                    }

                    case META_SETPOLYFILLMODE: {
                        state.SetPolyFillMode(@in.ReadWord());
                        break;
                    }

                    case META_SETPIXEL: {
                        var color = @in.ReadColor();
                        var y = @in.ReadShort();
                        var x = @in.ReadShort();
                        cb.SaveState();
                        cb.SetFillColor(color);
                        cb.Rectangle(state.TransformX(x), state.TransformY(y), .2f, .2f);
                        cb.Fill();
                        cb.RestoreState();
                        break;
                    }

                    case META_DIBSTRETCHBLT:
                    case META_STRETCHDIB: {
                        var rop = @in.ReadInt();
                        if (function == META_STRETCHDIB) {
                            /*int usage = */
                            @in.ReadWord();
                        }
                        var srcHeight = @in.ReadShort();
                        var srcWidth = @in.ReadShort();
                        var ySrc = @in.ReadShort();
                        var xSrc = @in.ReadShort();
                        var destHeight = state.TransformY(@in.ReadShort()) - state.TransformY(0);
                        var destWidth = state.TransformX(@in.ReadShort()) - state.TransformX(0);
                        var yDest = state.TransformY(@in.ReadShort());
                        var xDest = state.TransformX(@in.ReadShort());
                        var b = new byte[tsize * 2 - (@in.GetLength() - lenMarker)];
                        for (var k = 0; k < b.Length; ++k) {
                            b[k] = (byte)@in.ReadByte();
                        }
                        try {
                            cb.SaveState();
                            cb.Rectangle(xDest, yDest, destWidth, destHeight);
                            cb.Clip();
                            cb.EndPath();
                            var bmpImage = ImageDataFactory.CreateBmp(b, true);
                            var imageXObject = new PdfImageXObject(bmpImage);
                            var width = destWidth * bmpImage.GetWidth() / srcWidth;
                            var height = -destHeight * bmpImage.GetHeight() / srcHeight;
                            var x = xDest - destWidth * xSrc / srcWidth;
                            var y = yDest + destHeight * ySrc / srcHeight - height;
                            cb.AddXObject(imageXObject, new Rectangle(x, y, width, height));
                            cb.RestoreState();
                        }
                        catch (Exception) {
                        }
                        // empty on purpose
                        break;
                    }
                }
                @in.Skip(tsize * 2 - (@in.GetLength() - lenMarker));
            }
            state.Cleanup(cb);
        }

        /// <summary>Output Text at a certain x and y coordinate.</summary>
        /// <remarks>Output Text at a certain x and y coordinate. Clipped or opaque text isn't supported as of yet.</remarks>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <param name="flag">flag indicating clipped or opaque</param>
        /// <param name="x1">x1-coordinate of the rectangle if clipped or opaque</param>
        /// <param name="y1">y1-coordinate of the rectangle if clipped or opaque</param>
        /// <param name="x2">x2-coordinate of the rectangle if clipped or opaque</param>
        /// <param name="y2">y1-coordinate of the rectangle if clipped or opaque</param>
        /// <param name="text">text to output</param>
        public virtual void OutputText(int x, int y, int flag, int x1, int y1, int x2, int y2, string text) {
            var font = state.GetCurrentFont();
            var refX = state.TransformX(x);
            var refY = state.TransformY(y);
            var angle = state.TransformAngle(font.GetAngle());
            var sin = (float)Math.Sin(angle);
            var cos = (float)Math.Cos(angle);
            var fontSize = font.GetFontSize(state);
            var fp = font.GetFont();
            var align = state.GetTextAlign();
            // NOTE, MetaFont always creates with CP1252 encoding.
            var normalizedWidth = 0;
            var bytes = font.encoding.ConvertToBytes(text);
            foreach (var b in bytes) {
                normalizedWidth += fp.GetWidth(0xff & b);
            }
            var textWidth = fontSize / FontProgram.UNITS_NORMALIZATION * normalizedWidth;
            float tx = 0;
            float ty = 0;
            float descender = fp.GetFontMetrics().GetTypoDescender();
            float ury = fp.GetFontMetrics().GetBbox()[3];
            cb.SaveState();
            cb.ConcatMatrix(cos, sin, -sin, cos, refX, refY);
            if ((align & MetaState.TA_CENTER) == MetaState.TA_CENTER) {
                tx = -textWidth / 2;
            }
            else {
                if ((align & MetaState.TA_RIGHT) == MetaState.TA_RIGHT) {
                    tx = -textWidth;
                }
            }
            if ((align & MetaState.TA_BASELINE) == MetaState.TA_BASELINE) {
                ty = 0;
            }
            else {
                if ((align & MetaState.TA_BOTTOM) == MetaState.TA_BOTTOM) {
                    ty = -descender;
                }
                else {
                    ty = -ury;
                }
            }
            Color textColor;
            if (state.GetBackgroundMode() == MetaState.OPAQUE) {
                textColor = state.GetCurrentBackgroundColor();
                cb.SetFillColor(textColor);
                cb.Rectangle(tx, ty + descender, textWidth, ury - descender);
                cb.Fill();
            }
            textColor = state.GetCurrentTextColor();
            cb.SetFillColor(textColor);
            cb.BeginText();
            cb.SetFontAndSize(PdfFontFactory.CreateFont(state.GetCurrentFont().GetFont(), PdfEncodings.CP1252, PdfFontFactory.EmbeddingStrategy
                .PREFER_EMBEDDED), fontSize);
            cb.SetTextMatrix(tx, ty);
            cb.ShowText(text);
            cb.EndText();
            if (font.IsUnderline()) {
                cb.Rectangle(tx, ty - fontSize / 4, textWidth, fontSize / 15);
                cb.Fill();
            }
            if (font.IsStrikeout()) {
                cb.Rectangle(tx, ty + fontSize / 3, textWidth, fontSize / 15);
                cb.Fill();
            }
            cb.RestoreState();
        }

        /// <summary>Return true if the pen style is null and if it isn't a brush.</summary>
        /// <param name="isRectangle">
        /// value to decide how to change the state. If true state.setLineJoinRectangle(cb) is called,
        /// if false state.setLineJoinPolygon(cb) is called.
        /// </param>
        /// <returns>true if the pen style is null and if it isn't a brush</returns>
        public virtual bool IsNullStrokeFill(bool isRectangle) {
            var pen = state.GetCurrentPen();
            var brush = state.GetCurrentBrush();
            var noPen = pen.GetStyle() == MetaPen.PS_NULL;
            var style = brush.GetStyle();
            var isBrush = style == MetaBrush.BS_SOLID || style == MetaBrush.BS_HATCHED && state.GetBackgroundMode() ==
                 MetaState.OPAQUE;
            var result = noPen && !isBrush;
            if (!noPen) {
                if (isRectangle) {
                    state.SetLineJoinRectangle(cb);
                }
                else {
                    state.SetLineJoinPolygon(cb);
                }
            }
            return result;
        }

        /// <summary>Stroke and fill the MetaPen and MetaBrush paths.</summary>
        public virtual void StrokeAndFill() {
            var pen = state.GetCurrentPen();
            var brush = state.GetCurrentBrush();
            var penStyle = pen.GetStyle();
            var brushStyle = brush.GetStyle();
            if (penStyle == MetaPen.PS_NULL) {
                cb.ClosePath();
                if (state.GetPolyFillMode() == MetaState.ALTERNATE) {
                    cb.EoFill();
                }
                else {
                    cb.Fill();
                }
            }
            else {
                var isBrush = brushStyle == MetaBrush.BS_SOLID || brushStyle == MetaBrush.BS_HATCHED && state.GetBackgroundMode
                    () == MetaState.OPAQUE;
                if (isBrush) {
                    if (state.GetPolyFillMode() == MetaState.ALTERNATE) {
                        cb.ClosePathEoFillStroke();
                    }
                    else {
                        cb.ClosePathFillStroke();
                    }
                }
                else {
                    cb.ClosePathStroke();
                }
            }
        }

        internal static float GetArc(float xCenter, float yCenter, float xDot, float yDot) {
            var s = Math.Atan2(yDot - yCenter, xDot - xCenter);
            if (s < 0) {
                s += Math.PI * 2;
            }
            return (float)(s / Math.PI * 180);
        }

        /// <summary>Wrap a BMP image in an WMF.</summary>
        /// <param name="image">the BMP image to be wrapped</param>
        /// <returns>the wrapped BMP</returns>
        public static byte[] WrapBMP(ImageData image) {
            if (image.GetOriginalType() != ImageType.BMP) {
                throw new PdfException(PdfException.OnlyBmpCanBeWrappedInWmf);
            }
            Stream imgIn;
            byte[] data;
            if (image.GetData() == null) {
                imgIn = UrlUtil.OpenStream(image.GetUrl());
                var @out = new MemoryStream();
                var b = 0;
                while ((b = imgIn.CustomRead()) != -1) {
                    @out.CustomWrite(b);
                }
                imgIn.Dispose();
                data = @out.ToArray();
            }
            else {
                data = image.GetData();
            }
            var sizeBmpWords = (int)(((uint)data.Length - 14 + 1) >> 1);
            var os = new MemoryStream();
            // write metafile header
            WriteWord(os, 1);
            WriteWord(os, 9);
            WriteWord(os, 0x0300);
            // total metafile size
            WriteDWord(os, 9 + 4 + 5 + 5 + 13 + sizeBmpWords + 3);
            WriteWord(os, 1);
            // max record size
            WriteDWord(os, 14 + sizeBmpWords);
            WriteWord(os, 0);
            // write records
            WriteDWord(os, 4);
            WriteWord(os, META_SETMAPMODE);
            WriteWord(os, 8);
            WriteDWord(os, 5);
            WriteWord(os, META_SETWINDOWORG);
            WriteWord(os, 0);
            WriteWord(os, 0);
            WriteDWord(os, 5);
            WriteWord(os, META_SETWINDOWEXT);
            WriteWord(os, (int)image.GetHeight());
            WriteWord(os, (int)image.GetWidth());
            WriteDWord(os, 13 + sizeBmpWords);
            WriteWord(os, META_DIBSTRETCHBLT);
            WriteDWord(os, 0x00cc0020);
            WriteWord(os, (int)image.GetHeight());
            WriteWord(os, (int)image.GetWidth());
            WriteWord(os, 0);
            WriteWord(os, 0);
            WriteWord(os, (int)image.GetHeight());
            WriteWord(os, (int)image.GetWidth());
            WriteWord(os, 0);
            WriteWord(os, 0);
            os.Write(data, 14, data.Length - 14);
            if ((data.Length & 1) == 1) {
                os.CustomWrite(0);
            }
            WriteDWord(os, 3);
            WriteWord(os, 0);
            os.Dispose();
            return os.ToArray();
        }

        /// <summary>Writes the specified value to the specified outputstream as a word.</summary>
        /// <param name="os">outputstream to write the word to</param>
        /// <param name="v">value to be written</param>
        public static void WriteWord(Stream os, int v) {
            os.CustomWrite(v & 0xff);
            os.CustomWrite((int)(((uint)v) >> 8) & 0xff);
        }

        /// <summary>Writes the specified value to the specified outputstream as a dword.</summary>
        /// <param name="os">outputstream to write the dword to</param>
        /// <param name="v">value to be written</param>
        public static void WriteDWord(Stream os, int v) {
            WriteWord(os, v & 0xffff);
            WriteWord(os, (int)(((uint)v) >> 16) & 0xffff);
        }
    }
}
