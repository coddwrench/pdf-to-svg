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
using IText.IO.Image;
using IText.IO.Util;
using IText.Kernel.Geom;
using IText.Kernel.Pdf.Xobject;
using IOException = IText.IO.IOException;

namespace IText.Kernel.Pdf.Canvas.Wmf {
    /// <summary>Helper class for the WmfImage implementation.</summary>
    /// <remarks>
    /// Helper class for the WmfImage implementation. Assists in the creation of a
    /// <see cref="PdfFormXObject"/>.
    /// </remarks>
    public class WmfImageHelper {
        /// <summary>Scales the WMF font size.</summary>
        /// <remarks>Scales the WMF font size. The default value is 0.86.</remarks>
        public static float wmfFontCorrection = 0.86f;

        private WmfImageData wmf;

        private float plainWidth;

        private float plainHeight;

        /// <summary>Creates a helper instance.</summary>
        /// <param name="wmf">
        /// the
        /// <see cref="WmfImageData"/>
        /// object
        /// </param>
        public WmfImageHelper(ImageData wmf) {
            if (wmf.GetOriginalType() != ImageType.WMF) {
                throw new ArgumentException("WMF image expected");
            }
            this.wmf = (WmfImageData)wmf;
            ProcessParameters();
        }

        /// <summary>This method checks if the image is a valid WMF and processes some parameters.</summary>
        private void ProcessParameters() {
            Stream @is = null;
            try {
                string errorID;
                if (wmf.GetData() == null) {
                    @is = UrlUtil.OpenStream(wmf.GetUrl());
                    errorID = wmf.GetUrl().ToString();
                }
                else {
                    @is = new MemoryStream(wmf.GetData());
                    errorID = "Byte array";
                }
                var @in = new InputMeta(@is);
                if (@in.ReadInt() != unchecked((int)(0x9AC6CDD7))) {
                    throw new PdfException(PdfException._1IsNotAValidPlaceableWindowsMetafile, errorID);
                }
                @in.ReadWord();
                var left = @in.ReadShort();
                var top = @in.ReadShort();
                var right = @in.ReadShort();
                var bottom = @in.ReadShort();
                var inch = @in.ReadWord();
                wmf.SetDpi(72, 72);
                wmf.SetHeight((float)(bottom - top) / inch * 72f);
                wmf.SetWidth((float)(right - left) / inch * 72f);
            }
            catch (IOException) {
                throw new PdfException(PdfException.WmfImageException);
            }
            finally {
                if (@is != null) {
                    try {
                        @is.Dispose();
                    }
                    catch (IOException) {
                    }
                }
            }
        }

        /// <summary>Create a PdfXObject based on the WMF image.</summary>
        /// <remarks>
        /// Create a PdfXObject based on the WMF image. The PdfXObject will have the dimensions of the
        /// WMF image.
        /// </remarks>
        /// <param name="document">PdfDocument to add the PdfXObject to</param>
        /// <returns>PdfXObject based on the WMF image</returns>
        public virtual PdfXObject CreateFormXObject(PdfDocument document) {
            var pdfForm = new PdfFormXObject(new Rectangle(0, 0, wmf.GetWidth(), wmf.GetHeight()));
            var canvas = new PdfCanvas(pdfForm, document);
            Stream @is = null;
            try {
                if (wmf.GetData() == null) {
                    @is = UrlUtil.OpenStream(wmf.GetUrl());
                }
                else {
                    @is = new MemoryStream(wmf.GetData());
                }
                var meta = new MetaDo(@is, canvas);
                meta.ReadAll();
            }
            catch (IOException e) {
                throw new PdfException(PdfException.WmfImageException, e);
            }
            finally {
                if (@is != null) {
                    try {
                        @is.Dispose();
                    }
                    catch (IOException) {
                    }
                }
            }
            return pdfForm;
        }
    }
}
