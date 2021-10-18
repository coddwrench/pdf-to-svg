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
using System.Diagnostics;
using System.Text;
using IText.IO;
using IText.IO.Font;
using IText.IO.Source;
using IText.IO.Util;
using IText.Kernel.Counter;
using IText.Kernel.Counter.Event;
using IText.Kernel.Crypto;
using IText.Kernel.Events;
using IText.Kernel.Font;
using IText.Kernel.Geom;
using IText.Kernel.Log;
using IText.Kernel.Numbering;
using IText.Kernel.Pdf.Annot;
using IText.Kernel.Pdf.Canvas;
using IText.Kernel.Pdf.Collection;
using IText.Kernel.Pdf.Filespec;
using IText.Kernel.Pdf.Navigation;
using IText.Kernel.Pdf.Tagging;
using IText.Kernel.Pdf.Tagutils;
using IText.Kernel.XMP;
using IText.Kernel.XMP.Options;
using IText.Logger;
using IOException = System.IO.IOException;

namespace IText.Kernel.Pdf {
    /// <summary>Main enter point to work with PDF document.</summary>
    public class PdfDocument : IEventDispatcher, IDisposable {
        private static IPdfPageFactory pdfPageFactory = new PdfPageFactory();

        /// <summary>Currently active page.</summary>
        [Obsolete(@"Will be removed in iText 7.2")]
        protected internal PdfPage currentPage;

        /// <summary>Default page size.</summary>
        /// <remarks>
        /// Default page size.
        /// New page by default will be created with this size.
        /// </remarks>
        protected internal PageSize defaultPageSize = PageSize.Default;

        [NonSerialized]
        protected internal EventDispatcher eventDispatcher = new EventDispatcher();

        /// <summary>PdfWriter associated with the document.</summary>
        /// <remarks>
        /// PdfWriter associated with the document.
        /// Not null if document opened either in writing or stamping mode.
        /// </remarks>
        protected internal PdfWriter writer;

        /// <summary>PdfReader associated with the document.</summary>
        /// <remarks>
        /// PdfReader associated with the document.
        /// Not null if document is opened either in reading or stamping mode.
        /// </remarks>
        protected internal PdfReader reader;

        /// <summary>XMP Metadata for the document.</summary>
        protected internal byte[] xmpMetadata;

        /// <summary>Document catalog.</summary>
        protected internal PdfCatalog catalog;

        /// <summary>Document trailed.</summary>
        protected internal PdfDictionary trailer;

        /// <summary>Document info.</summary>
        protected internal PdfDocumentInfo info;

        /// <summary>Document version.</summary>
        protected internal PdfVersion pdfVersion = PdfVersion.PDF_1_7;

        /// <summary>The original (first) id when the document is read initially.</summary>
        private PdfString originalDocumentId;

        /// <summary>The original modified (second) id when the document is read initially.</summary>
        private PdfString modifiedDocumentId;

        /// <summary>List of indirect objects used in the document.</summary>
        internal readonly PdfXrefTable xref = new PdfXrefTable();

        protected internal FingerPrint fingerPrint;

        protected internal readonly StampingProperties properties;

        protected internal PdfStructTreeRoot structTreeRoot;

        protected internal int structParentIndex = -1;

        protected internal bool closeReader = true;

        protected internal bool closeWriter = true;

        protected internal bool isClosing;

        protected internal bool closed;

        /// <summary>flag determines whether to write unused objects to result document</summary>
        protected internal bool flushUnusedObjects;

        private IDictionary<PdfIndirectReference, PdfFont> documentFonts = new Dictionary<PdfIndirectReference, PdfFont
            >();

        private PdfFont defaultFont;

        [NonSerialized]
        protected internal TagStructureContext tagStructureContext;

        private static readonly AtomicLong lastDocumentId = new AtomicLong();

        private long documentId;

        private VersionInfo versionInfo = Version.GetInstance().GetInfo();

        /// <summary>Yet not copied link annotations from the other documents.</summary>
        /// <remarks>
        /// Yet not copied link annotations from the other documents.
        /// Key - page from the source document, which contains this annotation.
        /// Value - link annotation from the source document.
        /// </remarks>
        private LinkedDictionary<PdfPage, IList<PdfLinkAnnotation>> linkAnnotations = new LinkedDictionary<PdfPage
            , IList<PdfLinkAnnotation>>();

        /// <summary>Cache of already serialized objects from this document for smart mode.</summary>
        internal IDictionary<PdfIndirectReference, byte[]> serializedObjectsCache = new Dictionary<PdfIndirectReference
            , byte[]>();

        /// <summary>Handler which will be used for decompression of pdf streams.</summary>
        internal MemoryLimitsAwareHandler memoryLimitsAwareHandler;

        private EncryptedEmbeddedStreamsHandler encryptedEmbeddedStreamsHandler;

        /// <summary>Open PDF document in reading mode.</summary>
        /// <param name="reader">PDF reader.</param>
        public PdfDocument(PdfReader reader)
            : this(reader, new DocumentProperties()) {
        }

        /// <summary>Open PDF document in reading mode.</summary>
        /// <param name="reader">PDF reader.</param>
        /// <param name="properties">document properties</param>
        public PdfDocument(PdfReader reader, DocumentProperties properties) {
            if (reader == null) {
                throw new ArgumentException("The reader in PdfDocument constructor can not be null.");
            }
            documentId = lastDocumentId.IncrementAndGet();
            this.reader = reader;
            // default values of the StampingProperties doesn't affect anything
            this.properties = new StampingProperties();
            this.properties.SetEventCountingMetaInfo(properties.metaInfo);
            Open(null);
        }

        /// <summary>Open PDF document in writing mode.</summary>
        /// <remarks>
        /// Open PDF document in writing mode.
        /// Document has no pages when initialized.
        /// </remarks>
        /// <param name="writer">PDF writer</param>
        public PdfDocument(PdfWriter writer)
            : this(writer, new DocumentProperties()) {
        }

        /// <summary>Open PDF document in writing mode.</summary>
        /// <remarks>
        /// Open PDF document in writing mode.
        /// Document has no pages when initialized.
        /// </remarks>
        /// <param name="writer">PDF writer</param>
        /// <param name="properties">document properties</param>
        public PdfDocument(PdfWriter writer, DocumentProperties properties) {
            if (writer == null) {
                throw new ArgumentException("The writer in PdfDocument constructor can not be null.");
            }
            documentId = lastDocumentId.IncrementAndGet();
            this.writer = writer;
            // default values of the StampingProperties doesn't affect anything
            this.properties = new StampingProperties();
            this.properties.SetEventCountingMetaInfo(properties.metaInfo);
            Open(writer.properties.pdfVersion);
        }

        /// <summary>Opens PDF document in the stamping mode.</summary>
        /// <remarks>
        /// Opens PDF document in the stamping mode.
        /// <br />
        /// </remarks>
        /// <param name="reader">PDF reader.</param>
        /// <param name="writer">PDF writer.</param>
        public PdfDocument(PdfReader reader, PdfWriter writer)
            : this(reader, writer, new StampingProperties()) {
        }

        /// <summary>Open PDF document in stamping mode.</summary>
        /// <param name="reader">PDF reader.</param>
        /// <param name="writer">PDF writer.</param>
        /// <param name="properties">properties of the stamping process</param>
        public PdfDocument(PdfReader reader, PdfWriter writer, StampingProperties properties) {
            if (reader == null) {
                throw new ArgumentException("The reader in PdfDocument constructor can not be null.");
            }
            if (writer == null) {
                throw new ArgumentException("The writer in PdfDocument constructor can not be null.");
            }
            documentId = lastDocumentId.IncrementAndGet();
            this.reader = reader;
            this.writer = writer;
            this.properties = properties;
            var writerHasEncryption = WriterHasEncryption();
            if (properties.appendMode && writerHasEncryption) {
                var logger = LogManager.GetLogger(typeof(PdfDocument));
                logger.Warn(LogMessageConstant.WRITER_ENCRYPTION_IS_IGNORED_APPEND);
            }
            if (properties.preserveEncryption && writerHasEncryption) {
                var logger = LogManager.GetLogger(typeof(PdfDocument));
                logger.Warn(LogMessageConstant.WRITER_ENCRYPTION_IS_IGNORED_PRESERVE);
            }
            Open(writer.properties.pdfVersion);
        }

        /// <summary>Use this method to set the XMP Metadata.</summary>
        /// <param name="xmpMetadata">The xmpMetadata to set.</param>
        protected internal virtual void SetXmpMetadata(byte[] xmpMetadata) {
            this.xmpMetadata = xmpMetadata;
        }

        /// <summary>Sets the XMP Metadata.</summary>
        /// <param name="xmpMeta">the xmpMetadata to set</param>
        /// <param name="serializeOptions">serialization options</param>
        public virtual void SetXmpMetadata(XMPMeta xmpMeta, SerializeOptions serializeOptions) {
            SetXmpMetadata(XMPMetaFactory.SerializeToBuffer(xmpMeta, serializeOptions));
        }

        /// <summary>Sets the XMP Metadata.</summary>
        /// <param name="xmpMeta">the xmpMetadata to set</param>
        public virtual void SetXmpMetadata(XMPMeta xmpMeta) {
            var serializeOptions = new SerializeOptions();
            serializeOptions.SetPadding(2000);
            SetXmpMetadata(xmpMeta, serializeOptions);
        }

        /// <summary>Gets XMPMetadata.</summary>
        /// <returns>the XMPMetadata</returns>
        public virtual byte[] GetXmpMetadata() {
            return GetXmpMetadata(false);
        }

        /// <summary>Gets XMPMetadata or create a new one.</summary>
        /// <param name="createNew">if true, create a new empty XMPMetadata if it did not present.</param>
        /// <returns>existed or newly created XMPMetadata byte array.</returns>
        public virtual byte[] GetXmpMetadata(bool createNew) {
            if (xmpMetadata == null && createNew) {
                var xmpMeta = XMPMetaFactory.Create();
                xmpMeta.SetObjectName(XMPConst.TAG_XMPMETA);
                xmpMeta.SetObjectName("");
                AddCustomMetadataExtensions(xmpMeta);
                try {
                    xmpMeta.SetProperty(XMPConst.NS_DC, PdfConst.Format, "application/pdf");
                    xmpMeta.SetProperty(XMPConst.NS_PDF, PdfConst.Producer, versionInfo.GetVersion());
                    SetXmpMetadata(xmpMeta);
                }
                catch (XMPException) {
                }
            }
            return xmpMetadata;
        }

        /// <summary>Gets PdfObject by object number.</summary>
        /// <param name="objNum">object number.</param>
        /// <returns>
        /// 
        /// <see cref="PdfObject"/>
        /// or
        /// <see langword="null"/>
        /// , if object not found.
        /// </returns>
        public virtual PdfObject GetPdfObject(int objNum) {
            CheckClosingStatus();
            var reference = xref.Get(objNum);
            if (reference == null) {
                return null;
            }

            return reference.GetRefersTo();
        }

        /// <summary>Get number of indirect objects in the document.</summary>
        /// <returns>number of indirect objects.</returns>
        public virtual int GetNumberOfPdfObjects() {
            return xref.Size();
        }

        /// <summary>Gets the page by page number.</summary>
        /// <param name="pageNum">page number.</param>
        /// <returns>
        /// page by page number. may return
        /// <see langword="null"/>
        /// in case the page tree is broken
        /// </returns>
        public virtual PdfPage GetPage(int pageNum) {
            CheckClosingStatus();
            return catalog.GetPageTree().GetPage(pageNum);
        }

        /// <summary>
        /// Gets the
        /// <see cref="PdfPage"/>
        /// instance by
        /// <see cref="PdfDictionary"/>.
        /// </summary>
        /// <param name="pageDictionary">
        /// 
        /// <see cref="PdfDictionary"/>
        /// that present page.
        /// </param>
        /// <returns>
        /// page by
        /// <see cref="PdfDictionary"/>.
        /// </returns>
        public virtual PdfPage GetPage(PdfDictionary pageDictionary) {
            CheckClosingStatus();
            return catalog.GetPageTree().GetPage(pageDictionary);
        }

        /// <summary>Get the first page of the document.</summary>
        /// <returns>first page of the document.</returns>
        public virtual PdfPage GetFirstPage() {
            CheckClosingStatus();
            return GetPage(1);
        }

