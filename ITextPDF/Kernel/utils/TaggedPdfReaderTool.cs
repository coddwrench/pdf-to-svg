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
using System.IO;
using System.Text;
using IText.IO.Util;
using IText.Kernel.Pdf;
using IText.Kernel.Pdf.Canvas.Parser;
using IText.Kernel.Pdf.Canvas.Parser.Data;
using IText.Kernel.Pdf.Canvas.Parser.Listener;
using IText.Kernel.Pdf.Tagging;
using IOException = IText.IO.IOException;

namespace IText.Kernel.Utils {
    /// <summary>Converts a tagged PDF document into an XML file.</summary>
    public class TaggedPdfReaderTool {
        protected internal PdfDocument document;

        protected internal StreamWriter @out;

        protected internal string rootTag;

        // key - page dictionary; value - a mapping of mcids to text in them
        protected internal IDictionary<PdfDictionary, IDictionary<int, string>> parsedTags = new Dictionary<PdfDictionary
            , IDictionary<int, string>>();

        /// <summary>
        /// Constructs a
        /// <see cref="TaggedPdfReaderTool"/>
        /// via a given
        /// <see cref="PdfDocument"/>.
        /// </summary>
        /// <param name="document">the document to read tag structure from</param>
        public TaggedPdfReaderTool(PdfDocument document) {
            this.document = document;
        }

        /// <summary>Checks if a character value should be escaped/unescaped.</summary>
        /// <param name="c">a character value</param>
        /// <returns>true if it's OK to escape or unescape this value</returns>
        public static bool IsValidCharacterValue(int c) {
            return (c == 0x9 || c == 0xA || c == 0xD || c >= 0x20 && c <= 0xD7FF || c >= 0xE000 && c <= 0xFFFD || c >=
                 0x10000 && c <= 0x10FFFF);
        }

        /// <summary>Converts the current tag structure into an XML file with default encoding (UTF-8).</summary>
        /// <param name="os">the output stream to save XML file to</param>
        public virtual void ConvertToXml(Stream os) {
            ConvertToXml(os, "UTF-8");
        }

        /// <summary>Converts the current tag structure into an XML file with provided encoding.</summary>
        /// <param name="os">the output stream to save XML file to</param>
        /// <param name="charset">the charset of the resultant XML file</param>
        public virtual void ConvertToXml(Stream os, string charset) {
            @out = new StreamWriter(os, EncodingUtil.GetEncoding(charset));
            if (rootTag != null) {
                @out.Write("<" + rootTag + ">" + Environment.NewLine);
            }
            // get the StructTreeRoot from the document
            var structTreeRoot = document.GetStructTreeRoot();
            if (structTreeRoot == null) {
                throw new PdfException(PdfException.DocumentDoesntContainStructTreeRoot);
            }
            // Inspect the child or children of the StructTreeRoot
            InspectKids(structTreeRoot.GetKids());
            if (rootTag != null) {
                @out.Write("</" + rootTag + ">");
            }
            @out.Flush();
            @out.Dispose();
        }

        /// <summary>Sets the name of the root tag of the resultant XML file</summary>
        /// <param name="rootTagName">the name of the root tag</param>
        /// <returns>this object</returns>
        public virtual TaggedPdfReaderTool SetRootTag(string rootTagName) {
            rootTag = rootTagName;
            return this;
        }

        protected internal virtual void InspectKids(IList<IStructureNode> kids) {
            if (kids == null) {
                return;
            }
            foreach (var kid in kids) {
                InspectKid(kid);
            }
        }

        protected internal virtual void InspectKid(IStructureNode kid) {
            try {
                if (kid is PdfStructElem) {
                    var structElemKid = (PdfStructElem)kid;
                    var s = structElemKid.GetRole();
                    var tagN = s.GetValue();
                    var tag = FixTagName(tagN);
                    @out.Write("<");
                    @out.Write(tag);
                    InspectAttributes(structElemKid);
                    @out.Write(">" + Environment.NewLine);
                    var alt = (structElemKid).GetAlt();
                    if (alt != null) {
                        @out.Write("<alt><![CDATA[");
                        @out.Write(StringUtil.ReplaceAll(alt.GetValue(), "[\\000]*", ""));
                        @out.Write("]]></alt>" + Environment.NewLine);
                    }
                    InspectKids(structElemKid.GetKids());
                    @out.Write("</");
                    @out.Write(tag);
                    @out.Write(">" + Environment.NewLine);
                }
                else {
                    if (kid is PdfMcr) {
                        ParseTag((PdfMcr)kid);
                    }
                    else {
                        @out.Write(" <flushedKid/> ");
                    }
                }
            }
            catch (IOException e) {
                throw new IOException(IOException.UnknownIOException, e);
            }
        }

        protected internal virtual void InspectAttributes(PdfStructElem kid) {
            var attrObj = kid.GetAttributes(false);
            if (attrObj != null) {
                PdfDictionary attrDict;
                if (attrObj is PdfArray) {
                    attrDict = ((PdfArray)attrObj).GetAsDictionary(0);
                }
                else {
                    attrDict = (PdfDictionary)attrObj;
                }
                try {
                    foreach (var key in attrDict.KeySet()) {
                        @out.Write(' ');
                        var attrName = key.GetValue();
                        @out.Write(char.ToLower(attrName[0]) + attrName.Substring(1));
                        @out.Write("=\"");
                        @out.Write(attrDict.Get(key, false).ToString());
                        @out.Write("\"");
                    }
                }
                catch (IOException e) {
                    throw new IOException(IOException.UnknownIOException, e);
                }
            }
        }

