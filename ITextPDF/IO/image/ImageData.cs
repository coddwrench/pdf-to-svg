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
using IText.IO.Colors;
using IText.IO.Source;
using IText.IO.Util;
using IText.Logger;

namespace  IText.IO.Image {
    public abstract class ImageData {
        /// <summary>a static that is used for attributing a unique id to each image.</summary>
        private static long serialId;

        private static readonly object staticLock = new object();

        protected internal Uri url;

        protected internal int[] transparency;

        protected internal ImageType originalType;

        protected internal float width;

        protected internal float height;

        protected internal byte[] data;

        protected internal int imageSize;

        protected internal int bpc = 1;

        /// <summary>is the number of components used to encode colorspace, not actual colorspace.</summary>
        protected internal int colorSpace = -1;

        protected internal float[] decode;

        protected internal IDictionary<string, object> decodeParms;

        protected internal bool inverted;

        protected internal float rotation;

        protected internal IccProfile profile;

        protected internal int dpiX;

        protected internal int dpiY;

        protected internal int colorTransform = 1;

        protected internal bool deflated;

        protected internal bool mask;

        protected internal ImageData imageMask;

        protected internal bool interpolation;

        protected internal float XYRatio;

        protected internal string filter;

        protected internal IDictionary<string, object> imageAttributes;

        protected internal long? mySerialId = GetSerialId();

        protected internal ImageData(Uri url, ImageType type) {
            this.url = url;
            originalType = type;
        }

        protected internal ImageData(byte[] bytes, ImageType type) {
            data = bytes;
            originalType = type;
        }

        public virtual bool IsRawImage() {
            return false;
        }

        public virtual Uri GetUrl() {
            return url;
        }

        public virtual void SetUrl(Uri url) {
            this.url = url;
        }

        public virtual int[] GetTransparency() {
            return transparency;
        }

        public virtual void SetTransparency(int[] transparency) {
            this.transparency = transparency;
        }

        public virtual bool IsInverted() {
            return inverted;
        }

        public virtual void SetInverted(bool inverted) {
            this.inverted = inverted;
        }

        public virtual float GetRotation() {
            return rotation;
        }

        public virtual void SetRotation(float rotation) {
            this.rotation = rotation;
        }

        public virtual IccProfile GetProfile() {
            return profile;
        }

        public virtual void SetProfile(IccProfile profile) {
            this.profile = profile;
        }

        public virtual int GetDpiX() {
            return dpiX;
        }

        public virtual int GetDpiY() {
            return dpiY;
        }

        public virtual void SetDpi(int dpiX, int dpiY) {
            this.dpiX = dpiX;
            this.dpiY = dpiY;
        }

        public virtual int GetColorTransform() {
            return colorTransform;
        }

        public virtual void SetColorTransform(int colorTransform) {
            this.colorTransform = colorTransform;
        }

        public virtual bool IsDeflated() {
            return deflated;
        }

        public virtual void SetDeflated(bool deflated) {
            this.deflated = deflated;
        }

        public virtual ImageType GetOriginalType() {
            return originalType;
        }

        /// <summary>Gets the number of components used to encode colorspace, not actual colorspace.</summary>
        /// <returns>the number of components used to encode colorspace</returns>
        public virtual int GetColorSpace() {
            return colorSpace;
        }

        public virtual void SetColorSpace(int colorSpace) {
            this.colorSpace = colorSpace;
        }

        public virtual byte[] GetData() {
            return data;
        }

        public virtual bool CanBeMask() {
            if (IsRawImage()) {
                if (bpc > 0xff) {
                    return true;
                }
            }
            return colorSpace == 1;
        }

        public virtual bool IsMask() {
            return mask;
        }

        public virtual ImageData GetImageMask() {
            return imageMask;
        }

        public virtual void SetImageMask(ImageData imageMask) {
            if (mask) {
                throw new IOException(IOException.ImageMaskCannotContainAnotherImageMask);
            }
            if (!imageMask.mask) {
                throw new IOException(IOException.ImageIsNotMaskYouMustCallImageDataMakeMask);
            }
            this.imageMask = imageMask;
        }

        public virtual bool IsSoftMask() {
            return mask && bpc > 1 && bpc <= 8;
        }

        public virtual void MakeMask() {
            if (!CanBeMask()) {
                throw new IOException(IOException.ThisImageCanNotBeAnImageMask);
            }
            mask = true;
        }

        public virtual float GetWidth() {
            return width;
        }

        public virtual void SetWidth(float width) {
            this.width = width;
        }

        public virtual float GetHeight() {
            return height;
        }

        public virtual void SetHeight(float height) {
            this.height = height;
        }

        public virtual int GetBpc() {
            return bpc;
        }

        public virtual void SetBpc(int bpc) {
            this.bpc = bpc;
        }

        public virtual bool IsInterpolation() {
            return interpolation;
        }

        public virtual void SetInterpolation(bool interpolation) {
            this.interpolation = interpolation;
        }

        public virtual float GetXYRatio() {
            return XYRatio;
        }

        public virtual void SetXYRatio(float XYRatio) {
            this.XYRatio = XYRatio;
        }

        public virtual IDictionary<string, object> GetImageAttributes() {
            return imageAttributes;
        }

        public virtual void SetImageAttributes(IDictionary<string, object> imageAttributes) {
            this.imageAttributes = imageAttributes;
        }

        public virtual string GetFilter() {
            return filter;
        }

        public virtual void SetFilter(string filter) {
            this.filter = filter;
        }

        public virtual IDictionary<string, object> GetDecodeParms() {
            return decodeParms;
        }

        public virtual float[] GetDecode() {
            return decode;
        }

        public virtual void SetDecode(float[] decode) {
            this.decode = decode;
        }

        /// <summary>Checks if image can be inline</summary>
        /// <returns>if the image can be inline</returns>
        public virtual bool CanImageBeInline() {
            var logger = LogManager.GetLogger(typeof(ImageData));
            if (imageSize > 4096) {
                logger.Warn(LogMessageConstant.IMAGE_SIZE_CANNOT_BE_MORE_4KB);
                return false;
            }
            if (imageMask != null) {
                logger.Warn(LogMessageConstant.IMAGE_HAS_MASK);
                return false;
            }
            return true;
        }

        /// <summary>Load data from URL.</summary>
        /// <remarks>
        /// Load data from URL. url must be not null.
        /// Note, this method doesn't check if data or url is null.
        /// </remarks>
        internal virtual void LoadData() {
            var raf = new RandomAccessFileOrArray(new RandomAccessSourceFactory().CreateSource(url
                ));
            var stream = new ByteArrayOutputStream();
            StreamUtil.TransferBytes(raf, stream);
            raf.Close();
            data = stream.ToArray();
        }

        /// <summary>Creates a new serial id.</summary>
        /// <returns>the new serialId</returns>
        private static long? GetSerialId() {
            lock (staticLock) {
                return ++serialId;
            }
        }
    }
}