        /// <summary>Gets the last page of the document.</summary>
        /// <returns>last page.</returns>
        public virtual PdfPage GetLastPage() {
            return GetPage(GetNumberOfPages());
        }

        /// <summary>
        /// Marks
        /// <see cref="PdfStream"/>
        /// object as embedded file stream.
        /// </summary>
        /// <remarks>
        /// Marks
        /// <see cref="PdfStream"/>
        /// object as embedded file stream. Note that this method is for internal usage.
        /// To add an embedded file to the PDF document please use specialized API for file attachments.
        /// (e.g.
        /// <see cref="AddFileAttachment(System.String, PdfFileSpec)"/>
        /// ,
        /// <see cref="PdfPage.AddAnnotation(PdfAnnotation)"/>
        /// )
        /// </remarks>
        /// <param name="stream">to be marked as embedded file stream</param>
        public virtual void MarkStreamAsEmbeddedFile(PdfStream stream) {
            encryptedEmbeddedStreamsHandler.StoreEmbeddedStream(stream);
        }

        /// <summary>Creates and adds new page to the end of document.</summary>
        /// <returns>added page</returns>
        public virtual PdfPage AddNewPage() {
            return AddNewPage(GetDefaultPageSize());
        }

        /// <summary>Creates and adds new page with the specified page size.</summary>
        /// <param name="pageSize">page size of the new page</param>
        /// <returns>added page</returns>
        public virtual PdfPage AddNewPage(PageSize pageSize) {
            CheckClosingStatus();
            var page = GetPageFactory().CreatePdfPage(this, pageSize);
            CheckAndAddPage(page);
            DispatchEvent(new PdfDocumentEvent(PdfDocumentEvent.START_PAGE, page));
            DispatchEvent(new PdfDocumentEvent(PdfDocumentEvent.INSERT_PAGE, page));
            return page;
        }

        /// <summary>Creates and inserts new page to the document.</summary>
        /// <param name="index">position to addPage page to</param>
        /// <returns>inserted page</returns>
        public virtual PdfPage AddNewPage(int index) {
            return AddNewPage(index, GetDefaultPageSize());
        }

        /// <summary>Creates and inserts new page to the document.</summary>
        /// <param name="index">position to addPage page to</param>
        /// <param name="pageSize">page size of the new page</param>
        /// <returns>inserted page</returns>
        public virtual PdfPage AddNewPage(int index, PageSize pageSize) {
            CheckClosingStatus();
            var page = GetPageFactory().CreatePdfPage(this, pageSize);
            CheckAndAddPage(index, page);
            currentPage = page;
            DispatchEvent(new PdfDocumentEvent(PdfDocumentEvent.START_PAGE, page));
            DispatchEvent(new PdfDocumentEvent(PdfDocumentEvent.INSERT_PAGE, page));
            return page;
        }

        /// <summary>Adds page to the end of document.</summary>
        /// <param name="page">page to add.</param>
        /// <returns>added page.</returns>
        public virtual PdfPage AddPage(PdfPage page) {
            CheckClosingStatus();
            CheckAndAddPage(page);
            DispatchEvent(new PdfDocumentEvent(PdfDocumentEvent.INSERT_PAGE, page));
            return page;
        }

        /// <summary>Inserts page to the document.</summary>
        /// <param name="index">position to addPage page to</param>
        /// <param name="page">page to addPage</param>
        /// <returns>inserted page</returns>
        public virtual PdfPage AddPage(int index, PdfPage page) {
            CheckClosingStatus();
            CheckAndAddPage(index, page);
            currentPage = page;
            DispatchEvent(new PdfDocumentEvent(PdfDocumentEvent.INSERT_PAGE, page));
            return page;
        }

        /// <summary>Gets number of pages of the document.</summary>
        /// <returns>number of pages.</returns>
        public virtual int GetNumberOfPages() {
            CheckClosingStatus();
            return catalog.GetPageTree().GetNumberOfPages();
        }

        /// <summary>Gets page number by page.</summary>
        /// <param name="page">the page.</param>
        /// <returns>page number.</returns>
        public virtual int GetPageNumber(PdfPage page) {
            CheckClosingStatus();
            return catalog.GetPageTree().GetPageNumber(page);
        }

        /// <summary>
        /// Gets page number by
        /// <see cref="PdfDictionary"/>.
        /// </summary>
        /// <param name="pageDictionary">
        /// 
        /// <see cref="PdfDictionary"/>
        /// that present page.
        /// </param>
        /// <returns>
        /// page number by
        /// <see cref="PdfDictionary"/>.
        /// </returns>
        public virtual int GetPageNumber(PdfDictionary pageDictionary) {
            return catalog.GetPageTree().GetPageNumber(pageDictionary);
        }

        /// <summary>Moves page to new place in same document with all it tag structure</summary>
        /// <param name="page">page to be moved in document if present</param>
        /// <param name="insertBefore">indicates before which page new one will be inserted to</param>
        /// <returns><tt>true</tt> if this document contained the specified page</returns>
        public virtual bool MovePage(PdfPage page, int insertBefore) {
            CheckClosingStatus();
            var pageNum = GetPageNumber(page);
            if (pageNum > 0) {
                MovePage(pageNum, insertBefore);
                return true;
            }
            return false;
        }

        /// <summary>Moves page to new place in same document with all it tag structure</summary>
        /// <param name="pageNumber">number of Page that will be moved</param>
        /// <param name="insertBefore">indicates before which page new one will be inserted to</param>
        public virtual void MovePage(int pageNumber, int insertBefore) {
            CheckClosingStatus();
            if (insertBefore < 1 || insertBefore > GetNumberOfPages() + 1) {
                throw new IndexOutOfRangeException(MessageFormatUtil.Format(PdfException.RequestedPageNumberIsOutOfBounds, 
                    insertBefore));
            }
            var page = GetPage(pageNumber);
            if (IsTagged()) {
                GetStructTreeRoot().Move(page, insertBefore);
                GetTagStructureContext().NormalizeDocumentRootTag();
            }
            var removedPage = catalog.GetPageTree().RemovePage(pageNumber);
            if (insertBefore > pageNumber) {
                --insertBefore;
            }
            catalog.GetPageTree().AddPage(insertBefore, removedPage);
        }

        /// <summary>
        /// Removes the first occurrence of the specified page from this document,
        /// if it is present.
        /// </summary>
        /// <remarks>
        /// Removes the first occurrence of the specified page from this document,
        /// if it is present. Returns <tt>true</tt> if this document
        /// contained the specified element (or equivalently, if this document
        /// changed as a result of the call).
        /// </remarks>
        /// <param name="page">page to be removed from this document, if present</param>
        /// <returns><tt>true</tt> if this document contained the specified page</returns>
        public virtual bool RemovePage(PdfPage page) {
            CheckClosingStatus();
            var pageNum = GetPageNumber(page);
            if (pageNum >= 1) {
                RemovePage(pageNum);
                return true;
            }
            return false;
        }

        /// <summary>Removes page from the document by page number.</summary>
        /// <param name="pageNum">the one-based index of the PdfPage to be removed</param>
        public virtual void RemovePage(int pageNum) {
            CheckClosingStatus();
            var removedPage = GetPage(pageNum);
            if (removedPage != null && removedPage.IsFlushed() && (IsTagged() || HasAcroForm())) {
                throw new PdfException(PdfException.FLUSHED_PAGE_CANNOT_BE_REMOVED);
            }
            if (removedPage != null) {
                catalog.RemoveOutlines(removedPage);
                RemoveUnusedWidgetsFromFields(removedPage);
                if (IsTagged()) {
                    GetTagStructureContext().RemovePageTags(removedPage);
                }
                if (!removedPage.IsFlushed()) {
                    removedPage.GetPdfObject().Remove(PdfName.Parent);
                    removedPage.GetPdfObject().GetIndirectReference().SetFree();
                }
                DispatchEvent(new PdfDocumentEvent(PdfDocumentEvent.REMOVE_PAGE, removedPage));
            }
            catalog.GetPageTree().RemovePage(pageNum);
        }

        /// <summary>Gets document information dictionary.</summary>
        /// <remarks>
        /// Gets document information dictionary.
        /// <see cref="info"/>
        /// is lazy initialized. It will be initialized during the first call of this method.
        /// </remarks>
        /// <returns>document information dictionary.</returns>
        public virtual PdfDocumentInfo GetDocumentInfo() {
            CheckClosingStatus();
            if (info == null) {
                var infoDict = trailer.Get(PdfName.Info);
                info = new PdfDocumentInfo(infoDict is PdfDictionary ? (PdfDictionary)infoDict : new PdfDictionary(), this
                    );
                XmpMetaInfoConverter.AppendMetadataToInfo(xmpMetadata, info);
            }
            return info;
        }

        /// <summary>
        /// Gets original document id
        /// In order to set originalDocumentId
        /// <see cref="WriterProperties.SetInitialDocumentId(PdfString)"/>
        /// should be used
        /// </summary>
        /// <returns>original dccument id</returns>
        public virtual PdfString GetOriginalDocumentId() {
            return originalDocumentId;
        }

        /// <summary>
        /// Gets modified document id
        /// In order to set modifiedDocumentId
        /// <see cref="WriterProperties.SetModifiedDocumentId(PdfString)"/>
        /// should be used
        /// </summary>
        /// <returns>modified document id</returns>
        public virtual PdfString GetModifiedDocumentId() {
            return modifiedDocumentId;
        }

        /// <summary>Gets default page size.</summary>
        /// <returns>default page size.</returns>
        public virtual PageSize GetDefaultPageSize() {
            return defaultPageSize;
        }

        /// <summary>Sets default page size.</summary>
        /// <param name="pageSize">page size to be set as default.</param>
        public virtual void SetDefaultPageSize(PageSize pageSize) {
            defaultPageSize = pageSize;
        }

        /// <summary><inheritDoc/></summary>
        public virtual void AddEventHandler(string type, IEventHandler handler) {
            eventDispatcher.AddEventHandler(type, handler);
        }

        /// <summary><inheritDoc/></summary>
        public virtual void DispatchEvent(Event @event) {
            eventDispatcher.DispatchEvent(@event);
        }

        /// <summary><inheritDoc/></summary>
        public virtual void DispatchEvent(Event @event, bool delayed) {
            eventDispatcher.DispatchEvent(@event, delayed);
        }

        /// <summary><inheritDoc/></summary>
        public virtual bool HasEventHandler(string type) {
            return eventDispatcher.HasEventHandler(type);
        }

        /// <summary><inheritDoc/></summary>
        public virtual void RemoveEventHandler(string type, IEventHandler handler) {
            eventDispatcher.RemoveEventHandler(type, handler);
        }

        /// <summary><inheritDoc/></summary>
        public virtual void RemoveAllHandlers() {
            eventDispatcher.RemoveAllHandlers();
        }

        /// <summary>
        /// Gets
        /// <c>PdfWriter</c>
        /// associated with the document.
        /// </summary>
        /// <returns>PdfWriter associated with the document.</returns>
        public virtual PdfWriter GetWriter() {
            CheckClosingStatus();
            return writer;
        }

        /// <summary>
        /// Gets
        /// <c>PdfReader</c>
        /// associated with the document.
        /// </summary>
        /// <returns>PdfReader associated with the document.</returns>
        public virtual PdfReader GetReader() {
            CheckClosingStatus();
            return reader;
        }

        /// <summary>
        /// Returns
        /// <see langword="true"/>
        /// if the document is opened in append mode, and
        /// <see langword="false"/>
        /// otherwise.
        /// </summary>
        /// <returns>
        /// 
        /// <see langword="true"/>
        /// if the document is opened in append mode, and
        /// <see langword="false"/>
        /// otherwise.
        /// </returns>
        public virtual bool IsAppendMode() {
            CheckClosingStatus();
            return properties.appendMode;
        }

        /// <summary>Creates next available indirect reference.</summary>
        /// <returns>created indirect reference.</returns>
        public virtual PdfIndirectReference CreateNextIndirectReference() {
            CheckClosingStatus();
            return xref.CreateNextIndirectReference(this);
        }

        /// <summary>Gets PDF version.</summary>
        /// <returns>PDF version.</returns>
        public virtual PdfVersion GetPdfVersion() {
            return pdfVersion;
        }

        /// <summary>Gets PDF catalog.</summary>
        /// <returns>PDF catalog.</returns>
        public virtual PdfCatalog GetCatalog() {
            CheckClosingStatus();
            return catalog;
        }