        protected internal virtual void ParseTag(PdfMcr kid) {
            var mcid = kid.GetMcid();
            var pageDic = kid.GetPageObject();
            var tagContent = "";
            if (mcid != -1) {
                if (!parsedTags.ContainsKey(pageDic)) {
                    var listener = new MarkedContentEventListener
                        (this);
                    var processor = new PdfCanvasProcessor(listener);
                    var page = document.GetPage(pageDic);
                    processor.ProcessContent(page.GetContentBytes(), page.GetResources());
                    parsedTags.Put(pageDic, listener.GetMcidContent());
                }
                if (parsedTags.Get(pageDic).ContainsKey(mcid)) {
                    tagContent = parsedTags.Get(pageDic).Get(mcid);
                }
            }
            else {
                var objRef = (PdfObjRef)kid;
                PdfObject @object = objRef.GetReferencedObject();
                if (@object.IsDictionary()) {
                    var subtype = ((PdfDictionary)@object).GetAsName(PdfName.Subtype);
                    tagContent = subtype.ToString();
                }
            }
            try {
                @out.Write(EscapeXML(tagContent, true));
            }
            catch (IOException e) {
                throw new IOException(IOException.UnknownIOException, e);
            }
        }

        protected internal static string FixTagName(string tag) {
            var sb = new StringBuilder();
            for (var k = 0; k < tag.Length; ++k) {
                var c = tag[k];
                var nameStart = c == ':' || (c >= 'A' && c <= 'Z') || c == '_' || (c >= 'a' && c <= 'z') || (c >= '\u00c0'
                     && c <= '\u00d6') || (c >= '\u00d8' && c <= '\u00f6') || (c >= '\u00f8' && c <= '\u02ff') || (c >= '\u0370'
                     && c <= '\u037d') || (c >= '\u037f' && c <= '\u1fff') || (c >= '\u200c' && c <= '\u200d') || (c >= '\u2070'
                     && c <= '\u218f') || (c >= '\u2c00' && c <= '\u2fef') || (c >= '\u3001' && c <= '\ud7ff') || (c >= '\uf900'
                     && c <= '\ufdcf') || (c >= '\ufdf0' && c <= '\ufffd');
                var nameMiddle = c == '-' || c == '.' || (c >= '0' && c <= '9') || c == '\u00b7' || (c >= '\u0300' && c <=
                     '\u036f') || (c >= '\u203f' && c <= '\u2040') || nameStart;
                if (k == 0) {
                    if (!nameStart) {
                        c = '_';
                    }
                }
                else {
                    if (!nameMiddle) {
                        c = '-';
                    }
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// NOTE: copied from itext5 XMLUtils class
        /// Escapes a string with the appropriated XML codes.
        /// </summary>
        /// <param name="s">the string to be escaped</param>
        /// <param name="onlyASCII">codes above 127 will always be escaped with &amp;#nn; if <c>true</c></param>
        /// <returns>the escaped string</returns>
        protected internal static string EscapeXML(string s, bool onlyASCII) {
            var cc = s.ToCharArray();
            var len = cc.Length;
            var sb = new StringBuilder();
            for (var k = 0; k < len; ++k) {
                int c = cc[k];
                switch (c) {
                    case '<': {
                        sb.Append("&lt;");
                        break;
                    }

                    case '>': {
                        sb.Append("&gt;");
                        break;
                    }

                    case '&': {
                        sb.Append("&amp;");
                        break;
                    }

                    case '"': {
                        sb.Append("&quot;");
                        break;
                    }

                    case '\'': {
                        sb.Append("&apos;");
                        break;
                    }

                    default: {
                        if (IsValidCharacterValue(c)) {
                            if (onlyASCII && c > 127) {
                                sb.Append("&#").Append(c).Append(';');
                            }
                            else {
                                sb.Append((char)c);
                            }
                        }
                        break;
                    }
                }
            }
            return sb.ToString();
        }

        private class MarkedContentEventListener : IEventListener {
            private IDictionary<int, ITextExtractionStrategy> contentByMcid = new Dictionary<int, ITextExtractionStrategy
                >();

            public virtual IDictionary<int, string> GetMcidContent() {
                IDictionary<int, string> content = new Dictionary<int, string>();
                foreach (var id in contentByMcid.Keys) {
                    content.Put(id, contentByMcid.Get(id).GetResultantText());
                }
                return content;
            }

            public virtual void EventOccurred(IEventData data, EventType type) {
                switch (type) {
                    case EventType.RENDER_TEXT: {
                        var textInfo = (TextRenderInfo)data;
                        var mcid = textInfo.GetMcid();
                        if (mcid != -1) {
                            var textExtractionStrategy = contentByMcid.Get(mcid);
                            if (textExtractionStrategy == null) {
                                textExtractionStrategy = new LocationTextExtractionStrategy();
                                contentByMcid.Put(mcid, textExtractionStrategy);
                            }
                            textExtractionStrategy.EventOccurred(data, type);
                        }
                        break;
                    }
                }
            }

            public virtual ICollection<EventType> GetSupportedEvents() {
                return null;
            }

            internal MarkedContentEventListener(TaggedPdfReaderTool _enclosing) {
                this._enclosing = _enclosing;
            }

            private readonly TaggedPdfReaderTool _enclosing;
        }
    }
}
