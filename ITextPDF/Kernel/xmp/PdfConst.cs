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

namespace IText.Kernel.XMP {
    /// <summary>Class that contains several constants.</summary>
    public class PdfConst {
        /// <summary>External Contributors to the resource (other than the authors).</summary>
        public const string Contributor = "contributor";

        /// <summary>The extent or scope of the resource.</summary>
        public const string Coverage = "coverage";

        /// <summary>The authors of the resource (listed in order of precedence, if significant).</summary>
        public const string Creator = "creator";

        /// <summary>Date(s) that something interesting happened to the resource.</summary>
        public const string Date = "date";

        /// <summary>A textual description of the content of the resource.</summary>
        /// <remarks>A textual description of the content of the resource. Multiple values may be present for different languages.
        ///     </remarks>
        public const string Description = "description";

        /// <summary>The file format used when saving the resource.</summary>
        /// <remarks>The file format used when saving the resource. Tools and applications should set this property to the save format of the data. It may include appropriate qualifiers.
        ///     </remarks>
        public const string Format = "format";

        /// <summary>An unordered array of text strings that unambiguously identify the resource within a given context.
        ///     </summary>
        public const string Identifier = "identifier";

        /// <summary>An unordered array specifying the languages used in the	resource.</summary>
        public const string Language = "language";

        /// <summary>Publishers.</summary>
        public const string Publisher = "publisher";

        /// <summary>Relationships to other documents.</summary>
        public const string Relation = "relation";

        /// <summary>Informal rights statement, selected by language.</summary>
        public const string Rights = "rights";

        /// <summary>Unique identifier of the work from which this resource was derived.</summary>
        public const string Source = "source";

        /// <summary>An unordered array of descriptive phrases or keywords that specify the topic of the content of the resource.
        ///     </summary>
        public const string Subject = "subject";

        /// <summary>The title of the document, or the name given to the resource.</summary>
        /// <remarks>The title of the document, or the name given to the resource. Typically, it will be a name by which the resource is formally known.
        ///     </remarks>
        public const string Title = "title";

        /// <summary>A document type; for example, novel, poem, or working paper.</summary>
        public const string Type = "type";

        /// <summary>Keywords.</summary>
        public const string Keywords = "Keywords";

        /// <summary>The PDF file version (for example: 1.0, 1.3, and so on).</summary>
        public const string Version = "PDFVersion";

        /// <summary>The Producer.</summary>
        public const string Producer = "Producer";

        /// <summary>The part</summary>
        public const string Part = "part";

        /// <summary>An unordered array specifying properties that were edited outside the authoring application.</summary>
        /// <remarks>An unordered array specifying properties that were edited outside the authoring application. Each item should contain a single namespace and XPath separated by one ASCII space (U+0020).
        ///     </remarks>
        public const string Advisory = "Advisory";

        /// <summary>The base URL for relative URLs in the document content.</summary>
        /// <remarks>The base URL for relative URLs in the document content. If this document contains Internet links, and those links are relative, they are relative to this base URL. This property provides a standard way for embedded relative URLs to be interpreted by tools. Web authoring tools should set the value based on their notion of where URLs will be interpreted.
        ///     </remarks>
        public const string BaseURL = "BaseURL";

        /// <summary>The date and time the resource was originally created.</summary>
        public const string CreateDate = "CreateDate";

        /// <summary>The name of the first known tool used to create the resource.</summary>
        /// <remarks>The name of the first known tool used to create the resource. If history is present in the metadata, this value should be equivalent to that of xmpMM:History's softwareAgent property.
        ///     </remarks>
        public const string CreatorTool = "CreatorTool";

        /// <summary>The date and time that any metadata for this resource was last changed.</summary>
        public const string MetadataDate = "MetadataDate";

        /// <summary>The date and time the resource was last modified.</summary>
        public const string ModifyDate = "ModifyDate";

        /// <summary>A short informal name for the resource.</summary>
        public const string Nickname = "Nickname";

        /// <summary>An alternative array of thumbnail images for a file, which can differ in characteristics such as size or image encoding.
        ///     </summary>
        public const string Thumbnails = "Thumbnails";

        /// <summary>Indicates whether the document has been modified to include trapping information</summary>
        public const string Trapped = "Trapped";
    }
}