        /// <summary>Close PDF document.</summary>
        public virtual void Close() {
            if (closed) {
                return;
            }
            isClosing = true;
            try {
                if (writer != null) {
                    if (catalog.IsFlushed()) {
                        throw new PdfException(PdfException.CannotCloseDocumentWithAlreadyFlushedPdfCatalog);
                    }
                    UpdateProducerInInfoDictionary();
                    UpdateXmpMetadata();
                    // In PDF 2.0, all the values except CreationDate and ModDate are deprecated. Remove them now
                    if (pdfVersion.CompareTo(PdfVersion.PDF_2_0) >= 0) {
                        foreach (var deprecatedKey in PdfDocumentInfo.PDF20_DEPRECATED_KEYS) {
                            GetDocumentInfo().GetPdfObject().Remove(deprecatedKey);
                        }
                    }
                    if (GetXmpMetadata() != null) {
                        var xmp = catalog.GetPdfObject().GetAsStream(PdfName.Metadata);
                        if (IsAppendMode() && xmp != null && !xmp.IsFlushed() && xmp.GetIndirectReference() != null) {
                            // Use existing object for append mode
                            xmp.SetData(xmpMetadata);
                            xmp.SetModified();
                        }
                        else {
                            // Create new object
                            xmp = (PdfStream)new PdfStream().MakeIndirect(this);
                            xmp.GetOutputStream().Write(xmpMetadata);
                            catalog.GetPdfObject().Put(PdfName.Metadata, xmp);
                            catalog.SetModified();
                        }
                        xmp.Put(PdfName.Type, PdfName.Metadata);
                        xmp.Put(PdfName.Subtype, PdfName.XML);
                        if (writer.crypto != null && !writer.crypto.IsMetadataEncrypted()) {
                            var ar = new PdfArray();
                            ar.Add(PdfName.Crypt);
                            xmp.Put(PdfName.Filter, ar);
                        }
                    }
                    CheckIsoConformance();
                    PdfObject crypto = null;
                    ICollection<PdfIndirectReference> forbiddenToFlush = new HashSet<PdfIndirectReference>();
                    if (properties.appendMode) {
                        if (structTreeRoot != null) {
                            TryFlushTagStructure(true);
                        }
                        if (catalog.IsOCPropertiesMayHaveChanged() && catalog.GetOCProperties(false).GetPdfObject().IsModified()) {
                            catalog.GetOCProperties(false).Flush();
                        }
                        if (catalog.pageLabels != null) {
                            catalog.Put(PdfName.PageLabels, catalog.pageLabels.BuildTree());
                        }
                        foreach (var entry in catalog.nameTrees) {
                            var tree = entry.Value;
                            if (tree.IsModified()) {
                                EnsureTreeRootAddedToNames(tree.BuildTree().MakeIndirect(this), entry.Key);
                            }
                        }
                        var pageRoot = catalog.GetPageTree().GenerateTree();
                        if (catalog.GetPdfObject().IsModified() || pageRoot.IsModified()) {
                            catalog.Put(PdfName.Pages, pageRoot);
                            catalog.GetPdfObject().Flush(false);
                        }
                        if (GetDocumentInfo().GetPdfObject().IsModified()) {
                            GetDocumentInfo().GetPdfObject().Flush(false);
                        }
                        FlushFonts();
                        if (writer.crypto != null) {
                            Debug.Assert(reader.decrypt.GetPdfObject() == writer.crypto.GetPdfObject(), "Conflict with source encryption"
                                );
                            crypto = reader.decrypt.GetPdfObject();
                            if (crypto.GetIndirectReference() != null) {
                                // Checking just for extra safety, encryption dictionary shall never be direct.
                                forbiddenToFlush.Add(crypto.GetIndirectReference());
                            }
                        }
                        writer.FlushModifiedWaitingObjects(forbiddenToFlush);
                        for (var i = 0; i < xref.Size(); i++) {
                            var indirectReference = xref.Get(i);
                            if (indirectReference != null && !indirectReference.IsFree() && indirectReference.CheckState(PdfObject.MODIFIED
                                ) && !indirectReference.CheckState(PdfObject.FLUSHED) && !forbiddenToFlush.Contains(indirectReference)
                                ) {
                                indirectReference.SetFree();
                            }
                        }
                    }
                    else {
                        if (catalog.IsOCPropertiesMayHaveChanged()) {
                            catalog.GetPdfObject().Put(PdfName.OCProperties, catalog.GetOCProperties(false).GetPdfObject());
                            catalog.GetOCProperties(false).Flush();
                        }
                        if (catalog.pageLabels != null) {
                            catalog.Put(PdfName.PageLabels, catalog.pageLabels.BuildTree());
                        }
                        catalog.GetPdfObject().Put(PdfName.Pages, catalog.GetPageTree().GenerateTree());
                        foreach (var entry in catalog.nameTrees) {
                            var tree = entry.Value;
                            if (tree.IsModified()) {
                                EnsureTreeRootAddedToNames(tree.BuildTree().MakeIndirect(this), entry.Key);
                            }
                        }
                        for (var pageNum = 1; pageNum <= GetNumberOfPages(); pageNum++) {
                            GetPage(pageNum).Flush();
                        }
                        if (structTreeRoot != null) {
                            TryFlushTagStructure(false);
                        }
                        catalog.GetPdfObject().Flush(false);
                        GetDocumentInfo().GetPdfObject().Flush(false);
                        FlushFonts();
                        if (writer.crypto != null) {
                            crypto = writer.crypto.GetPdfObject();
                            crypto.MakeIndirect(this);
                            forbiddenToFlush.Add(crypto.GetIndirectReference());
                        }
                        writer.FlushWaitingObjects(forbiddenToFlush);
                        for (var i = 0; i < xref.Size(); i++) {
                            var indirectReference = xref.Get(i);
                            if (indirectReference != null && !indirectReference.IsFree() && !indirectReference.CheckState(PdfObject.FLUSHED
                                ) && !forbiddenToFlush.Contains(indirectReference)) {
                                PdfObject @object;
                                if (IsFlushUnusedObjects() && !indirectReference.CheckState(PdfObject.ORIGINAL_OBJECT_STREAM) && (@object 
                                    = indirectReference.GetRefersTo(false)) != null) {
                                    @object.Flush();
                                }
                                else {
                                    indirectReference.SetFree();
                                }
                            }
                        }
                    }
                    // To avoid encryption of XrefStream and Encryption dictionary remove crypto.
                    // NOTE. No need in reverting, because it is the last operation with the document.
                    writer.crypto = null;
                    if (!properties.appendMode && crypto != null) {
                        // no need to flush crypto in append mode, it shall not have changed in this case
                        crypto.Flush(false);
                    }
                    // The following two operators prevents the possible inconsistency between root and info
                    // entries existing in the trailer object and corresponding fields. This inconsistency
                    // may appear when user gets trailer and explicitly sets new root or info dictionaries.
                    trailer.Put(PdfName.Root, catalog.GetPdfObject());
                    trailer.Put(PdfName.Info, GetDocumentInfo().GetPdfObject());
                    //By this time original and modified document ids should always be not null due to initializing in
                    // either writer properties, or in the writer init section on document open or from pdfreader. So we shouldn't worry about it being null next
                    var fileId = PdfEncryption.CreateInfoId(ByteUtils.GetIsoBytes(originalDocumentId.GetValue()), ByteUtils
                        .GetIsoBytes(modifiedDocumentId.GetValue()));
                    xref.WriteXrefTableAndTrailer(this, fileId, crypto);
                    writer.Flush();
                    foreach (var counter in GetCounters()) {
                        counter.OnDocumentWritten(writer.GetCurrentPos());
                    }
                }
                catalog.GetPageTree().ClearPageRefs();
                RemoveAllHandlers();
            }
            catch (IOException e) {
                throw new PdfException(PdfException.CannotCloseDocument, e, this);
            }
            finally {
                if (writer != null && IsCloseWriter()) {
                    try {
                        writer.Dispose();
                    }
                    catch (Exception e) {
                        var logger = LogManager.GetLogger(typeof(PdfDocument));
                        logger.Error(LogMessageConstant.PDF_WRITER_CLOSING_FAILED, e);
                    }
                }
                if (reader != null && IsCloseReader()) {
                    try {
                        reader.Close();
                    }
                    catch (Exception e) {
                        var logger = LogManager.GetLogger(typeof(PdfDocument));
                        logger.Error(LogMessageConstant.PDF_READER_CLOSING_FAILED, e);
                    }
                }
            }
            closed = true;
        }

        /// <summary>Gets close status of the document.</summary>
        /// <returns>true, if the document has already been closed, otherwise false.</returns>
        public virtual bool IsClosed() {
            return closed;
        }

        /// <summary>Gets tagged status of the document.</summary>
        /// <returns>true, if the document has tag structure, otherwise false.</returns>
        public virtual bool IsTagged() {
            return structTreeRoot != null;
        }

        /// <summary>Specifies that document shall contain tag structure.</summary>
        /// <remarks>
        /// Specifies that document shall contain tag structure.
        /// See ISO 32000-1, section 14.8 "Tagged PDF"
        /// </remarks>
        /// <returns>this PdfDocument</returns>
        public virtual PdfDocument SetTagged() {
            CheckClosingStatus();
            if (structTreeRoot == null) {
                structTreeRoot = new PdfStructTreeRoot(this);
                catalog.GetPdfObject().Put(PdfName.StructTreeRoot, structTreeRoot.GetPdfObject());
                UpdateValueInMarkInfoDict(PdfName.Marked, PdfBoolean.TRUE);
                structParentIndex = 0;
            }
            return this;
        }

        /// <summary>
        /// Gets
        /// <see cref="PdfStructTreeRoot"/>
        /// of tagged document.
        /// </summary>
        /// <returns>
        /// 
        /// <see cref="PdfStructTreeRoot"/>
        /// in case tagged document, otherwise false.
        /// </returns>
        /// <seealso cref="IsTagged()"/>
        /// <seealso cref="GetNextStructParentIndex()"/>
        public virtual PdfStructTreeRoot GetStructTreeRoot() {
            return structTreeRoot;
        }

        /// <summary>Gets next parent index of tagged document.</summary>
        /// <returns>-1 if document is not tagged, or &gt;= 0 if tagged.</returns>
        /// <seealso cref="IsTagged()"/>
        /// <seealso cref="GetNextStructParentIndex()"/>
        public virtual int GetNextStructParentIndex() {
            return structParentIndex < 0 ? -1 : structParentIndex++;
        }

        /// <summary>
        /// Gets document
        /// <c>TagStructureContext</c>.
        /// </summary>
        /// <remarks>
        /// Gets document
        /// <c>TagStructureContext</c>.
        /// The document must be tagged, otherwise an exception will be thrown.
        /// </remarks>
        /// <returns>
        /// document
        /// <c>TagStructureContext</c>.
        /// </returns>
        public virtual TagStructureContext GetTagStructureContext() {
            CheckClosingStatus();
            if (tagStructureContext == null) {
                if (!IsTagged()) {
                    throw new PdfException(PdfException.MustBeATaggedDocument);
                }
                InitTagStructureContext();
            }
            return tagStructureContext;
        }

        /// <summary>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>.
        /// </summary>
        /// <remarks>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>.
        /// Use this method if you want to copy pages across tagged documents.
        /// This will keep resultant PDF structure consistent.
        /// <para />
        /// If outlines destination names are the same in different documents, all
        /// such outlines will lead to a single location in the resultant document.
        /// In this case iText will log a warning. This can be avoided by renaming
        /// destinations names in the source document.
        /// </remarks>
        /// <param name="pageFrom">start of the range of pages to be copied.</param>
        /// <param name="pageTo">end of the range of pages to be copied.</param>
        /// <param name="toDocument">a document to copy pages to.</param>
        /// <param name="insertBeforePage">a position where to insert copied pages.</param>
        /// <returns>list of copied pages</returns>
        public virtual IList<PdfPage> CopyPagesTo(int pageFrom, int pageTo, PdfDocument toDocument
            , int insertBeforePage) {
            return CopyPagesTo(pageFrom, pageTo, toDocument, insertBeforePage, null);
        }

