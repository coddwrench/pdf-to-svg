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

namespace  IText.IO.Image {
    public class BmpImageData : RawImageData {
        private int size;

        private bool noHeader;

        /// <summary>
        /// Creates instance of
        /// <see cref="BmpImageData"/>
        /// </summary>
        /// <param name="url">url of the image</param>
        /// <param name="noHeader">indicates that the source image does not have a header</param>
        protected internal BmpImageData(Uri url, bool noHeader)
            : base(url, ImageType.BMP) {
            this.noHeader = noHeader;
        }

        /// <summary>
        /// Creates instance of
        /// <see cref="BmpImageData"/>
        /// </summary>
        /// <param name="url">url of the image</param>
        /// <param name="noHeader">indicates that the source image does not have a header</param>
        /// <param name="size">the size of the image (length of the byte array)</param>
        [Obsolete(@"will be removed in 7.2")]
        protected internal BmpImageData(Uri url, bool noHeader, int size)
            : this(url, noHeader) {
            this.size = size;
        }

        /// <summary>
        /// Creates instance of
        /// <see cref="BmpImageData"/>
        /// </summary>
        /// <param name="bytes">contents of the image</param>
        /// <param name="noHeader">indicates that the source image does not have a header</param>
        protected internal BmpImageData(byte[] bytes, bool noHeader)
            : base(bytes, ImageType.BMP) {
            this.noHeader = noHeader;
        }

        /// <summary>
        /// Creates instance of
        /// <see cref="BmpImageData"/>
        /// </summary>
        /// <param name="bytes">contents of the image</param>
        /// <param name="noHeader">indicates that the source image does not have a header</param>
        /// <param name="size">the size of the image (length of the byte array)</param>
        [Obsolete(@"will be removed in 7.2")]
        protected internal BmpImageData(byte[] bytes, bool noHeader, int size)
            : this(bytes, noHeader) {
            this.size = size;
        }

        /// <returns>size of the image</returns>
        [Obsolete(@"will be removed in 7.2")]
        public virtual int GetSize() {
            return size;
        }

        /// <returns>True if the bitmap image does not contain a header</returns>
        public virtual bool IsNoHeader() {
            return noHeader;
        }
    }
}
