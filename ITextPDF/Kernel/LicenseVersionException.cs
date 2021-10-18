/*
This file is part of the iText (R) project.
Copyright (c) 1998-2021 iText Group NV
Authors: iText Software.

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
using IText.IO.Util;

namespace IText.Kernel {
    /// <summary>Exception class for License-key version exceptions throw in the Version class</summary>
    public class LicenseVersionException : Exception {
        public const string NO_I_TEXT7_LICENSE_IS_LOADED_BUT_AN_I_TEXT5_LICENSE_IS_LOADED = "No iText7 License is loaded but an iText5 license is loaded.";

        public const string THE_MAJOR_VERSION_OF_THE_LICENSE_0_IS_LOWER_THAN_THE_MAJOR_VERSION_1_OF_THE_CORE_LIBRARY
             = "The major version of the license ({0}) is lower than the major version ({1}) of the Core library.";

        public const string THE_MAJOR_VERSION_OF_THE_LICENSE_0_IS_HIGHER_THAN_THE_MAJOR_VERSION_1_OF_THE_CORE_LIBRARY
             = "The major version of the license ({0}) is higher than the major version ({1}) of the Core library.";

        public const string THE_MINOR_VERSION_OF_THE_LICENSE_0_IS_LOWER_THAN_THE_MINOR_VERSION_1_OF_THE_CORE_LIBRARY
             = "The minor version of the license ({0}) is lower than the minor version ({1}) of the Core library.";

        public const string THE_MINOR_VERSION_OF_THE_LICENSE_0_IS_HIGHER_THAN_THE_MINOR_VERSION_1_OF_THE_CORE_LIBRARY
             = "The minor version of the license ({0}) is higher than the minor version ({1}) of the Core library.";

        public const string VERSION_STRING_IS_EMPTY_AND_CANNOT_BE_PARSED = "Version string is empty and cannot be parsed.";

        public const string MAJOR_VERSION_IS_NOT_NUMERIC = "Major version is not numeric";

        public const string MINOR_VERSION_IS_NOT_NUMERIC = "Minor version is not numeric";

        public const string UNKNOWN_EXCEPTION_WHEN_CHECKING_LICENSE_VERSION = "Unknown Exception when checking License version";

        public const string LICENSE_FILE_NOT_LOADED = "License file not loaded.";

        /// <summary>Object for more details</summary>
        protected internal object @object;

        private IList<object> messageParams;

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="message">the detail message.</param>
        public LicenseVersionException(string message)
            : base(message) {
        }

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="cause">
        /// the cause (which is saved for later retrieval by
        /// <see cref="System.Exception.InnerException()"/>
        /// method).
        /// </param>
        public LicenseVersionException(Exception cause)
            : this(UNKNOWN_EXCEPTION_WHEN_CHECKING_LICENSE_VERSION, cause) {
        }

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="message">the detail message.</param>
        /// <param name="obj">an object for more details.</param>
        public LicenseVersionException(string message, object obj)
            : this(message) {
            @object = obj;
        }

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="message">the detail message.</param>
        /// <param name="cause">
        /// the cause (which is saved for later retrieval by
        /// <see cref="System.Exception.InnerException()"/>
        /// method).
        /// </param>
        public LicenseVersionException(string message, Exception cause)
            : base(message, cause) {
        }

        /// <summary>Creates a new instance of PdfException.</summary>
        /// <param name="message">the detail message.</param>
        /// <param name="cause">
        /// the cause (which is saved for later retrieval by
        /// <see cref="System.Exception.InnerException()"/>
        /// method).
        /// </param>
        /// <param name="obj">an object for more details.</param>
        public LicenseVersionException(string message, Exception cause, object obj)
            : this(message, cause) {
            @object = obj;
        }

        public override string Message {
            get
            {
	            if (messageParams == null || messageParams.Count == 0) {
                    return base.Message;
                }

	            return MessageFormatUtil.Format(base.Message, GetMessageParams());
            }
        }

        /// <summary>Sets additional params for Exception message.</summary>
        /// <param name="messageParams">additional params.</param>
        /// <returns>object itself.</returns>
        public virtual LicenseVersionException SetMessageParams(params object[] messageParams) {
            this.messageParams = new List<object>();
            this.messageParams.AddAll(messageParams);
            return this;
        }

        /// <summary>Gets parameters that are to be inserted in exception message placeholders.</summary>
        /// <remarks>
        /// Gets parameters that are to be inserted in exception message placeholders.
        /// Placeholder format is defined similar to the following: "{0}".
        /// </remarks>
        /// <returns>params for exception message.</returns>
        protected internal virtual object[] GetMessageParams() {
            var parameters = new object[messageParams.Count];
            for (var i = 0; i < messageParams.Count; i++) {
                parameters[i] = messageParams[i];
            }
            return parameters;
        }
    }
}