        /// <summary>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>.
        /// </summary>
        /// <remarks>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>
        /// . This range is inclusive, both
        /// <c>page</c>
        /// and
        /// <paramref name="pageTo"/>
        /// are included in list of copied pages.
        /// Use this method if you want to copy pages across tagged documents.
        /// This will keep resultant PDF structure consistent.
        /// <para />
        /// If outlines destination names are the same in different documents, all
        /// such outlines will lead to a single location in the resultant document.
        /// In this case iText will log a warning. This can be avoided by renaming
        /// destinations names in the source document.
        /// </remarks>
        /// <param name="pageFrom">1-based start of the range of pages to be copied.</param>
        /// <param name="pageTo">1-based end (inclusive) of the range of pages to be copied. This page is included in list of copied pages.
        ///     </param>
        /// <param name="toDocument">a document to copy pages to.</param>
        /// <param name="insertBeforePage">a position where to insert copied pages.</param>
        /// <param name="copier">
        /// a copier which bears a special copy logic. May be null.
        /// It is recommended to use the same instance of
        /// <see cref="IPdfPageExtraCopier"/>
        /// for the same output document.
        /// </param>
        /// <returns>list of new copied pages</returns>
        public virtual IList<PdfPage> CopyPagesTo(int pageFrom, int pageTo, PdfDocument toDocument
            , int insertBeforePage, IPdfPageExtraCopier copier) {
            IList<int> pages = new List<int>();
            for (var i = pageFrom; i <= pageTo; i++) {
                pages.Add(i);
            }
            return CopyPagesTo(pages, toDocument, insertBeforePage, copier);
        }

        /// <summary>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>
        /// appending copied pages to the end.
        /// </summary>
        /// <remarks>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>
        /// appending copied pages to the end. This range
        /// is inclusive, both
        /// <c>page</c>
        /// and
        /// <paramref name="pageTo"/>
        /// are included in list of copied pages.
        /// Use this method if you want to copy pages across tagged documents.
        /// This will keep resultant PDF structure consistent.
        /// <para />
        /// If outlines destination names are the same in different documents, all
        /// such outlines will lead to a single location in the resultant document.
        /// In this case iText will log a warning. This can be avoided by renaming
        /// destinations names in the source document.
        /// </remarks>
        /// <param name="pageFrom">1-based start of the range of pages to be copied.</param>
        /// <param name="pageTo">1-based end (inclusive) of the range of pages to be copied. This page is included in list of copied pages.
        ///     </param>
        /// <param name="toDocument">a document to copy pages to.</param>
        /// <returns>list of new copied pages</returns>
        public virtual IList<PdfPage> CopyPagesTo(int pageFrom, int pageTo, PdfDocument toDocument
            ) {
            return CopyPagesTo(pageFrom, pageTo, toDocument, null);
        }

        /// <summary>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>
        /// appending copied pages to the end.
        /// </summary>
        /// <remarks>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>
        /// appending copied pages to the end. This range
        /// is inclusive, both
        /// <c>page</c>
        /// and
        /// <paramref name="pageTo"/>
        /// are included in list of copied pages.
        /// Use this method if you want to copy pages across tagged documents.
        /// This will keep resultant PDF structure consistent.
        /// <para />
        /// If outlines destination names are the same in different documents, all
        /// such outlines will lead to a single location in the resultant document.
        /// In this case iText will log a warning. This can be avoided by renaming
        /// destinations names in the source document.
        /// </remarks>
        /// <param name="pageFrom">1-based start of the range of pages to be copied.</param>
        /// <param name="pageTo">1-based end (inclusive) of the range of pages to be copied. This page is included in list of copied pages.
        ///     </param>
        /// <param name="toDocument">a document to copy pages to.</param>
        /// <param name="copier">
        /// a copier which bears a special copy logic. May be null.
        /// It is recommended to use the same instance of
        /// <see cref="IPdfPageExtraCopier"/>
        /// for the same output document.
        /// </param>
        /// <returns>list of new copied pages.</returns>
        public virtual IList<PdfPage> CopyPagesTo(int pageFrom, int pageTo, PdfDocument toDocument
            , IPdfPageExtraCopier copier) {
            return CopyPagesTo(pageFrom, pageTo, toDocument, toDocument.GetNumberOfPages() + 1, copier);
        }

        /// <summary>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>.
        /// </summary>
        /// <remarks>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>.
        /// Use this method if you want to copy pages across tagged documents.
        /// This will keep resultant PDF structure consistent.
        /// <para />
        /// If outlines destination names are the same in different documents, all
        /// such outlines will lead to a single location in the resultant document.
        /// In this case iText will log a warning. This can be avoided by renaming
        /// destinations names in the source document.
        /// </remarks>
        /// <param name="pagesToCopy">list of pages to be copied.</param>
        /// <param name="toDocument">a document to copy pages to.</param>
        /// <param name="insertBeforePage">a position where to insert copied pages.</param>
        /// <returns>list of new copied pages</returns>
        public virtual IList<PdfPage> CopyPagesTo(IList<int> pagesToCopy, PdfDocument toDocument, 
            int insertBeforePage) {
            return CopyPagesTo(pagesToCopy, toDocument, insertBeforePage, null);
        }

        /// <summary>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>.
        /// </summary>
        /// <remarks>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>.
        /// Use this method if you want to copy pages across tagged documents.
        /// This will keep resultant PDF structure consistent.
        /// <para />
        /// If outlines destination names are the same in different documents, all
        /// such outlines will lead to a single location in the resultant document.
        /// In this case iText will log a warning. This can be avoided by renaming
        /// destinations names in the source document.
        /// </remarks>
        /// <param name="pagesToCopy">list of pages to be copied.</param>
        /// <param name="toDocument">a document to copy pages to.</param>
        /// <param name="insertBeforePage">a position where to insert copied pages.</param>
        /// <param name="copier">
        /// a copier which bears a special copy logic. May be null.
        /// It is recommended to use the same instance of
        /// <see cref="IPdfPageExtraCopier"/>
        /// for the same output document.
        /// </param>
        /// <returns>list of new copied pages</returns>
        public virtual IList<PdfPage> CopyPagesTo(IList<int> pagesToCopy, PdfDocument toDocument, 
            int insertBeforePage, IPdfPageExtraCopier copier) {
            if (pagesToCopy.IsEmpty()) {
                return JavaCollectionsUtil.EmptyList<PdfPage>();
            }
            CheckClosingStatus();
            IList<PdfPage> copiedPages = new List<PdfPage>();
            IDictionary<PdfPage, PdfPage> page2page = new LinkedDictionary<PdfPage, PdfPage>();
            ICollection<PdfOutline> outlinesToCopy = new HashSet<PdfOutline>();
            IList<IDictionary<PdfPage, PdfPage>> rangesOfPagesWithIncreasingNumbers = new List<IDictionary<PdfPage, PdfPage
                >>();
            var lastCopiedPageNum = pagesToCopy[0];
            var pageInsertIndex = insertBeforePage;
            var insertInBetween = insertBeforePage < toDocument.GetNumberOfPages() + 1;
            foreach (int? pageNum in pagesToCopy) {
                var page = GetPage((int)pageNum);
                var newPage = page.CopyTo(toDocument, copier);
                copiedPages.Add(newPage);
                page2page.Put(page, newPage);
                if (lastCopiedPageNum >= pageNum) {
                    rangesOfPagesWithIncreasingNumbers.Add(new Dictionary<PdfPage, PdfPage>());
                }
                var lastRangeInd = rangesOfPagesWithIncreasingNumbers.Count - 1;
                rangesOfPagesWithIncreasingNumbers[lastRangeInd].Put(page, newPage);
                if (insertInBetween) {
                    toDocument.AddPage(pageInsertIndex, newPage);
                }
                else {
                    toDocument.AddPage(newPage);
                }
                pageInsertIndex++;
                if (toDocument.HasOutlines()) {
                    var pageOutlines = page.GetOutlines(false);
                    if (pageOutlines != null) {
                        outlinesToCopy.AddAll(pageOutlines);
                    }
                }
                lastCopiedPageNum = (int)pageNum;
            }
            CopyLinkAnnotations(toDocument, page2page);
            // Copying OCGs should go after copying LinkAnnotations
            if (GetCatalog() != null && GetCatalog().GetPdfObject().GetAsDictionary(PdfName.OCProperties) != null) {
                OcgPropertiesCopier.CopyOCGProperties(this, toDocument, page2page);
            }
            // It's important to copy tag structure after link annotations were copied, because object content items in tag
            // structure are not copied in case if their's OBJ key is annotation and doesn't contain /P entry.
            if (toDocument.IsTagged()) {
                if (IsTagged()) {
                    try {
                        foreach (var increasingPagesRange in rangesOfPagesWithIncreasingNumbers) {
                            if (insertInBetween) {
                                GetStructTreeRoot().CopyTo(toDocument, insertBeforePage, increasingPagesRange);
                            }
                            else {
                                GetStructTreeRoot().CopyTo(toDocument, increasingPagesRange);
                            }
                            insertBeforePage += increasingPagesRange.Count;
                        }
                        toDocument.GetTagStructureContext().NormalizeDocumentRootTag();
                    }
                    catch (Exception ex) {
                        throw new PdfException(PdfException.TagStructureCopyingFailedItMightBeCorruptedInOneOfTheDocuments, ex);
                    }
                }
                else {
                    var logger = LogManager.GetLogger(typeof(PdfDocument));
                    logger.Warn(LogMessageConstant.NOT_TAGGED_PAGES_IN_TAGGED_DOCUMENT);
                }
            }
            if (catalog.IsOutlineMode()) {
                CopyOutlines(outlinesToCopy, toDocument, page2page);
            }
            return copiedPages;
        }

        /// <summary>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>
        /// appending copied pages to the end.
        /// </summary>
        /// <remarks>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>
        /// appending copied pages to the end.
        /// Use this method if you want to copy pages across tagged documents.
        /// This will keep resultant PDF structure consistent.
        /// <para />
        /// If outlines destination names are the same in different documents, all
        /// such outlines will lead to a single location in the resultant document.
        /// In this case iText will log a warning. This can be avoided by renaming
        /// destinations names in the source document.
        /// </remarks>
        /// <param name="pagesToCopy">list of pages to be copied.</param>
        /// <param name="toDocument">a document to copy pages to.</param>
        /// <returns>list of copied pages</returns>
        public virtual IList<PdfPage> CopyPagesTo(IList<int> pagesToCopy, PdfDocument toDocument) {
            return CopyPagesTo(pagesToCopy, toDocument, null);
        }

        /// <summary>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>
        /// appending copied pages to the end.
        /// </summary>
        /// <remarks>
        /// Copies a range of pages from current document to
        /// <paramref name="toDocument"/>
        /// appending copied pages to the end.
        /// Use this method if you want to copy pages across tagged documents.
        /// This will keep resultant PDF structure consistent.
        /// <para />
        /// If outlines destination names are the same in different documents, all
        /// such outlines will lead to a single location in the resultant document.
        /// In this case iText will log a warning. This can be avoided by renaming
        /// destinations names in the source document.
        /// </remarks>
        /// <param name="pagesToCopy">list of pages to be copied.</param>
        /// <param name="toDocument">a document to copy pages to.</param>
        /// <param name="copier">
        /// a copier which bears a special copy logic. May be null.
        /// It is recommended to use the same instance of
        /// <see cref="IPdfPageExtraCopier"/>
        /// for the same output document.
        /// </param>
        /// <returns>list of copied pages</returns>
        public virtual IList<PdfPage> CopyPagesTo(IList<int> pagesToCopy, PdfDocument toDocument, 
            IPdfPageExtraCopier copier) {
            return CopyPagesTo(pagesToCopy, toDocument, toDocument.GetNumberOfPages() + 1, copier);
        }

        /// <summary>Flush all copied objects and remove them from copied cache.</summary>
        /// <remarks>
        /// Flush all copied objects and remove them from copied cache.
        /// <para />
        /// Note, if you will copy objects from the same document, duplicated objects will be created.
        /// That's why usually this method is meant to be used when all copying from source document is finished.
        /// For other cases one can also consider other flushing mechanisms, e.g. pages-based flushing.
        /// </remarks>
        /// <param name="sourceDoc">source document</param>
        public virtual void FlushCopiedObjects(PdfDocument sourceDoc) {
            if (GetWriter() != null) {
                GetWriter().FlushCopiedObjects(sourceDoc.GetDocumentId());
            }
        }

        /// <summary>
        /// Checks, whether
        /// <see cref="Close()"/>
        /// method will close associated PdfReader.
        /// </summary>
        /// <returns>
        /// true,
        /// <see cref="Close()"/>
        /// method is going to close associated PdfReader, otherwise false.
        /// </returns>
        public virtual bool IsCloseReader() {
            return closeReader;
        }

        /// <summary>
        /// Sets, whether
        /// <see cref="Close()"/>
        /// method shall close associated PdfReader.
        /// </summary>
        /// <param name="closeReader">
        /// true,
        /// <see cref="Close()"/>
        /// method shall close associated PdfReader, otherwise false.
        /// </param>
        public virtual void SetCloseReader(bool closeReader) {
            CheckClosingStatus();
            this.closeReader = closeReader;
        }

        /// <summary>
        /// Checks, whether
        /// <see cref="Close()"/>
        /// method will close associated PdfWriter.
        /// </summary>
        /// <returns>
        /// true,
        /// <see cref="Close()"/>
        /// method is going to close associated PdfWriter, otherwise false.
        /// </returns>
        public virtual bool IsCloseWriter() {
            return closeWriter;
        }

        /// <summary>
        /// Sets, whether
        /// <see cref="Close()"/>
        /// method shall close associated PdfWriter.
        /// </summary>
        /// <param name="closeWriter">
        /// true,
        /// <see cref="Close()"/>
        /// method shall close associated PdfWriter, otherwise false.
        /// </param>
        public virtual void SetCloseWriter(bool closeWriter) {
            CheckClosingStatus();
            this.closeWriter = closeWriter;
        }

        /// <summary>
        /// Checks, whether
        /// <see cref="Close()"/>
        /// will flush unused objects,
        /// e.g. unreachable from PDF Catalog.
        /// </summary>
        /// <remarks>
        /// Checks, whether
        /// <see cref="Close()"/>
        /// will flush unused objects,
        /// e.g. unreachable from PDF Catalog. By default - false.
        /// </remarks>
        /// <returns>
        /// false, if
        /// <see cref="Close()"/>
        /// shall not flush unused objects, otherwise true.
        /// </returns>
        public virtual bool IsFlushUnusedObjects() {
            return flushUnusedObjects;
        }

        /// <summary>
        /// Sets, whether
        /// <see cref="Close()"/>
        /// shall flush unused objects,
        /// e.g. unreachable from PDF Catalog.
        /// </summary>
        /// <param name="flushUnusedObjects">
        /// false, if
        /// <see cref="Close()"/>
        /// shall not flush unused objects, otherwise true.
        /// </param>
        public virtual void SetFlushUnusedObjects(bool flushUnusedObjects) {
            CheckClosingStatus();
            this.flushUnusedObjects = flushUnusedObjects;
        }

        /// <summary>This method returns a complete outline tree of the whole document.</summary>
        /// <param name="updateOutlines">
        /// if the flag is
        /// <see langword="true"/>
        /// , the method reads the whole document and creates outline tree.
        /// If the flag is
        /// <see langword="false"/>
        /// , the method gets cached outline tree
        /// (if it was cached via calling getOutlines method before).
        /// </param>
        /// <returns>
        /// fully initialize
        /// <see cref="PdfOutline"/>
        /// object.
        /// </returns>
        public virtual PdfOutline GetOutlines(bool updateOutlines) {
            CheckClosingStatus();
            return catalog.GetOutlines(updateOutlines);
        }

        /// <summary>This method initializes an outline tree of the document and sets outline mode to true.</summary>
        public virtual void InitializeOutlines() {
            CheckClosingStatus();
            GetOutlines(false);
        }

        /// <summary>This methods adds new name in the Dests NameTree.</summary>
        /// <remarks>This methods adds new name in the Dests NameTree. It throws an exception, if the name already exists.
        ///     </remarks>
        /// <param name="key">Name of the destination.</param>
        /// <param name="value">
        /// An object destination refers to. Must be an array or a dictionary with key /D and array.
        /// See ISO 32000-1 12.3.2.3 for more info.
        /// </param>
        public virtual void AddNamedDestination(string key, PdfObject value) {
            CheckClosingStatus();
            if (value.IsArray() && ((PdfArray)value).Get(0).IsNumber()) {
                LogManager.GetLogger(typeof(PdfDocument)).Warn(LogMessageConstant.INVALID_DESTINATION_TYPE
                    );
            }
            catalog.AddNamedDestination(key, value);
        }

        /// <summary>Gets static copy of cross reference table.</summary>
        /// <returns>a static copy of cross reference table</returns>
        public virtual IList<PdfIndirectReference> ListIndirectReferences() {
            CheckClosingStatus();
            IList<PdfIndirectReference> indRefs = new List<PdfIndirectReference>(xref.Size());
            for (var i = 0; i < xref.Size(); ++i) {
                var indref = xref.Get(i);
                if (indref != null) {
                    indRefs.Add(indref);
                }
            }
            return indRefs;
        }

        /// <summary>Gets document trailer.</summary>
        /// <returns>document trailer.</returns>
        public virtual PdfDictionary GetTrailer() {
            CheckClosingStatus();
            return trailer;
        }

        /// <summary>
        /// Adds
        /// <see cref="PdfOutputIntent"/>
        /// that shall specify the colour characteristics of output devices
        /// on which the document might be rendered.
        /// </summary>
        /// <param name="outputIntent">
        /// 
        /// <see cref="PdfOutputIntent"/>
        /// to add.
        /// </param>
        /// <seealso cref="PdfOutputIntent"/>
        public virtual void AddOutputIntent(PdfOutputIntent outputIntent) {
            CheckClosingStatus();
            if (outputIntent == null) {
                return;
            }
            var outputIntents = catalog.GetPdfObject().GetAsArray(PdfName.OutputIntents);
            if (outputIntents == null) {
                outputIntents = new PdfArray();
                catalog.Put(PdfName.OutputIntents, outputIntents);
            }
            outputIntents.Add(outputIntent.GetPdfObject());
        }

        /// <summary>Checks whether PDF document conforms a specific standard.</summary>
        /// <remarks>
        /// Checks whether PDF document conforms a specific standard.
        /// Shall be override.
        /// </remarks>
        /// <param name="obj">An object to conform.</param>
        /// <param name="key">type of object to conform.</param>
        public virtual void CheckIsoConformance(object obj, IsoKey key) {
        }

        /// <summary>Checks whether PDF document conforms a specific standard.</summary>
        /// <remarks>
        /// Checks whether PDF document conforms a specific standard.
        /// Shall be override.
        /// </remarks>
        /// <param name="obj">an object to conform.</param>
        /// <param name="key">type of object to conform.</param>
        /// <param name="resources">
        /// 
        /// <see cref="PdfResources"/>
        /// associated with an object to check.
        /// </param>
        [Obsolete(@"This method will be replaced by CheckIsoConformance(System.Object, IsoKey, PdfResources, PdfStream) checkIsoConformance in  7.2 release"
            )]
        public virtual void CheckIsoConformance(object obj, IsoKey key, PdfResources resources) {
        }

        /// <summary>Checks whether PDF document conforms a specific standard.</summary>
        /// <remarks>
        /// Checks whether PDF document conforms a specific standard.
        /// Shall be override.
        /// </remarks>
        /// <param name="obj">an object to conform.</param>
        /// <param name="key">type of object to conform.</param>
        /// <param name="resources">
        /// 
        /// <see cref="PdfResources"/>
        /// associated with an object to check.
        /// </param>
        /// <param name="contentStream">current content stream</param>
        public virtual void CheckIsoConformance(object obj, IsoKey key, PdfResources resources, PdfStream contentStream
            ) {
        }

        /// <summary>Checks whether PDF document conforms a specific standard.</summary>
        /// <remarks>
        /// Checks whether PDF document conforms a specific standard.
        /// Shall be override.
        /// </remarks>
        /// <param name="gState">
        /// a
        /// <see cref="CanvasGraphicsState"/>
        /// object to conform.
        /// </param>
        /// <param name="resources">
        /// 
        /// <see cref="PdfResources"/>
        /// associated with an object to check.
        /// </param>
        public virtual void CheckShowTextIsoConformance(CanvasGraphicsState gState, PdfResources resources) {
        }

        /// <summary>Adds file attachment at document level.</summary>
        /// <param name="key">name of the destination.</param>
        /// <param name="fs">
        /// 
        /// <see cref="PdfFileSpec"/>
        /// object.
        /// </param>
        public virtual void AddFileAttachment(string key, PdfFileSpec fs) {
            CheckClosingStatus();
            catalog.AddNameToNameTree(key, fs.GetPdfObject(), PdfName.EmbeddedFiles);
        }

        /// <summary>Adds file associated with PDF document as a whole and identifies the relationship between them.</summary>
        /// <remarks>
        /// Adds file associated with PDF document as a whole and identifies the relationship between them.
        /// <para />
        /// Associated files may be used in Pdf/A-3 and Pdf 2.0 documents.
        /// The method is very similar to
        /// <see cref="AddFileAttachment(System.String, PdfFileSpec)"/>.
        /// However, besides adding file description to Names tree, it adds file to array value of the AF key in the document catalog.
        /// <para />
        /// For associated files their associated file specification dictionaries shall include the AFRelationship key
        /// </remarks>
        /// <param name="description">the file description</param>
        /// <param name="fs">file specification dictionary of associated file</param>
        /// <seealso cref="AddFileAttachment(System.String, PdfFileSpec)"/>
        public virtual void AddAssociatedFile(string description, PdfFileSpec fs) {
            if (null == ((PdfDictionary)fs.GetPdfObject()).Get(PdfName.AFRelationship)) {
                var logger = LogManager.GetLogger(typeof(PdfDocument));
                logger.Error(LogMessageConstant.ASSOCIATED_FILE_SPEC_SHALL_INCLUDE_AFRELATIONSHIP);
            }
            var afArray = catalog.GetPdfObject().GetAsArray(PdfName.AF);
            if (afArray == null) {
                afArray = (PdfArray)new PdfArray().MakeIndirect(this);
                catalog.Put(PdfName.AF, afArray);
            }
            afArray.Add(fs.GetPdfObject());
            AddFileAttachment(description, fs);
        }

        /// <summary>Returns files associated with PDF document.</summary>
        /// <returns>associated files array.</returns>
        public virtual PdfArray GetAssociatedFiles() {
            CheckClosingStatus();
            return catalog.GetPdfObject().GetAsArray(PdfName.AF);
        }

        /// <summary>
        /// Gets the encrypted payload of this document,
        /// or returns
        /// <see langword="null"/>
        /// if this document isn't an unencrypted wrapper document.
        /// </summary>
        /// <returns>encrypted payload of this document.</returns>
        public virtual PdfEncryptedPayloadDocument GetEncryptedPayloadDocument() {
            if (GetReader() != null && GetReader().IsEncrypted()) {
                return null;
            }
            var collection = GetCatalog().GetCollection();
            if (collection != null && collection.IsViewHidden()) {
                var documentName = collection.GetInitialDocument();
                var embeddedFiles = GetCatalog().GetNameTree(PdfName.EmbeddedFiles);
                var documentNameUnicode = documentName.ToUnicodeString();
                var fileSpecObject = embeddedFiles.GetNames().Get(documentNameUnicode);
                if (fileSpecObject != null && fileSpecObject.IsDictionary()) {
                    try {
                        var fileSpec = PdfEncryptedPayloadFileSpecFactory.Wrap((PdfDictionary)fileSpecObject);
                        if (fileSpec != null) {
                            var embeddedDictionary = ((PdfDictionary)fileSpec.GetPdfObject()).GetAsDictionary(PdfName.EF);
                            var stream = embeddedDictionary.GetAsStream(PdfName.UF);
                            if (stream == null) {
                                stream = embeddedDictionary.GetAsStream(PdfName.F);
                            }
                            if (stream != null) {
                                return new PdfEncryptedPayloadDocument(stream, fileSpec, documentNameUnicode);
                            }
                        }
                    }
                    catch (PdfException e) {
                        LogManager.GetLogger(GetType()).Error(e.Message);
                    }
                }
            }
            return null;
        }

        /// <summary>Sets an encrypted payload, making this document an unencrypted wrapper document.</summary>
        /// <remarks>
        /// Sets an encrypted payload, making this document an unencrypted wrapper document.
        /// The file spec shall include the AFRelationship key with a value of EncryptedPayload,
        /// and shall include an encrypted payload dictionary.
        /// </remarks>
        /// <param name="fs">
        /// encrypted payload file spec.
        /// <see cref="PdfEncryptedPayloadFileSpecFactory"/>
        /// can produce one.
        /// </param>
        public virtual void SetEncryptedPayload(PdfFileSpec fs) {
            if (GetWriter() == null) {
                throw new PdfException(PdfException.CannotSetEncryptedPayloadToDocumentOpenedInReadingMode);
            }
            if (WriterHasEncryption()) {
                throw new PdfException(PdfException.CannotSetEncryptedPayloadToEncryptedDocument);
            }
            if (!PdfName.EncryptedPayload.Equals(((PdfDictionary)fs.GetPdfObject()).Get(PdfName.AFRelationship))) {
                LogManager.GetLogger(GetType()).Error(LogMessageConstant.ENCRYPTED_PAYLOAD_FILE_SPEC_SHALL_HAVE_AFRELATIONSHIP_FILED_EQUAL_TO_ENCRYPTED_PAYLOAD
                    );
            }
            var encryptedPayload = PdfEncryptedPayload.ExtractFrom(fs);
            if (encryptedPayload == null) {
                throw new PdfException(PdfException.EncryptedPayloadFileSpecDoesntHaveEncryptedPayloadDictionary);
            }
            var collection = GetCatalog().GetCollection();
            if (collection != null) {
                LogManager.GetLogger(GetType()).Warn(LogMessageConstant.COLLECTION_DICTIONARY_ALREADY_EXISTS_IT_WILL_BE_MODIFIED
                    );
            }
            else {
                collection = new PdfCollection();
                GetCatalog().SetCollection(collection);
            }
            collection.SetView(PdfCollection.HIDDEN);
            var displayName = PdfEncryptedPayloadFileSpecFactory.GenerateFileDisplay(encryptedPayload);
            collection.SetInitialDocument(displayName);
            AddAssociatedFile(displayName, fs);
        }

        /// <summary>This method retrieves the page labels from a document as an array of String objects.</summary>
        /// <returns>
        /// 
        /// <see cref="System.String"/>
        /// list of page labels if they were found, or
        /// <see langword="null"/>
        /// otherwise
        /// </returns>
        public virtual string[] GetPageLabels() {
            if (catalog.GetPageLabelsTree(false) == null) {
                return null;
            }
            var pageLabels = catalog.GetPageLabelsTree(false).GetNumbers();
            if (pageLabels.Count == 0) {
                return null;
            }
            var labelStrings = new string[GetNumberOfPages()];
            var pageCount = 1;
            var prefix = "";
            var type = "D";
            for (var i = 0; i < GetNumberOfPages(); i++) {
                if (pageLabels.ContainsKey(i)) {
                    var labelDictionary = (PdfDictionary)pageLabels.Get(i);
                    var pageRange = labelDictionary.GetAsNumber(PdfName.St);
                    if (pageRange != null) {
                        pageCount = pageRange.IntValue();
                    }
                    else {
                        pageCount = 1;
                    }
                    var p = labelDictionary.GetAsString(PdfName.P);
                    if (p != null) {
                        prefix = p.ToUnicodeString();
                    }
                    else {
                        prefix = "";
                    }
                    var t = labelDictionary.GetAsName(PdfName.S);
                    if (t != null) {
                        type = t.GetValue();
                    }
                    else {
                        type = "e";
                    }
                }
                switch (type) {
                    case "R": {
                        labelStrings[i] = prefix + RomanNumbering.ToRomanUpperCase(pageCount);
                        break;
                    }

                    case "r": {
                        labelStrings[i] = prefix + RomanNumbering.ToRomanLowerCase(pageCount);
                        break;
                    }

                    case "A": {
                        labelStrings[i] = prefix + EnglishAlphabetNumbering.ToLatinAlphabetNumberUpperCase(pageCount);
                        break;
                    }

                    case "a": {
                        labelStrings[i] = prefix + EnglishAlphabetNumbering.ToLatinAlphabetNumberLowerCase(pageCount);
                        break;
                    }

                    case "e": {
                        labelStrings[i] = prefix;
                        break;
                    }

                    default: {
                        labelStrings[i] = prefix + pageCount;
                        break;
                    }
                }
                pageCount++;
            }
            return labelStrings;
        }

        /// <summary>Indicates if the document has any outlines</summary>
        /// <returns>
        /// 
        /// <see langword="true"/>
        /// , if there are outlines and
        /// <see langword="false"/>
        /// otherwise.
        /// </returns>
        public virtual bool HasOutlines() {
            return catalog.HasOutlines();
        }

        /// <summary>Sets the flag indicating the presence of structure elements that contain user properties attributes.
        ///     </summary>
        /// <param name="userProperties">the user properties flag</param>
        public virtual void SetUserProperties(bool userProperties) {
            var userPropsVal = userProperties ? PdfBoolean.TRUE : PdfBoolean.FALSE;
            UpdateValueInMarkInfoDict(PdfName.UserProperties, userPropsVal);
        }

        /// <summary>
        /// Create a new instance of
        /// <see cref="PdfFont"/>
        /// or load already created one.
        /// </summary>
        /// <param name="dictionary">
        /// 
        /// <see cref="PdfDictionary"/>
        /// that presents
        /// <see cref="PdfFont"/>.
        /// </param>
        /// <returns>
        /// instance of
        /// <see cref="PdfFont"/>
        /// <para />
        /// Note, PdfFont which created with
        /// <see cref="PdfFontFactory.CreateFont(PdfDictionary)"/>
        /// won't be cached
        /// until it will be added to
        /// <see cref="PdfCanvas"/>
        /// or
        /// <see cref="PdfResources"/>.
        /// </returns>
        public virtual PdfFont GetFont(PdfDictionary dictionary) {
            var indirectReference = dictionary.GetIndirectReference();
            if (indirectReference != null && documentFonts.ContainsKey(indirectReference)) {
                return documentFonts.Get(indirectReference);
            }

            return AddFont(PdfFontFactory.CreateFont(dictionary));
        }

        /// <summary>Gets default font for the document: Helvetica, WinAnsi.</summary>
        /// <remarks>
        /// Gets default font for the document: Helvetica, WinAnsi.
        /// One instance per document.
        /// </remarks>
        /// <returns>
        /// instance of
        /// <see cref="PdfFont"/>
        /// or
        /// <see langword="null"/>
        /// on error.
        /// </returns>
        public virtual PdfFont GetDefaultFont() {
            if (defaultFont == null) {
                try {
                    defaultFont = PdfFontFactory.CreateFont();
                    if (writer != null) {
                        defaultFont.MakeIndirect(this);
                    }
                }
                catch (IOException e) {
                    var logger = LogManager.GetLogger(typeof(PdfDocument));
                    logger.Error(LogMessageConstant.EXCEPTION_WHILE_CREATING_DEFAULT_FONT, e);
                    defaultFont = null;
                }
            }
            return defaultFont;
        }

        /// <summary>
        /// Adds a
        /// <see cref="PdfFont"/>
        /// instance to this document so that this font is flushed automatically
        /// on document close.
        /// </summary>
        /// <remarks>
        /// Adds a
        /// <see cref="PdfFont"/>
        /// instance to this document so that this font is flushed automatically
        /// on document close. As a side effect, the underlying font dictionary is made indirect if it wasn't the case yet
        /// </remarks>
        /// <param name="font">
        /// a
        /// <see cref="PdfFont"/>
        /// instance to add
        /// </param>
        /// <returns>the same PdfFont instance.</returns>
        public virtual PdfFont AddFont(PdfFont font) {
            font.MakeIndirect(this);
            // forbid release for font dictionaries that are stored in #documentFonts collection
            font.SetForbidRelease();
            documentFonts.Put(font.GetPdfObject().GetIndirectReference(), font);
            return font;
        }

        /// <summary>Registers a product for debugging purposes.</summary>
        /// <param name="productInfo">product to be registered.</param>
        /// <returns>true if the product hadn't been registered before.</returns>
        public virtual bool RegisterProduct(ProductInfo productInfo) {
            return fingerPrint.RegisterProduct(productInfo);
        }

        /// <summary>Returns the object containing the registered products.</summary>
        /// <returns>fingerprint object</returns>
        public virtual FingerPrint GetFingerPrint() {
            return fingerPrint;
        }

        /// <summary>
        /// Find
        /// <see cref="PdfFont"/>
        /// from loaded fonts with corresponding fontProgram and encoding or CMAP.
        /// </summary>
        /// <param name="fontProgram">a font name or path to a font program</param>
        /// <param name="encoding">an encoding or CMAP</param>
        /// <returns>the font instance, or null if font wasn't found</returns>
        public virtual PdfFont FindFont(string fontProgram, string encoding) {
            foreach (var font in documentFonts.Values) {
                if (!font.IsFlushed() && font.IsBuiltWith(fontProgram, encoding)) {
                    return font;
                }
            }
            return null;
        }

        /// <summary>Gets list of indirect references.</summary>
        /// <returns>list of indirect references.</returns>
        internal virtual PdfXrefTable GetXref() {
            return xref;
        }

        internal virtual bool IsDocumentFont(PdfIndirectReference indRef) {
            return indRef != null && documentFonts.ContainsKey(indRef);
        }

        /// <summary>
        /// Initialize
        /// <see cref="TagStructureContext"/>.
        /// </summary>
        protected internal virtual void InitTagStructureContext() {
            tagStructureContext = new TagStructureContext(this);
        }

        /// <summary>Save the link annotation in a temporary storage for further copying.</summary>
        /// <param name="page">
        /// just copied
        /// <see cref="PdfPage"/>
        /// link annotation belongs to.
        /// </param>
        /// <param name="annotation">
        /// 
        /// <see cref="PdfLinkAnnotation"/>
        /// itself.
        /// </param>
        protected internal virtual void StoreLinkAnnotation(PdfPage page, PdfLinkAnnotation annotation) {
            var pageAnnotations = linkAnnotations.Get(page);
            if (pageAnnotations == null) {
                pageAnnotations = new List<PdfLinkAnnotation>();
                linkAnnotations.Put(page, pageAnnotations);
            }
            pageAnnotations.Add(annotation);
        }

        /// <summary>Checks whether PDF document conforms a specific standard.</summary>
        /// <remarks>
        /// Checks whether PDF document conforms a specific standard.
        /// Shall be override.
        /// </remarks>
        protected internal virtual void CheckIsoConformance() {
        }

        /// <summary>
        /// Mark an object with
        /// <see cref="PdfObject.MUST_BE_FLUSHED"/>.
        /// </summary>
        /// <param name="pdfObject">an object to mark.</param>
        protected internal virtual void MarkObjectAsMustBeFlushed(PdfObject pdfObject) {
            if (pdfObject.GetIndirectReference() != null) {
                pdfObject.GetIndirectReference().SetState(PdfObject.MUST_BE_FLUSHED);
            }
        }

        /// <summary>Flush an object.</summary>
        /// <param name="pdfObject">object to flush.</param>
        /// <param name="canBeInObjStm">indicates whether object can be placed into object stream.</param>
        protected internal virtual void FlushObject(PdfObject pdfObject, bool canBeInObjStm) {
            writer.FlushObject(pdfObject, canBeInObjStm);
        }

        /// <summary>Initializes document.</summary>
        /// <param name="newPdfVersion">
        /// new pdf version of the resultant file if stamper is used and the version needs to be changed,
        /// or
        /// <see langword="null"/>
        /// otherwise
        /// </param>
        protected internal virtual void Open(PdfVersion newPdfVersion) {
            fingerPrint = new FingerPrint();
            encryptedEmbeddedStreamsHandler = new EncryptedEmbeddedStreamsHandler(this);
            try {
                EventCounterHandler.GetInstance().OnEvent(CoreEvent.PROCESS, properties.metaInfo, GetType());
                var embeddedStreamsSavedOnReading = false;
                if (reader != null) {
                    if (reader.pdfDocument != null) {
                        throw new PdfException(PdfException.PdfReaderHasBeenAlreadyUtilized);
                    }
                    reader.pdfDocument = this;
                    memoryLimitsAwareHandler = reader.properties.memoryLimitsAwareHandler;
                    if (null == memoryLimitsAwareHandler) {
                        memoryLimitsAwareHandler = new MemoryLimitsAwareHandler(reader.tokens.GetSafeFile().Length());
                    }
                    reader.ReadPdf();
                    if (reader.decrypt != null && reader.decrypt.IsEmbeddedFilesOnly()) {
                        encryptedEmbeddedStreamsHandler.StoreAllEmbeddedStreams();
                        embeddedStreamsSavedOnReading = true;
                    }
                    foreach (var counter in GetCounters()) {
                        counter.OnDocumentRead(reader.GetFileLength());
                    }
                    pdfVersion = reader.headerPdfVersion;
                    trailer = new PdfDictionary(reader.trailer);
                    ReadDocumentIds();
                    catalog = new PdfCatalog((PdfDictionary)trailer.Get(PdfName.Root, true));
                    UpdatePdfVersionFromCatalog();
                    var xmpMetadataStream = catalog.GetPdfObject().GetAsStream(PdfName.Metadata);
                    if (xmpMetadataStream != null) {
                        xmpMetadata = xmpMetadataStream.GetBytes();
                        if (!GetType().Equals(typeof(PdfDocument))) {
                            // TODO DEVSIX-5292 If somebody extends PdfDocument we have to initialize document info
                            //  and conformance level to provide compatibility. This code block shall be removed
                            reader.GetPdfAConformanceLevel();
                            GetDocumentInfo();
                        }
                    }
                    var str = catalog.GetPdfObject().GetAsDictionary(PdfName.StructTreeRoot);
                    if (str != null) {
                        TryInitTagStructure(str);
                    }
                    if (properties.appendMode && (reader.HasRebuiltXref() || reader.HasFixedXref())) {
                        throw new PdfException(PdfException.AppendModeRequiresADocumentWithoutErrorsEvenIfRecoveryWasPossible);
                    }
                }
                xref.InitFreeReferencesList(this);
                if (writer != null) {
                    if (reader != null && reader.HasXrefStm() && writer.properties.isFullCompression == null) {
                        writer.properties.isFullCompression = true;
                    }
                    if (reader != null && !reader.IsOpenedWithFullPermission()) {
                        throw new BadPasswordException(BadPasswordException.PdfReaderNotOpenedWithOwnerPassword);
                    }
                    if (reader != null && properties.preserveEncryption) {
                        writer.crypto = reader.decrypt;
                    }
                    writer.document = this;
                    if (reader == null) {
                        catalog = new PdfCatalog(this);
                        info = new PdfDocumentInfo(this).AddCreationDate();
                    }
                    UpdateProducerInInfoDictionary();
                    GetDocumentInfo().AddModDate();
                    trailer = new PdfDictionary();
                    trailer.Put(PdfName.Root, catalog.GetPdfObject().GetIndirectReference());
                    trailer.Put(PdfName.Info, GetDocumentInfo().GetPdfObject().GetIndirectReference());
                    if (reader != null) {
                        // If the reader's trailer contains an ID entry, let's copy it over to the new trailer
                        if (reader.trailer.ContainsKey(PdfName.ID)) {
                            trailer.Put(PdfName.ID, reader.trailer.Get(PdfName.ID));
                        }
                    }
                    if (writer.properties != null) {
                        var readerModifiedId = modifiedDocumentId;
                        if (writer.properties.initialDocumentId != null && !(reader != null && reader.decrypt != null && (properties
                            .appendMode || properties.preserveEncryption))) {
                            originalDocumentId = writer.properties.initialDocumentId;
                        }
                        if (writer.properties.modifiedDocumentId != null) {
                            modifiedDocumentId = writer.properties.modifiedDocumentId;
                        }
                        if (originalDocumentId == null && modifiedDocumentId != null) {
                            originalDocumentId = modifiedDocumentId;
                        }
                        if (modifiedDocumentId == null) {
                            if (originalDocumentId == null) {
                                originalDocumentId = new PdfString(PdfEncryption.GenerateNewDocumentId());
                            }
                            modifiedDocumentId = originalDocumentId;
                        }
                        if (writer.properties.modifiedDocumentId == null && modifiedDocumentId.Equals(readerModifiedId)) {
                            modifiedDocumentId = new PdfString(PdfEncryption.GenerateNewDocumentId());
                        }
                    }
                    Debug.Assert(originalDocumentId != null);
                    Debug.Assert(modifiedDocumentId != null);
                }
                if (properties.appendMode) {
                    // Due to constructor reader and writer not null.
                    Debug.Assert(reader != null);
                    var file = reader.tokens.GetSafeFile();
                    int n;
                    var buffer = new byte[8192];
                    while ((n = file.Read(buffer)) > 0) {
                        writer.Write(buffer, 0, n);
                    }
                    file.Close();
                    writer.Write((byte)'\n');
                    OverrideFullCompressionInWriterProperties(writer.properties, reader.HasXrefStm());
                    writer.crypto = reader.decrypt;
                    if (newPdfVersion != null) {
                        // In PDF 1.4, a PDF version can also be specified in the Version entry of the document catalog,
                        // essentially updating the version associated with the file by overriding the one specified in the file header
                        if (pdfVersion.CompareTo(PdfVersion.PDF_1_4) >= 0) {
                            // If the header specifies a later version, or if this entry is absent, the document conforms to the
                            // version specified in the header.
                            // So only update the version if it is older than the one in the header
                            if (newPdfVersion.CompareTo(reader.headerPdfVersion) > 0) {
                                catalog.Put(PdfName.Version, newPdfVersion.ToPdfName());
                                catalog.SetModified();
                                pdfVersion = newPdfVersion;
                            }
                        }
                    }
                }
                else {
                    // Formally we cannot update version in the catalog as it is not supported for the
                    // PDF version of the original document
                    if (writer != null) {
                        if (newPdfVersion != null) {
                            pdfVersion = newPdfVersion;
                        }
                        writer.WriteHeader();
                        if (writer.crypto == null) {
                            writer.InitCryptoIfSpecified(pdfVersion);
                        }
                        if (writer.crypto != null) {
                            if (!embeddedStreamsSavedOnReading && writer.crypto.IsEmbeddedFilesOnly()) {
                                encryptedEmbeddedStreamsHandler.StoreAllEmbeddedStreams();
                            }
                            if (writer.crypto.GetCryptoMode() < EncryptionConstants.ENCRYPTION_AES_256) {
                                VersionConforming.ValidatePdfVersionForDeprecatedFeatureLogWarn(this, PdfVersion.PDF_2_0, VersionConforming
                                    .DEPRECATED_ENCRYPTION_ALGORITHMS);
                            }
                            else {
                                if (writer.crypto.GetCryptoMode() == EncryptionConstants.ENCRYPTION_AES_256) {
                                    var r = writer.crypto.GetPdfObject().GetAsNumber(PdfName.R);
                                    if (r != null && r.IntValue() == 5) {
                                        VersionConforming.ValidatePdfVersionForDeprecatedFeatureLogWarn(this, PdfVersion.PDF_2_0, VersionConforming
                                            .DEPRECATED_AES256_REVISION);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException e) {
                throw new PdfException(PdfException.CannotOpenDocument, e, this);
            }
        }

        /// <summary>Adds custom XMP metadata extension.</summary>
        /// <remarks>Adds custom XMP metadata extension. Useful for PDF/UA, ZUGFeRD, etc.</remarks>
        /// <param name="xmpMeta">
        /// 
        /// <see cref="XMPMeta"/>
        /// to add custom metadata to.
        /// </param>
        protected internal virtual void AddCustomMetadataExtensions(XMPMeta xmpMeta) {
        }

        /// <summary>Updates XMP metadata.</summary>
        /// <remarks>
        /// Updates XMP metadata.
        /// Shall be override.
        /// </remarks>
        protected internal virtual void UpdateXmpMetadata() {
            try {
                // We add PDF producer info in any case, and the valid way to do it for PDF 2.0 in only in metadata, not in the info dictionary.
                if (xmpMetadata != null || writer.properties.addXmpMetadata || pdfVersion.CompareTo(PdfVersion.PDF_2_0) >=
                     0) {
                    SetXmpMetadata(UpdateDefaultXmpMetadata());
                }
            }
            catch (XMPException e) {
                var logger = LogManager.GetLogger(typeof(PdfDocument));
                logger.Error(LogMessageConstant.EXCEPTION_WHILE_UPDATING_XMPMETADATA, e);
            }
        }

        /// <summary>
        /// Update XMP metadata values from
        /// <see cref="PdfDocumentInfo"/>.
        /// </summary>
        /// <returns>the XMPMetadata</returns>
        protected internal virtual XMPMeta UpdateDefaultXmpMetadata() {
            var xmpMeta = XMPMetaFactory.ParseFromBuffer(GetXmpMetadata(true));
            XmpMetaInfoConverter.AppendDocumentInfoToMetadata(GetDocumentInfo(), xmpMeta);
            if (IsTagged() && writer.properties.addUAXmpMetadata && !IsXmpMetaHasProperty(xmpMeta, XMPConst.NS_PDFUA_ID
                , XMPConst.PART)) {
                xmpMeta.SetPropertyInteger(XMPConst.NS_PDFUA_ID, XMPConst.PART, 1, new PropertyOptions(PropertyOptions.SEPARATE_NODE
                    ));
            }
            return xmpMeta;
        }

        /// <summary>List all newly added or loaded fonts</summary>
        /// <returns>
        /// List of
        /// <see cref="PdfFont"/>.
        /// </returns>
        protected internal virtual ICollection<PdfFont> GetDocumentFonts() {
            return documentFonts.Values;
        }

        /// <summary>Flushes all newly added or loaded fonts.</summary>
        protected internal virtual void FlushFonts() {
            if (properties.appendMode) {
                foreach (var font in GetDocumentFonts()) {
                    if (font.GetPdfObject().CheckState(PdfObject.MUST_BE_INDIRECT) || font.GetPdfObject().GetIndirectReference
                        ().CheckState(PdfObject.MODIFIED)) {
                        font.Flush();
                    }
                }
            }
            else {
                foreach (var font in GetDocumentFonts()) {
                    font.Flush();
                }
            }
        }

        /// <summary>Checks page before adding and add.</summary>
        /// <param name="index">one-base index of the page.</param>
        /// <param name="page">
        /// 
        /// <see cref="PdfPage"/>
        /// to add.
        /// </param>
        protected internal virtual void CheckAndAddPage(int index, PdfPage page) {
            if (page.IsFlushed()) {
                throw new PdfException(PdfException.FlushedPageCannotBeAddedOrInserted, page);
            }
            if (page.GetDocument() != null && this != page.GetDocument()) {
                throw new PdfException(PdfException.Page1CannotBeAddedToDocument2BecauseItBelongsToDocument3).SetMessageParams
                    (page, this, page.GetDocument());
            }
            catalog.GetPageTree().AddPage(index, page);
        }

        /// <summary>Checks page before adding.</summary>
        /// <param name="page">
        /// 
        /// <see cref="PdfPage"/>
        /// to add.
        /// </param>
        protected internal virtual void CheckAndAddPage(PdfPage page) {
            if (page.IsFlushed()) {
                throw new PdfException(PdfException.FlushedPageCannotBeAddedOrInserted, page);
            }
            if (page.GetDocument() != null && this != page.GetDocument()) {
                throw new PdfException(PdfException.Page1CannotBeAddedToDocument2BecauseItBelongsToDocument3).SetMessageParams
                    (page, this, page.GetDocument());
            }
            catalog.GetPageTree().AddPage(page);
        }

        /// <summary>checks whether a method is invoked at the closed document</summary>
        protected internal virtual void CheckClosingStatus() {
            if (closed) {
                throw new PdfException(PdfException.DocumentClosedItIsImpossibleToExecuteAction);
            }
        }

        /// <summary>
        /// Gets all
        /// <see cref="ICounter"/>
        /// instances.
        /// </summary>
        /// <returns>
        /// list of
        /// <see cref="ICounter"/>
        /// instances.
        /// </returns>
        [Obsolete]
        protected internal virtual IList<ICounter> GetCounters() {
            return CounterManager.GetInstance().GetCounters(typeof(PdfDocument));
        }

        /// <summary>Returns the factory for creating page instances.</summary>
        /// <returns>
        /// implementation of
        /// <see cref="IPdfPageFactory"/>
        /// for current document
        /// </returns>
        protected internal virtual IPdfPageFactory GetPageFactory() {
            return pdfPageFactory;
        }

        internal virtual long GetDocumentId() {
            return documentId;
        }

        internal virtual bool DoesStreamBelongToEmbeddedFile(PdfStream stream) {
            return encryptedEmbeddedStreamsHandler.IsStreamStoredAsEmbedded(stream);
        }

        /// <summary>Gets iText version info.</summary>
        /// <returns>iText version info.</returns>
        internal VersionInfo GetVersionInfo() {
            return versionInfo;
        }

        internal virtual bool HasAcroForm() {
            return GetCatalog().GetPdfObject().ContainsKey(PdfName.AcroForm);
        }

        private void UpdateProducerInInfoDictionary() {
            string producer = null;
            var documentInfoObject = GetDocumentInfo().GetPdfObject();
            if (reader == null) {
                producer = versionInfo.GetVersion();
            }
            else {
                if (documentInfoObject.ContainsKey(PdfName.Producer)) {
                    var producerPdfStr = documentInfoObject.GetAsString(PdfName.Producer);
                    producer = producerPdfStr == null ? null : producerPdfStr.ToUnicodeString();
                }
                producer = AddModifiedPostfix(producer);
            }
            documentInfoObject.Put(PdfName.Producer, new PdfString(producer, PdfEncodings.UNICODE_BIG));
        }

        /// <summary>
        /// Initializes the new instance of document's structure tree root
        /// <see cref="PdfStructTreeRoot"/>.
        /// </summary>
        /// <remarks>
        /// Initializes the new instance of document's structure tree root
        /// <see cref="PdfStructTreeRoot"/>.
        /// See ISO 32000-1, section 14.7.2 Structure Hierarchy.
        /// </remarks>
        /// <param name="str">dictionary to create structure tree root</param>
        protected internal virtual void TryInitTagStructure(PdfDictionary str) {
            try {
                structTreeRoot = new PdfStructTreeRoot(str, this);
                structParentIndex = GetStructTreeRoot().GetParentTreeNextKey();
            }
            catch (Exception ex) {
                structTreeRoot = null;
                structParentIndex = -1;
                var logger = LogManager.GetLogger(typeof(PdfDocument));
                logger.Error(LogMessageConstant.TAG_STRUCTURE_INIT_FAILED, ex);
            }
        }

        private void TryFlushTagStructure(bool isAppendMode) {
            try {
                if (tagStructureContext != null) {
                    tagStructureContext.PrepareToDocumentClosing();
                }
                if (!isAppendMode || structTreeRoot.GetPdfObject().IsModified()) {
                    structTreeRoot.Flush();
                }
            }
            catch (Exception ex) {
                throw new PdfException(PdfException.TagStructureFlushingFailedItMightBeCorrupted, ex);
            }
        }

        private void UpdateValueInMarkInfoDict(PdfName key, PdfObject value) {
            var markInfo = catalog.GetPdfObject().GetAsDictionary(PdfName.MarkInfo);
            if (markInfo == null) {
                markInfo = new PdfDictionary();
                catalog.GetPdfObject().Put(PdfName.MarkInfo, markInfo);
            }
            markInfo.Put(key, value);
        }

        /// <summary>This method removes all annotation entries from form fields associated with a given page.</summary>
        /// <param name="page">to remove from.</param>
        private void RemoveUnusedWidgetsFromFields(PdfPage page) {
            if (page.IsFlushed()) {
                return;
            }
            var annots = page.GetAnnotations();
            foreach (var annot in annots) {
                if (annot.GetSubtype().Equals(PdfName.Widget)) {
                    ((PdfWidgetAnnotation)annot).ReleaseFormFieldFromWidgetAnnotation();
                }
            }
        }

        private void CopyLinkAnnotations(PdfDocument toDocument, IDictionary<PdfPage, PdfPage> page2page
            ) {
            IList<PdfName> excludedKeys = new List<PdfName>();
            excludedKeys.Add(PdfName.Dest);
            excludedKeys.Add(PdfName.A);
            foreach (var entry in linkAnnotations) {
                // We don't want to copy those link annotations, which reference to pages which weren't copied.
                foreach (var annot in entry.Value) {
                    var toCopyAnnot = true;
                    PdfDestination copiedDest = null;
                    PdfDictionary copiedAction = null;
                    var dest = annot.GetDestinationObject();
                    if (dest != null) {
                        // If link annotation has destination object, we try to copy this destination.
                        // Destination is not copied if it points to the not copied page, and therefore the whole
                        // link annotation is not copied.
                        copiedDest = GetCatalog().CopyDestination(dest, page2page, toDocument);
                        toCopyAnnot = copiedDest != null;
                    }
                    else {
                        // Link annotation may have associated action. If it is GoTo type, we try to copy it's destination.
                        // GoToR and GoToE also contain destinations, but they point not to pages of the current document,
                        // so we just copy them as is. If it is action of any other type, it is also just copied as is.
                        var action = annot.GetAction();
                        if (action != null) {
                            if (PdfName.GoTo.Equals(action.Get(PdfName.S))) {
                                copiedAction = action.CopyTo(toDocument, JavaUtil.ArraysAsList(PdfName.D), false);
                                var goToDest = GetCatalog().CopyDestination(action.Get(PdfName.D), page2page, toDocument);
                                if (goToDest != null) {
                                    copiedAction.Put(PdfName.D, goToDest.GetPdfObject());
                                }
                                else {
                                    toCopyAnnot = false;
                                }
                            }
                            else {
                                copiedAction = (PdfDictionary)action.CopyTo(toDocument, false);
                            }
                        }
                    }
                    if (toCopyAnnot) {
                        var newAnnot = (PdfLinkAnnotation)PdfAnnotation.MakeAnnotation(annot.GetPdfObject().CopyTo(toDocument
                            , excludedKeys, true));
                        if (copiedDest != null) {
                            newAnnot.SetDestination(copiedDest);
                        }
                        if (copiedAction != null) {
                            newAnnot.SetAction(copiedAction);
                        }
                        entry.Key.AddAnnotation(-1, newAnnot, false);
                    }
                }
            }
            linkAnnotations.Clear();
        }

        /// <summary>This method copies all given outlines</summary>
        /// <param name="outlines">outlines to be copied</param>
        /// <param name="toDocument">document where outlines should be copied</param>
        private void CopyOutlines(ICollection<PdfOutline> outlines, PdfDocument toDocument, IDictionary
            <PdfPage, PdfPage> page2page) {
            ICollection<PdfOutline> outlinesToCopy = new HashSet<PdfOutline>();
            outlinesToCopy.AddAll(outlines);
            foreach (var outline in outlines) {
                GetAllOutlinesToCopy(outline, outlinesToCopy);
            }
            var rootOutline = toDocument.GetOutlines(false);
            if (rootOutline == null) {
                rootOutline = new PdfOutline(toDocument);
                rootOutline.SetTitle("Outlines");
            }
            CloneOutlines(outlinesToCopy, rootOutline, GetOutlines(false), page2page, toDocument);
        }

        /// <summary>This method gets all outlines to be copied including parent outlines</summary>
        /// <param name="outline">current outline</param>
        /// <param name="outlinesToCopy">a Set of outlines to be copied</param>
        private void GetAllOutlinesToCopy(PdfOutline outline, ICollection<PdfOutline> outlinesToCopy) {
            var parent = outline.GetParent();
            //note there's no need to continue recursion if the current outline parent is root (first condition) or
            // if it is already in the Set of outlines to be copied (second condition)
            if ("Outlines".Equals(parent.GetTitle()) || outlinesToCopy.Contains(parent)) {
                return;
            }
            outlinesToCopy.Add(parent);
            GetAllOutlinesToCopy(parent, outlinesToCopy);
        }

        /// <summary>This method copies create new outlines in the Document to copy.</summary>
        /// <param name="outlinesToCopy">- Set of outlines to be copied</param>
        /// <param name="newParent">- new parent outline</param>
        /// <param name="oldParent">- old parent outline</param>
        private void CloneOutlines(ICollection<PdfOutline> outlinesToCopy, PdfOutline newParent, PdfOutline oldParent
            , IDictionary<PdfPage, PdfPage> page2page, PdfDocument toDocument) {
            if (null == oldParent) {
                return;
            }
            foreach (var outline in oldParent.GetAllChildren()) {
                if (outlinesToCopy.Contains(outline)) {
                    PdfDestination copiedDest = null;
                    if (null != outline.GetDestination()) {
                        var destObjToCopy = outline.GetDestination().GetPdfObject();
                        copiedDest = GetCatalog().CopyDestination(destObjToCopy, page2page, toDocument);
                    }
                    var child = newParent.AddOutline(outline.GetTitle());
                    if (copiedDest != null) {
                        child.AddDestination(copiedDest);
                    }
                    CloneOutlines(outlinesToCopy, child, outline, page2page, toDocument);
                }
            }
        }

        private void EnsureTreeRootAddedToNames(PdfObject treeRoot, PdfName treeType) {
            var names = catalog.GetPdfObject().GetAsDictionary(PdfName.Names);
            if (names == null) {
                names = new PdfDictionary();
                catalog.Put(PdfName.Names, names);
                names.MakeIndirect(this);
            }
            names.Put(treeType, treeRoot);
            names.SetModified();
        }

        private bool WriterHasEncryption() {
            return writer.properties.IsStandardEncryptionUsed() || writer.properties.IsPublicKeyEncryptionUsed();
        }

        private string AddModifiedPostfix(string producer)
        {
	        if (producer == null || !versionInfo.GetVersion().Contains(versionInfo.GetProduct())) {
                return versionInfo.GetVersion();
            }

	        var idx = producer.IndexOf("; modified using", StringComparison.Ordinal);
	        StringBuilder buf;
	        if (idx == -1) {
		        buf = new StringBuilder(producer);
	        }
	        else {
		        buf = new StringBuilder(producer.JSubstring(0, idx));
	        }
	        buf.Append("; modified using ");
	        buf.Append(versionInfo.GetVersion());
	        return buf.ToString();
        }

        private void UpdatePdfVersionFromCatalog() {
            if (catalog.GetPdfObject().ContainsKey(PdfName.Version)) {
                // The version of the PDF specification to which the document conforms (for example, 1.4)
                // if later than the version specified in the file's header
                try {
                    var catalogVersion = PdfVersion.FromPdfName(catalog.GetPdfObject().GetAsName(PdfName.Version));
                    if (catalogVersion.CompareTo(pdfVersion) > 0) {
                        pdfVersion = catalogVersion;
                    }
                }
                catch (ArgumentException) {
                    ProcessReadingError(LogMessageConstant.DOCUMENT_VERSION_IN_CATALOG_CORRUPTED);
                }
            }
        }

        private void ReadDocumentIds() {
            var id = reader.trailer.GetAsArray(PdfName.ID);
            if (id != null) {
                if (id.Size() == 2) {
                    originalDocumentId = id.GetAsString(0);
                    modifiedDocumentId = id.GetAsString(1);
                }
                if (originalDocumentId == null || modifiedDocumentId == null) {
                    ProcessReadingError(LogMessageConstant.DOCUMENT_IDS_ARE_CORRUPTED);
                }
            }
        }

        private void ProcessReadingError(string errorMessage) {
            if (PdfReader.StrictnessLevel.CONSERVATIVE.IsStricter(reader.GetStrictnessLevel())) {
                var logger = LogManager.GetLogger(typeof(PdfDocument));
                logger.Error(errorMessage);
            }
            else {
                throw new PdfException(errorMessage);
            }
        }

        private static void OverrideFullCompressionInWriterProperties(WriterProperties properties, bool readerHasXrefStream
            ) {
            if (true == properties.isFullCompression && !readerHasXrefStream) {
                var logger = LogManager.GetLogger(typeof(PdfDocument));
                logger.Warn(KernelLogMessageConstant.FULL_COMPRESSION_APPEND_MODE_XREF_TABLE_INCONSISTENCY);
            }
            else {
                if (false == properties.isFullCompression && readerHasXrefStream) {
                    var logger = LogManager.GetLogger(typeof(PdfDocument));
                    logger.Warn(KernelLogMessageConstant.FULL_COMPRESSION_APPEND_MODE_XREF_STREAM_INCONSISTENCY);
                }
            }
            properties.isFullCompression = readerHasXrefStream;
        }

        private static bool IsXmpMetaHasProperty(XMPMeta xmpMeta, string schemaNS, string propName) {
            return xmpMeta.GetProperty(schemaNS, propName) != null;
        }

        void IDisposable.Dispose() {
            Close();
        }
    }
}
