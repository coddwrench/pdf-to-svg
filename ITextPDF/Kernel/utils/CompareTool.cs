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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using IText.IO;
using IText.IO.Font;
using IText.IO.Util;
using IText.Kernel.Counter.Event;
using IText.Kernel.Geom;
using IText.Kernel.Pdf;
using IText.Kernel.Pdf.Annot;
using IText.Kernel.Pdf.Canvas;
using IText.Kernel.XMP;
using IText.Kernel.XMP.Options;
using IText.Logger;
using IOException = System.IO.IOException;

namespace IText.Kernel.Utils {
    /// <summary>
    /// This class provides means to compare two PDF files both by content and visually
    /// and gives the report on their differences.
    /// </summary>
    /// <remarks>
    /// This class provides means to compare two PDF files both by content and visually
    /// and gives the report on their differences.
    /// <para />
    /// For visual comparison it uses external tools: Ghostscript and ImageMagick, which
    /// should be installed on your machine. To allow CompareTool to use them, you need
    /// to pass either java properties or environment variables with names "ITEXT_GS_EXEC" and
    /// "ITEXT_MAGICK_COMPARE_EXEC", which would contain the commands to execute the
    /// Ghostscript and ImageMagick tools.
    /// <para />
    /// CompareTool class was mainly designed for the testing purposes of iText in order to
    /// ensure that the same code produces the same PDF document. For this reason you will
    /// often encounter such parameter names as "outDoc" and "cmpDoc" which stand for output
    /// document and document-for-comparison. The first one is viewed as the current result,
    /// and the second one is referred as normal or ideal result. OutDoc is compared to the
    /// ideal cmpDoc. Therefore all reports of the comparison are in the form: "Expected ...,
    /// but was ...". This should be interpreted in the following way: "expected" part stands
    /// for the content of the cmpDoc and "but was" part stands for the content of the outDoc.
    /// </remarks>
    public class CompareTool {
        private const string FILE_PROTOCOL = "file://";

        private const string UNEXPECTED_NUMBER_OF_PAGES = "Unexpected number of pages for <filename>.";

        private const string DIFFERENT_PAGES = "File " + FILE_PROTOCOL + "<filename> differs on page <pagenumber>.";

        private const string IGNORED_AREAS_PREFIX = "ignored_areas_";

        private const string VERSION_REGEXP = "(iText\u00ae( pdfX(FA|fa)| DITO)?|iTextSharp\u2122) (\\d+\\.)+\\d+(-SNAPSHOT)?";

        private const string VERSION_REPLACEMENT = "iText\u00ae <version>";

        private const string COPYRIGHT_REGEXP = "\u00a9\\d+-\\d+ iText Group NV";

        private const string COPYRIGHT_REPLACEMENT = "\u00a9<copyright years> iText Group NV";

        private const string NEW_LINES = "\\r|\\n";

        private string cmpPdf;

        private string cmpPdfName;

        private string cmpImage;

        private string outPdf;

        private string outPdfName;

        private string outImage;

        private ReaderProperties outProps;

        private ReaderProperties cmpProps;

        private IList<PdfIndirectReference> outPagesRef;

        private IList<PdfIndirectReference> cmpPagesRef;

        private int compareByContentErrorsLimit = 1000;

        private bool generateCompareByContentXmlReport;

        private bool encryptionCompareEnabled;

        private bool useCachedPagesForComparison = true;

        private IMetaInfo metaInfo;

        private string gsExec;

        private string compareExec;

        public CompareTool() {
        }

        internal CompareTool(string gsExec, string compareExec) {
            this.gsExec = gsExec;
            this.compareExec = compareExec;
        }

        /// <summary>
        /// Compares two PDF documents by content starting from Catalog dictionary and then recursively comparing
        /// corresponding objects which are referenced from it.
        /// </summary>
        /// <remarks>
        /// Compares two PDF documents by content starting from Catalog dictionary and then recursively comparing
        /// corresponding objects which are referenced from it. You can roughly imagine it as depth-first traversal
        /// of the two trees that represent pdf objects structure of the documents.
        /// <para />
        /// The main difference between this method and the
        /// <see cref="CompareByContent(System.String, System.String, System.String, System.String)"/>
        /// methods is the return value. This method returns a
        /// <see cref="CompareResult"/>
        /// class instance, which could be used
        /// in code, whilst compareByContent methods in case of the differences simply return String value, which could
        /// only be printed. Also, keep in mind that this method doesn't perform visual comparison of the documents.
        /// <para />
        /// For more explanations about what outDoc and cmpDoc are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <param name="outDocument">
        /// a
        /// <see cref="PdfDocument"/>
        /// corresponding to the output file, which is to be compared with cmp-file.
        /// </param>
        /// <param name="cmpDocument">
        /// a
        /// <see cref="PdfDocument"/>
        /// corresponding to the cmp-file, which is to be compared with output file.
        /// </param>
        /// <returns>
        /// the report on comparison of two files in the form of the custom class
        /// <see cref="CompareResult"/>
        /// instance.
        /// </returns>
        /// <seealso cref="CompareResult"/>
        public virtual CompareResult CompareByCatalog(PdfDocument outDocument, PdfDocument cmpDocument
            ) {
            CompareResult compareResult = null;
            compareResult = new CompareResult(this, compareByContentErrorsLimit);
            var catalogPath = new ObjectPath(cmpDocument.GetCatalog().GetPdfObject().GetIndirectReference
                (), outDocument.GetCatalog().GetPdfObject().GetIndirectReference());
            ICollection<PdfName> ignoredCatalogEntries = new LinkedHashSet<PdfName>(JavaUtil.ArraysAsList(PdfName.Metadata
                ));
            CompareDictionariesExtended(outDocument.GetCatalog().GetPdfObject(), cmpDocument.GetCatalog().GetPdfObject
                (), catalogPath, compareResult, ignoredCatalogEntries);
            // Method compareDictionariesExtended eventually calls compareObjects method which doesn't compare page objects.
            // At least for now compare page dictionaries explicitly here like this.
            if (cmpPagesRef == null || outPagesRef == null) {
                return compareResult;
            }
            if (outPagesRef.Count != cmpPagesRef.Count && !compareResult.IsMessageLimitReached()) {
                compareResult.AddError(catalogPath, "Documents have different numbers of pages.");
            }
            for (var i = 0; i < Math.Min(cmpPagesRef.Count, outPagesRef.Count); i++) {
                if (compareResult.IsMessageLimitReached()) {
                    break;
                }
                var currentPath = new ObjectPath(cmpPagesRef[i], outPagesRef[i]);
                var outPageDict = (PdfDictionary)outPagesRef[i].GetRefersTo();
                var cmpPageDict = (PdfDictionary)cmpPagesRef[i].GetRefersTo();
                CompareDictionariesExtended(outPageDict, cmpPageDict, currentPath, compareResult);
            }
            return compareResult;
        }

        /// <summary>Disables the default logic of pages comparison.</summary>
        /// <remarks>
        /// Disables the default logic of pages comparison.
        /// This option makes sense only for
        /// <see cref="CompareByCatalog(PdfDocument, PdfDocument)"/>
        /// method.
        /// <para />
        /// By default, pages are treated as special objects and if they are met in the process of comparison, then they are
        /// not checked as objects, but rather simply checked that they have same page numbers in both documents.
        /// This behaviour is intended for the
        /// <see cref="CompareByContent(System.String, System.String, System.String)"/>
        /// set of methods, because in them documents are compared in page by page basis. Thus, we don't need to check if pages
        /// are of the same content when they are met in comparison process, we are sure that we will compare their content or
        /// we have already compared them.
        /// <para />
        /// However, if you would use
        /// <see cref="CompareByCatalog(PdfDocument, PdfDocument)"/>
        /// with default behaviour
        /// of pages comparison, pages won't be checked at all, every time when reference to the page dictionary is met,
        /// only page numbers will be compared for both documents. You can say that in this case, comparison will be performed
        /// for all document's catalog entries except /Pages (However in fact, document's page tree structures will be compared,
        /// but pages themselves - won't).
        /// </remarks>
        /// <returns>
        /// this
        /// <see cref="CompareTool"/>
        /// instance.
        /// </returns>
        public virtual CompareTool DisableCachedPagesComparison() {
            useCachedPagesForComparison = false;
            return this;
        }

        /// <summary>Sets the maximum errors count which will be returned as the result of the comparison.</summary>
        /// <param name="compareByContentMaxErrorCount">the errors count.</param>
        /// <returns>this CompareTool instance.</returns>
        public virtual CompareTool SetCompareByContentErrorsLimit(int compareByContentMaxErrorCount
            ) {
            compareByContentErrorsLimit = compareByContentMaxErrorCount;
            return this;
        }

        /// <summary>Enables or disables the generation of the comparison report in the form of an xml document.</summary>
        /// <remarks>
        /// Enables or disables the generation of the comparison report in the form of an xml document.
        /// <para />
        /// IMPORTANT NOTE: this flag affects only the comparison performed by compareByContent methods!
        /// </remarks>
        /// <param name="generateCompareByContentXmlReport">true to enable xml report generation, false - to disable.</param>
        /// <returns>this CompareTool instance.</returns>
        public virtual CompareTool SetGenerateCompareByContentXmlReport(bool generateCompareByContentXmlReport
            ) {
            this.generateCompareByContentXmlReport = generateCompareByContentXmlReport;
            return this;
        }

        /// <summary>
        /// Sets
        /// <see cref="IMetaInfo"/>
        /// info that will be used for both read and written documents creation.
        /// </summary>
        /// <param name="metaInfo">meta info to set</param>
        public virtual void SetEventCountingMetaInfo(IMetaInfo metaInfo) {
            this.metaInfo = metaInfo;
        }

        /// <summary>Enables the comparison of the encryption properties of the documents.</summary>
        /// <remarks>
        /// Enables the comparison of the encryption properties of the documents. Encryption properties comparison
        /// results are returned along with all other comparison results.
        /// <para />
        /// IMPORTANT NOTE: this flag affects only the comparison performed by compareByContent methods!
        /// <see cref="CompareByCatalog(PdfDocument, PdfDocument)"/>
        /// doesn't compare encryption properties
        /// because encryption properties aren't part of the document's Catalog.
        /// </remarks>
        /// <returns>this CompareTool instance.</returns>
        public virtual CompareTool EnableEncryptionCompare() {
            encryptionCompareEnabled = true;
            return this;
        }

        /// <summary>
        /// Gets
        /// <see cref="ReaderProperties"/>
        /// to be passed later to the
        /// <see cref="PdfReader"/>
        /// of the output document.
        /// </summary>
        /// <remarks>
        /// Gets
        /// <see cref="ReaderProperties"/>
        /// to be passed later to the
        /// <see cref="PdfReader"/>
        /// of the output document.
        /// <para />
        /// Documents for comparison are opened in reader mode. This method is intended to alter
        /// <see cref="ReaderProperties"/>
        /// which are used to open the output document. This is particularly useful for comparison of encrypted documents.
        /// <para />
        /// For more explanations about what outDoc and cmpDoc are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <returns>
        /// 
        /// <see cref="ReaderProperties"/>
        /// instance to be passed later to the
        /// <see cref="PdfReader"/>
        /// of the output document.
        /// </returns>
        public virtual ReaderProperties GetOutReaderProperties() {
            if (outProps == null) {
                outProps = new ReaderProperties();
            }
            return outProps;
        }

        /// <summary>
        /// Gets
        /// <see cref="ReaderProperties"/>
        /// to be passed later to the
        /// <see cref="PdfReader"/>
        /// of the cmp document.
        /// </summary>
        /// <remarks>
        /// Gets
        /// <see cref="ReaderProperties"/>
        /// to be passed later to the
        /// <see cref="PdfReader"/>
        /// of the cmp document.
        /// <para />
        /// Documents for comparison are opened in reader mode. This method is intended to alter
        /// <see cref="ReaderProperties"/>
        /// which are used to open the cmp document. This is particularly useful for comparison of encrypted documents.
        /// <para />
        /// For more explanations about what outDoc and cmpDoc are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <returns>
        /// 
        /// <see cref="ReaderProperties"/>
        /// instance to be passed later to the
        /// <see cref="PdfReader"/>
        /// of the cmp document.
        /// </returns>
        public virtual ReaderProperties GetCmpReaderProperties() {
            if (cmpProps == null) {
                cmpProps = new ReaderProperties();
            }
            return cmpProps;
        }

        /// <summary>Compares two documents visually.</summary>
        /// <remarks>
        /// Compares two documents visually. For the comparison two external tools are used: Ghostscript and ImageMagick.
        /// For more info about needed configuration for visual comparison process see
        /// <see cref="CompareTool"/>
        /// class description.
        /// <para />
        /// During comparison for every page of the two documents an image file will be created in the folder specified by
        /// outPath parameter. Then those page images will be compared and if there are any differences for some pages,
        /// another image file will be created with marked differences on it.
        /// </remarks>
        /// <param name="outPdf">the absolute path to the output file, which is to be compared to cmp-file.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which is to be compared to output file.</param>
        /// <param name="outPath">the absolute path to the folder, which will be used to store image files for visual comparison.
        ///     </param>
        /// <param name="differenceImagePrefix">file name prefix for image files with marked differences if there is any.
        ///     </param>
        /// <returns>string containing list of the pages that are visually different, or null if there are no visual differences.
        ///     </returns>
        public virtual string CompareVisually(string outPdf, string cmpPdf, string outPath, string differenceImagePrefix
            ) {
            return CompareVisually(outPdf, cmpPdf, outPath, differenceImagePrefix, null);
        }

        /// <summary>Compares two documents visually.</summary>
        /// <remarks>
        /// Compares two documents visually. For the comparison two external tools are used: Ghostscript and ImageMagick.
        /// For more info about needed configuration for visual comparison process see
        /// <see cref="CompareTool"/>
        /// class description.
        /// <para />
        /// During comparison for every page of two documents an image file will be created in the folder specified by
        /// outPath parameter. Then those page images will be compared and if there are any differences for some pages,
        /// another image file will be created with marked differences on it.
        /// <para />
        /// It is possible to ignore certain areas of the document pages during visual comparison. This is useful for example
        /// in case if documents should be the same except certain page area with date on it. In this case, in the folder
        /// specified by the outPath, new pdf documents will be created with the black rectangles at the specified ignored
        /// areas, and visual comparison will be performed on these new documents.
        /// </remarks>
        /// <param name="outPdf">the absolute path to the output file, which is to be compared to cmp-file.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which is to be compared to output file.</param>
        /// <param name="outPath">the absolute path to the folder, which will be used to store image files for visual comparison.
        ///     </param>
        /// <param name="differenceImagePrefix">file name prefix for image files with marked differences if there is any.
        ///     </param>
        /// <param name="ignoredAreas">a map with one-based page numbers as keys and lists of ignored rectangles as values.
        ///     </param>
        /// <returns>string containing list of the pages that are visually different, or null if there are no visual differences.
        ///     </returns>
        public virtual string CompareVisually(string outPdf, string cmpPdf, string outPath, string differenceImagePrefix
            , IDictionary<int, IList<Rectangle>> ignoredAreas) {
            Init(outPdf, cmpPdf);
            Console.Out.WriteLine("Out pdf: " + UrlUtil.GetNormalizedFileUriString(outPdf));
            Console.Out.WriteLine("Cmp pdf: " + UrlUtil.GetNormalizedFileUriString(cmpPdf) + "\n");
            return CompareVisually(outPath, differenceImagePrefix, ignoredAreas);
        }

        /// <summary>
        /// Compares two PDF documents by content starting from page dictionaries and then recursively comparing
        /// corresponding objects which are referenced from them.
        /// </summary>
        /// <remarks>
        /// Compares two PDF documents by content starting from page dictionaries and then recursively comparing
        /// corresponding objects which are referenced from them. You can roughly imagine it as depth-first traversal
        /// of the two trees that represent pdf objects structure of the documents.
        /// <para />
        /// When comparison by content is finished, if any differences were found, visual comparison is automatically started.
        /// For this overload, differenceImagePrefix value is generated using diff_%outPdfFileName%_ format.
        /// <para />
        /// For more explanations about what outPdf and cmpPdf are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <param name="outPdf">the absolute path to the output file, which is to be compared to cmp-file.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which is to be compared to output file.</param>
        /// <param name="outPath">the absolute path to the folder, which will be used to store image files for visual comparison.
        ///     </param>
        /// <returns>
        /// string containing text report on the encountered content differences and also list of the pages that are
        /// visually different, or null if there are no content and therefore no visual differences.
        /// </returns>
        /// <seealso cref="CompareVisually(System.String, System.String, System.String, System.String)"/>
        public virtual string CompareByContent(string outPdf, string cmpPdf, string outPath) {
            return CompareByContent(outPdf, cmpPdf, outPath, null, null, null, null);
        }

        /// <summary>
        /// Compares two PDF documents by content starting from page dictionaries and then recursively comparing
        /// corresponding objects which are referenced from them.
        /// </summary>
        /// <remarks>
        /// Compares two PDF documents by content starting from page dictionaries and then recursively comparing
        /// corresponding objects which are referenced from them. You can roughly imagine it as depth-first traversal
        /// of the two trees that represent pdf objects structure of the documents.
        /// <para />
        /// When comparison by content is finished, if any differences were found, visual comparison is automatically started.
        /// <para />
        /// For more explanations about what outPdf and cmpPdf are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <param name="outPdf">the absolute path to the output file, which is to be compared to cmp-file.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which is to be compared to output file.</param>
        /// <param name="outPath">the absolute path to the folder, which will be used to store image files for visual comparison.
        ///     </param>
        /// <param name="differenceImagePrefix">
        /// file name prefix for image files with marked visual differences if there are any;
        /// if it's set to null the prefix defaults to diff_%outPdfFileName%_ format.
        /// </param>
        /// <returns>
        /// string containing text report on the encountered content differences and also list of the pages that are
        /// visually different, or null if there are no content and therefore no visual differences.
        /// </returns>
        /// <seealso cref="CompareVisually(System.String, System.String, System.String, System.String)"/>
        public virtual string CompareByContent(string outPdf, string cmpPdf, string outPath, string differenceImagePrefix
            ) {
            return CompareByContent(outPdf, cmpPdf, outPath, differenceImagePrefix, null, null, null);
        }

        /// <summary>This method overload is used to compare two encrypted PDF documents.</summary>
        /// <remarks>
        /// This method overload is used to compare two encrypted PDF documents. Document passwords are passed with
        /// outPass and cmpPass parameters.
        /// <para />
        /// Compares two PDF documents by content starting from page dictionaries and then recursively comparing
        /// corresponding objects which are referenced from them. You can roughly imagine it as depth-first traversal
        /// of the two trees that represent pdf objects structure of the documents.
        /// <para />
        /// When comparison by content is finished, if any differences were found, visual comparison is automatically started.
        /// For more info see
        /// <see cref="CompareVisually(System.String, System.String, System.String, System.String)"/>.
        /// <para />
        /// For more explanations about what outPdf and cmpPdf are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <param name="outPdf">the absolute path to the output file, which is to be compared to cmp-file.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which is to be compared to output file.</param>
        /// <param name="outPath">the absolute path to the folder, which will be used to store image files for visual comparison.
        ///     </param>
        /// <param name="differenceImagePrefix">
        /// file name prefix for image files with marked visual differences if there is any;
        /// if it's set to null the prefix defaults to diff_%outPdfFileName%_ format.
        /// </param>
        /// <param name="outPass">password for the encrypted document specified by the outPdf absolute path.</param>
        /// <param name="cmpPass">password for the encrypted document specified by the cmpPdf absolute path.</param>
        /// <returns>
        /// string containing text report on the encountered content differences and also list of the pages that are
        /// visually different, or null if there are no content and therefore no visual differences.
        /// </returns>
        /// <seealso cref="CompareVisually(System.String, System.String, System.String, System.String)"/>
        public virtual string CompareByContent(string outPdf, string cmpPdf, string outPath, string differenceImagePrefix
            , byte[] outPass, byte[] cmpPass) {
            return CompareByContent(outPdf, cmpPdf, outPath, differenceImagePrefix, null, outPass, cmpPass);
        }

        /// <summary>
        /// Compares two PDF documents by content starting from page dictionaries and then recursively comparing
        /// corresponding objects which are referenced from them.
        /// </summary>
        /// <remarks>
        /// Compares two PDF documents by content starting from page dictionaries and then recursively comparing
        /// corresponding objects which are referenced from them. You can roughly imagine it as depth-first traversal
        /// of the two trees that represent pdf objects structure of the documents.
        /// <para />
        /// When comparison by content is finished, if any differences were found, visual comparison is automatically started.
        /// <para />
        /// For more explanations about what outPdf and cmpPdf are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <param name="outPdf">the absolute path to the output file, which is to be compared to cmp-file.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which is to be compared to output file.</param>
        /// <param name="outPath">the absolute path to the folder, which will be used to store image files for visual comparison.
        ///     </param>
        /// <param name="differenceImagePrefix">
        /// file name prefix for image files with marked visual differences if there are any;
        /// if it's set to null the prefix defaults to diff_%outPdfFileName%_ format.
        /// </param>
        /// <param name="ignoredAreas">a map with one-based page numbers as keys and lists of ignored rectangles as values.
        ///     </param>
        /// <returns>
        /// string containing text report on the encountered content differences and also list of the pages that are
        /// visually different, or null if there are no content and therefore no visual differences.
        /// </returns>
        /// <seealso cref="CompareVisually(System.String, System.String, System.String, System.String)"/>
        public virtual string CompareByContent(string outPdf, string cmpPdf, string outPath, string differenceImagePrefix
            , IDictionary<int, IList<Rectangle>> ignoredAreas) {
            return CompareByContent(outPdf, cmpPdf, outPath, differenceImagePrefix, ignoredAreas, null, null);
        }

        /// <summary>This method overload is used to compare two encrypted PDF documents.</summary>
        /// <remarks>
        /// This method overload is used to compare two encrypted PDF documents. Document passwords are passed with
        /// outPass and cmpPass parameters.
        /// <para />
        /// Compares two PDF documents by content starting from page dictionaries and then recursively comparing
        /// corresponding objects which are referenced from them. You can roughly imagine it as depth-first traversal
        /// of the two trees that represent pdf objects structure of the documents.
        /// <para />
        /// When comparison by content is finished, if any differences were found, visual comparison is automatically started.
        /// <para />
        /// For more explanations about what outPdf and cmpPdf are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <param name="outPdf">the absolute path to the output file, which is to be compared to cmp-file.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which is to be compared to output file.</param>
        /// <param name="outPath">the absolute path to the folder, which will be used to store image files for visual comparison.
        ///     </param>
        /// <param name="differenceImagePrefix">
        /// file name prefix for image files with marked visual differences if there are any;
        /// if it's set to null the prefix defaults to diff_%outPdfFileName%_ format.
        /// </param>
        /// <param name="ignoredAreas">a map with one-based page numbers as keys and lists of ignored rectangles as values.
        ///     </param>
        /// <param name="outPass">password for the encrypted document specified by the outPdf absolute path.</param>
        /// <param name="cmpPass">password for the encrypted document specified by the cmpPdf absolute path.</param>
        /// <returns>
        /// string containing text report on the encountered content differences and also list of the pages that are
        /// visually different, or null if there are no content and therefore no visual differences.
        /// </returns>
        /// <seealso cref="CompareVisually(System.String, System.String, System.String, System.String)"/>
        public virtual string CompareByContent(string outPdf, string cmpPdf, string outPath, string differenceImagePrefix
            , IDictionary<int, IList<Rectangle>> ignoredAreas, byte[] outPass, byte[] cmpPass) {
            Init(outPdf, cmpPdf);
            Console.Out.WriteLine("Out pdf: " + UrlUtil.GetNormalizedFileUriString(outPdf));
            Console.Out.WriteLine("Cmp pdf: " + UrlUtil.GetNormalizedFileUriString(cmpPdf) + "\n");
            SetPassword(outPass, cmpPass);
            return CompareByContent(outPath, differenceImagePrefix, ignoredAreas);
        }

        /// <summary>Simple method that compares two given PdfDictionaries by content.</summary>
        /// <remarks>
        /// Simple method that compares two given PdfDictionaries by content. This is "deep" comparing, which means that all
        /// nested objects are also compared by content.
        /// </remarks>
        /// <param name="outDict">dictionary to compare.</param>
        /// <param name="cmpDict">dictionary to compare.</param>
        /// <returns>true if dictionaries are equal by content, otherwise false.</returns>
        public virtual bool CompareDictionaries(PdfDictionary outDict, PdfDictionary cmpDict) {
            return CompareDictionariesExtended(outDict, cmpDict, null, null);
        }

        /// <summary>Recursively compares structures of two corresponding dictionaries from out and cmp PDF documents.
        ///     </summary>
        /// <remarks>
        /// Recursively compares structures of two corresponding dictionaries from out and cmp PDF documents. You can roughly
        /// imagine it as depth-first traversal of the two trees that represent pdf objects structure of the documents.
        /// <para />
        /// Both out and cmp
        /// <see cref="PdfDictionary"/>
        /// shall have indirect references.
        /// <para />
        /// By default page dictionaries are excluded from the comparison when met and are instead compared in a special manner,
        /// simply comparing their page numbers. This behavior can be disabled by calling
        /// <see cref="DisableCachedPagesComparison()"/>.
        /// <para />
        /// For more explanations about what outPdf and cmpPdf are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <param name="outDict">
        /// an indirect
        /// <see cref="PdfDictionary"/>
        /// from the output file, which is to be compared to cmp-file dictionary.
        /// </param>
        /// <param name="cmpDict">
        /// an indirect
        /// <see cref="PdfDictionary"/>
        /// from the cmp-file file, which is to be compared to output file dictionary.
        /// </param>
        /// <returns>
        /// 
        /// <see cref="CompareResult"/>
        /// instance containing differences between the two dictionaries,
        /// or
        /// <see langword="null"/>
        /// if dictionaries are equal.
        /// </returns>
        public virtual CompareResult CompareDictionariesStructure(PdfDictionary outDict, PdfDictionary
             cmpDict) {
            return CompareDictionariesStructure(outDict, cmpDict, null);
        }

        /// <summary>Recursively compares structures of two corresponding dictionaries from out and cmp PDF documents.
        ///     </summary>
        /// <remarks>
        /// Recursively compares structures of two corresponding dictionaries from out and cmp PDF documents. You can roughly
        /// imagine it as depth-first traversal of the two trees that represent pdf objects structure of the documents.
        /// <para />
        /// Both out and cmp
        /// <see cref="PdfDictionary"/>
        /// shall have indirect references.
        /// <para />
        /// By default page dictionaries are excluded from the comparison when met and are instead compared in a special manner,
        /// simply comparing their page numbers. This behavior can be disabled by calling
        /// <see cref="DisableCachedPagesComparison()"/>.
        /// <para />
        /// For more explanations about what outPdf and cmpPdf are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <param name="outDict">
        /// an indirect
        /// <see cref="PdfDictionary"/>
        /// from the output file, which is to be compared to cmp-file dictionary.
        /// </param>
        /// <param name="cmpDict">
        /// an indirect
        /// <see cref="PdfDictionary"/>
        /// from the cmp-file file, which is to be compared to output file dictionary.
        /// </param>
        /// <param name="excludedKeys">
        /// a
        /// <see cref="Java.Util.Set{E}"/>
        /// of names that designate entries from
        /// <paramref name="outDict"/>
        /// and
        /// <paramref name="cmpDict"/>
        /// dictionaries
        /// which are to be skipped during comparison.
        /// </param>
        /// <returns>
        /// 
        /// <see cref="CompareResult"/>
        /// instance containing differences between the two dictionaries,
        /// or
        /// <see langword="null"/>
        /// if dictionaries are equal.
        /// </returns>
        public virtual CompareResult CompareDictionariesStructure(PdfDictionary outDict, PdfDictionary
             cmpDict, ICollection<PdfName> excludedKeys) {
            if (outDict.GetIndirectReference() == null || cmpDict.GetIndirectReference() == null) {
                throw new ArgumentException("The 'outDict' and 'cmpDict' objects shall have indirect references.");
            }
            var compareResult = new CompareResult(this, compareByContentErrorsLimit);
            var currentPath = new ObjectPath(cmpDict.GetIndirectReference(), outDict.GetIndirectReference
                ());
            if (!CompareDictionariesExtended(outDict, cmpDict, currentPath, compareResult, excludedKeys)) {
                Debug.Assert(!compareResult.IsOk());
                Console.Out.WriteLine(compareResult.GetReport());
                return compareResult;
            }
            Debug.Assert(compareResult.IsOk());
            return null;
        }

        /// <summary>Compares structures of two corresponding streams from out and cmp PDF documents.</summary>
        /// <remarks>
        /// Compares structures of two corresponding streams from out and cmp PDF documents. You can roughly
        /// imagine it as depth-first traversal of the two trees that represent pdf objects structure of the documents.
        /// <para />
        /// For more explanations about what outPdf and cmpPdf are see last paragraph of the
        /// <see cref="CompareTool"/>
        /// class description.
        /// </remarks>
        /// <param name="outStream">
        /// a
        /// <see cref="PdfStream"/>
        /// from the output file, which is to be compared to cmp-file stream.
        /// </param>
        /// <param name="cmpStream">
        /// a
        /// <see cref="PdfStream"/>
        /// from the cmp-file file, which is to be compared to output file stream.
        /// </param>
        /// <returns>
        /// 
        /// <see cref="CompareResult"/>
        /// instance containing differences between the two streams,
        /// or
        /// <see langword="null"/>
        /// if streams are equal.
        /// </returns>
        public virtual CompareResult CompareStreamsStructure(PdfStream outStream, PdfStream cmpStream) {
            var compareResult = new CompareResult(this, compareByContentErrorsLimit);
            var currentPath = new ObjectPath(cmpStream.GetIndirectReference(), outStream
                .GetIndirectReference());
            if (!CompareStreamsExtended(outStream, cmpStream, currentPath, compareResult)) {
                Debug.Assert(!compareResult.IsOk());
                Console.Out.WriteLine(compareResult.GetReport());
                return compareResult;
            }
            Debug.Assert(compareResult.IsOk());
            return null;
        }

        /// <summary>Simple method that compares two given PdfStreams by content.</summary>
        /// <remarks>
        /// Simple method that compares two given PdfStreams by content. This is "deep" comparing, which means that all
        /// nested objects are also compared by content.
        /// </remarks>
        /// <param name="outStream">stream to compare.</param>
        /// <param name="cmpStream">stream to compare.</param>
        /// <returns>true if stream are equal by content, otherwise false.</returns>
        public virtual bool CompareStreams(PdfStream outStream, PdfStream cmpStream) {
            return CompareStreamsExtended(outStream, cmpStream, null, null);
        }

        /// <summary>Simple method that compares two given PdfArrays by content.</summary>
        /// <remarks>
        /// Simple method that compares two given PdfArrays by content. This is "deep" comparing, which means that all
        /// nested objects are also compared by content.
        /// </remarks>
        /// <param name="outArray">array to compare.</param>
        /// <param name="cmpArray">array to compare.</param>
        /// <returns>true if arrays are equal by content, otherwise false.</returns>
        public virtual bool CompareArrays(PdfArray outArray, PdfArray cmpArray) {
            return CompareArraysExtended(outArray, cmpArray, null, null);
        }

        /// <summary>Simple method that compares two given PdfNames.</summary>
        /// <param name="outName">name to compare.</param>
        /// <param name="cmpName">name to compare.</param>
        /// <returns>true if names are equal, otherwise false.</returns>
        public virtual bool CompareNames(PdfName outName, PdfName cmpName) {
            return cmpName.Equals(outName);
        }

        /// <summary>Simple method that compares two given PdfNumbers.</summary>
        /// <param name="outNumber">number to compare.</param>
        /// <param name="cmpNumber">number to compare.</param>
        /// <returns>true if numbers are equal, otherwise false.</returns>
        public virtual bool CompareNumbers(PdfNumber outNumber, PdfNumber cmpNumber) {
            return cmpNumber.GetValue() == outNumber.GetValue();
        }

        /// <summary>Simple method that compares two given PdfStrings.</summary>
        /// <param name="outString">string to compare.</param>
        /// <param name="cmpString">string to compare.</param>
        /// <returns>true if strings are equal, otherwise false.</returns>
        public virtual bool CompareStrings(PdfString outString, PdfString cmpString) {
            return cmpString.GetValue().Equals(outString.GetValue());
        }

        /// <summary>Simple method that compares two given PdfBooleans.</summary>
        /// <param name="outBoolean">boolean to compare.</param>
        /// <param name="cmpBoolean">boolean to compare.</param>
        /// <returns>true if booleans are equal, otherwise false.</returns>
        public virtual bool CompareBooleans(PdfBoolean outBoolean, PdfBoolean cmpBoolean) {
            return cmpBoolean.GetValue() == outBoolean.GetValue();
        }

        /// <summary>Compares xmp metadata of the two given PDF documents.</summary>
        /// <param name="outPdf">the absolute path to the output file, which xmp is to be compared to cmp-file.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which xmp is to be compared to output file.</param>
        /// <returns>text report on the xmp differences, or null if there are no differences.</returns>
        public virtual string CompareXmp(string outPdf, string cmpPdf) {
            return CompareXmp(outPdf, cmpPdf, false);
        }

        /// <summary>Compares xmp metadata of the two given PDF documents.</summary>
        /// <param name="outPdf">the absolute path to the output file, which xmp is to be compared to cmp-file.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which xmp is to be compared to output file.</param>
        /// <param name="ignoreDateAndProducerProperties">
        /// true, if to ignore differences in date or producer xmp metadata
        /// properties.
        /// </param>
        /// <returns>text report on the xmp differences, or null if there are no differences.</returns>
        public virtual string CompareXmp(string outPdf, string cmpPdf, bool ignoreDateAndProducerProperties) {
            Init(outPdf, cmpPdf);
            PdfDocument cmpDocument = null;
            PdfDocument outDocument = null;
            try {
                cmpDocument = new PdfDocument(new PdfReader(this.cmpPdf), new DocumentProperties().SetEventCountingMetaInfo
                    (metaInfo));
                outDocument = new PdfDocument(new PdfReader(this.outPdf), new DocumentProperties().SetEventCountingMetaInfo
                    (metaInfo));
                var cmpBytes = cmpDocument.GetXmpMetadata();
                var outBytes = outDocument.GetXmpMetadata();
                if (ignoreDateAndProducerProperties) {
                    var xmpMeta = XMPMetaFactory.ParseFromBuffer(cmpBytes, new ParseOptions().SetOmitNormalization(true));
                    XMPUtils.RemoveProperties(xmpMeta, XMPConst.NS_XMP, PdfConst.CreateDate, true, true);
                    XMPUtils.RemoveProperties(xmpMeta, XMPConst.NS_XMP, PdfConst.ModifyDate, true, true);
                    XMPUtils.RemoveProperties(xmpMeta, XMPConst.NS_XMP, PdfConst.MetadataDate, true, true);
                    XMPUtils.RemoveProperties(xmpMeta, XMPConst.NS_PDF, PdfConst.Producer, true, true);
                    cmpBytes = XMPMetaFactory.SerializeToBuffer(xmpMeta, new SerializeOptions(SerializeOptions.SORT));
                    xmpMeta = XMPMetaFactory.ParseFromBuffer(outBytes, new ParseOptions().SetOmitNormalization(true));
                    XMPUtils.RemoveProperties(xmpMeta, XMPConst.NS_XMP, PdfConst.CreateDate, true, true);
                    XMPUtils.RemoveProperties(xmpMeta, XMPConst.NS_XMP, PdfConst.ModifyDate, true, true);
                    XMPUtils.RemoveProperties(xmpMeta, XMPConst.NS_XMP, PdfConst.MetadataDate, true, true);
                    XMPUtils.RemoveProperties(xmpMeta, XMPConst.NS_PDF, PdfConst.Producer, true, true);
                    outBytes = XMPMetaFactory.SerializeToBuffer(xmpMeta, new SerializeOptions(SerializeOptions.SORT));
                }
                if (!CompareXmls(cmpBytes, outBytes)) {
                    return "The XMP packages different!";
                }
            }
            catch (Exception) {
                return "XMP parsing failure!";
            }
            finally {
                if (cmpDocument != null) {
                    cmpDocument.Close();
                }
                if (outDocument != null) {
                    outDocument.Close();
                }
            }
            return null;
        }

        /// <summary>Utility method that provides simple comparison of the two xml files stored in byte arrays.</summary>
        /// <param name="xml1">first xml file data to compare.</param>
        /// <param name="xml2">second xml file data to compare.</param>
        /// <returns>true if xml structures are identical, false otherwise.</returns>
        public virtual bool CompareXmls(byte[] xml1, byte[] xml2) {
            return XmlUtils.CompareXmls(new MemoryStream(xml1), new MemoryStream(xml2));
        }

        /// <summary>Utility method that provides simple comparison of the two xml files.</summary>
        /// <param name="outXmlFile">absolute path to the out xml file to compare.</param>
        /// <param name="cmpXmlFile">absolute path to the cmp xml file to compare.</param>
        /// <returns>true if xml structures are identical, false otherwise.</returns>
        public virtual bool CompareXmls(string outXmlFile, string cmpXmlFile) {
            Console.Out.WriteLine("Out xml: " + UrlUtil.GetNormalizedFileUriString(outXmlFile));
            Console.Out.WriteLine("Cmp xml: " + UrlUtil.GetNormalizedFileUriString(cmpXmlFile) + "\n");
            using (Stream outXmlStream = FileUtil.GetInputStreamForFile(outXmlFile)) {
                using (Stream cmpXmlStream = FileUtil.GetInputStreamForFile(cmpXmlFile)) {
                    return XmlUtils.CompareXmls(outXmlStream, cmpXmlStream);
                }
            }
        }

        /// <summary>Compares document info dictionaries of two pdf documents.</summary>
        /// <remarks>
        /// Compares document info dictionaries of two pdf documents.
        /// <para />
        /// This method overload is used to compare two encrypted PDF documents. Document passwords are passed with
        /// outPass and cmpPass parameters.
        /// </remarks>
        /// <param name="outPdf">the absolute path to the output file, which info is to be compared to cmp-file info.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which info is to be compared to output file info.</param>
        /// <param name="outPass">password for the encrypted document specified by the outPdf absolute path.</param>
        /// <param name="cmpPass">password for the encrypted document specified by the cmpPdf absolute path.</param>
        /// <returns>text report on the differences in documents infos.</returns>
        public virtual string CompareDocumentInfo(string outPdf, string cmpPdf, byte[] outPass, byte[] cmpPass) {
            Console.Out.Write("[itext] INFO  Comparing document info.......");
            string message = null;
            SetPassword(outPass, cmpPass);
            var outDocument = new PdfDocument(new PdfReader(outPdf, GetOutReaderProperties()), new DocumentProperties
                ().SetEventCountingMetaInfo(metaInfo));
            var cmpDocument = new PdfDocument(new PdfReader(cmpPdf, GetCmpReaderProperties()), new DocumentProperties
                ().SetEventCountingMetaInfo(metaInfo));
            var cmpInfo = ConvertInfo(cmpDocument.GetDocumentInfo());
            var outInfo = ConvertInfo(outDocument.GetDocumentInfo());
            for (var i = 0; i < cmpInfo.Length; ++i) {
                if (!cmpInfo[i].Equals(outInfo[i])) {
                    message = MessageFormatUtil.Format("Document info fail. Expected: \"{0}\", actual: \"{1}\"", cmpInfo[i], outInfo
                        [i]);
                    break;
                }
            }
            outDocument.Close();
            cmpDocument.Close();
            if (message == null) {
                Console.Out.WriteLine("OK");
            }
            else {
                Console.Out.WriteLine("Fail");
            }
            Console.Out.Flush();
            return message;
        }

        /// <summary>Compares document info dictionaries of two pdf documents.</summary>
        /// <param name="outPdf">the absolute path to the output file, which info is to be compared to cmp-file info.</param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which info is to be compared to output file info.</param>
        /// <returns>text report on the differences in documents infos.</returns>
        public virtual string CompareDocumentInfo(string outPdf, string cmpPdf) {
            return CompareDocumentInfo(outPdf, cmpPdf, null, null);
        }

        /// <summary>Checks if two documents have identical link annotations on corresponding pages.</summary>
        /// <param name="outPdf">the absolute path to the output file, which links are to be compared to cmp-file links.
        ///     </param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which links are to be compared to output file links.
        ///     </param>
        /// <returns>text report on the differences in documents links.</returns>
        public virtual string CompareLinkAnnotations(string outPdf, string cmpPdf) {
            Console.Out.Write("[itext] INFO  Comparing link annotations....");
            string message = null;
            var outDocument = new PdfDocument(new PdfReader(outPdf), new DocumentProperties().SetEventCountingMetaInfo
                (metaInfo));
            var cmpDocument = new PdfDocument(new PdfReader(cmpPdf), new DocumentProperties().SetEventCountingMetaInfo
                (metaInfo));
            for (var i = 0; i < outDocument.GetNumberOfPages() && i < cmpDocument.GetNumberOfPages(); i++) {
                var outLinks = GetLinkAnnotations(i + 1, outDocument);
                var cmpLinks = GetLinkAnnotations(i + 1, cmpDocument);
                if (cmpLinks.Count != outLinks.Count) {
                    message = MessageFormatUtil.Format("Different number of links on page {0}.", i + 1);
                    break;
                }
                for (var j = 0; j < cmpLinks.Count; j++) {
                    if (!CompareLinkAnnotations(cmpLinks[j], outLinks[j], cmpDocument, outDocument)) {
                        message = MessageFormatUtil.Format("Different links on page {0}.\n{1}\n{2}", i + 1, cmpLinks[j].ToString()
                            , outLinks[j].ToString());
                        break;
                    }
                }
            }
            outDocument.Close();
            cmpDocument.Close();
            if (message == null) {
                Console.Out.WriteLine("OK");
            }
            else {
                Console.Out.WriteLine("Fail");
            }
            Console.Out.Flush();
            return message;
        }

        /// <summary>Compares tag structures of the two PDF documents.</summary>
        /// <remarks>
        /// Compares tag structures of the two PDF documents.
        /// <para />
        /// This method creates xml files in the same folder with outPdf file. These xml files contain documents tag structures
        /// converted into the xml structure. These xml files are compared if they are equal.
        /// </remarks>
        /// <param name="outPdf">the absolute path to the output file, which tags are to be compared to cmp-file tags.
        ///     </param>
        /// <param name="cmpPdf">the absolute path to the cmp-file, which tags are to be compared to output file tags.
        ///     </param>
        /// <returns>text report of the differences in documents tags.</returns>
        public virtual string CompareTagStructures(string outPdf, string cmpPdf) {
            Console.Out.Write("[itext] INFO  Comparing tag structures......");
            var outXmlPath = outPdf.Replace(".pdf", ".xml");
            var cmpXmlPath = outPdf.Replace(".pdf", ".cmp.xml");
            string message = null;
            var readerOut = new PdfReader(outPdf);
            var docOut = new PdfDocument(readerOut, new DocumentProperties().SetEventCountingMetaInfo(metaInfo
                ));
            var xmlOut = new FileStream(outXmlPath, FileMode.Create);
            new TaggedPdfReaderTool(docOut).SetRootTag("root").ConvertToXml(xmlOut);
            docOut.Close();
            xmlOut.Dispose();
            var readerCmp = new PdfReader(cmpPdf);
            var docCmp = new PdfDocument(readerCmp, new DocumentProperties().SetEventCountingMetaInfo(metaInfo
                ));
            var xmlCmp = new FileStream(cmpXmlPath, FileMode.Create);
            new TaggedPdfReaderTool(docCmp).SetRootTag("root").ConvertToXml(xmlCmp);
            docCmp.Close();
            xmlCmp.Dispose();
            if (!CompareXmls(outXmlPath, cmpXmlPath)) {
                message = "The tag structures are different.";
            }
            if (message == null) {
                Console.Out.WriteLine("OK");
            }
            else {
                Console.Out.WriteLine("Fail");
            }
            Console.Out.Flush();
            return message;
        }

        internal virtual string[] ConvertInfo(PdfDocumentInfo info) {
            string[] convertedInfo = { "", "", "", "", "" };
            var infoValue = info.GetTitle();
            if (infoValue != null) {
                convertedInfo[0] = infoValue;
            }
            infoValue = info.GetAuthor();
            if (infoValue != null) {
                convertedInfo[1] = infoValue;
            }
            infoValue = info.GetSubject();
            if (infoValue != null) {
                convertedInfo[2] = infoValue;
            }
            infoValue = info.GetKeywords();
            if (infoValue != null) {
                convertedInfo[3] = infoValue;
            }
            infoValue = info.GetProducer();
            if (infoValue != null) {
                convertedInfo[4] = ConvertProducerLine(infoValue);
            }
            return convertedInfo;
        }

        internal virtual string ConvertProducerLine(string producer) {
            return StringUtil.ReplaceAll(StringUtil.ReplaceAll(producer, VERSION_REGEXP, VERSION_REPLACEMENT
                ), COPYRIGHT_REGEXP, COPYRIGHT_REPLACEMENT);
        }

        private void Init(string outPdf, string cmpPdf) {
            this.outPdf = outPdf;
            this.cmpPdf = cmpPdf;
            outPdfName = new FileInfo(outPdf).Name;
            cmpPdfName = new FileInfo(cmpPdf).Name;
            outImage = outPdfName + "-%03d.png";
            if (cmpPdfName.StartsWith("cmp_")) {
                cmpImage = cmpPdfName + "-%03d.png";
            }
            else {
                cmpImage = "cmp_" + cmpPdfName + "-%03d.png";
            }
        }

        private void SetPassword(byte[] outPass, byte[] cmpPass) {
            if (outPass != null) {
                GetOutReaderProperties().SetPassword(outPass);
            }
            if (cmpPass != null) {
                GetCmpReaderProperties().SetPassword(outPass);
            }
        }

        private string CompareVisually(string outPath, string differenceImagePrefix, IDictionary<int, IList<Rectangle
            >> ignoredAreas) {
            return CompareVisually(outPath, differenceImagePrefix, ignoredAreas, null);
        }

        private string CompareVisually(string outPath, string differenceImagePrefix, IDictionary<int, IList<Rectangle
            >> ignoredAreas, IList<int> equalPages) {
            if (!outPath.EndsWith("/")) {
                outPath = outPath + "/";
            }
            if (differenceImagePrefix == null) {
                var fileBasedPrefix = "";
                if (outPdfName != null) {
                    // should always be initialized by this moment
                    fileBasedPrefix = outPdfName + "_";
                }
                differenceImagePrefix = "diff_" + fileBasedPrefix;
            }
            PrepareOutputDirs(outPath, differenceImagePrefix);
            Console.Out.WriteLine("Comparing visually..........");
            if (ignoredAreas != null && !ignoredAreas.IsEmpty()) {
                CreateIgnoredAreasPdfs(outPath, ignoredAreas);
            }
            GhostscriptHelper ghostscriptHelper = null;
            try {
                ghostscriptHelper = new GhostscriptHelper(gsExec);
            }
            catch (ArgumentException e) {
                throw new CompareToolExecutionException(this, e.Message);
            }
            ghostscriptHelper.RunGhostScriptImageGeneration(outPdf, outPath, outImage);
            ghostscriptHelper.RunGhostScriptImageGeneration(cmpPdf, outPath, cmpImage);
            return CompareImagesOfPdfs(outPath, differenceImagePrefix, equalPages);
        }

        private string CompareImagesOfPdfs(string outPath, string differenceImagePrefix, IList<int> equalPages) {
            var imageFiles = FileUtil.ListFilesInDirectoryByFilter(outPath, new PngFileFilter(this)
                );
            var cmpImageFiles = FileUtil.ListFilesInDirectoryByFilter(outPath, new CmpPngFileFilter
                (this));
            var bUnexpectedNumberOfPages = false;
            if (imageFiles.Length != cmpImageFiles.Length) {
                bUnexpectedNumberOfPages = true;
            }
            var cnt = Math.Min(imageFiles.Length, cmpImageFiles.Length);
            if (cnt < 1) {
                throw new CompareToolExecutionException(this, "No files for comparing. The result or sample pdf file is not processed by GhostScript."
                    );
            }
            JavaUtil.Sort(imageFiles, new ImageNameComparator(this));
            JavaUtil.Sort(cmpImageFiles, new ImageNameComparator(this));
            bool compareExecIsOk;
            string imageMagickInitError = null;
            ImageMagickHelper imageMagickHelper = null;
            try {
                imageMagickHelper = new ImageMagickHelper(compareExec);
                compareExecIsOk = true;
            }
            catch (ArgumentException e) {
                compareExecIsOk = false;
                imageMagickInitError = e.Message;
                LogManager.GetLogger(typeof(CompareTool)).Warn(e.Message);
            }
            IList<int> diffPages = new List<int>();
            string differentPagesFail = null;
            for (var i = 0; i < cnt; i++) {
                if (equalPages != null && equalPages.Contains(i)) {
                    continue;
                }
                Console.Out.WriteLine("Comparing page " + JavaUtil.IntegerToString(i + 1) + ": " + UrlUtil.GetNormalizedFileUriString
                    (imageFiles[i].Name) + " ...");
                Console.Out.WriteLine("Comparing page " + JavaUtil.IntegerToString(i + 1) + ": " + UrlUtil.GetNormalizedFileUriString
                    (imageFiles[i].Name) + " ...");
                var is1 = new FileStream(imageFiles[i].FullName, FileMode.Open, FileAccess.Read);
                var is2 = new FileStream(cmpImageFiles[i].FullName, FileMode.Open, FileAccess.Read);
                var cmpResult = CompareStreams(is1, is2);
                is1.Dispose();
                is2.Dispose();
                if (!cmpResult) {
                    differentPagesFail = "Page is different!";
                    diffPages.Add(i + 1);
                    if (compareExecIsOk) {
                        var diffName = outPath + differenceImagePrefix + JavaUtil.IntegerToString(i + 1) + ".png";
                        if (!imageMagickHelper.RunImageMagickImageCompare(imageFiles[i].FullName, cmpImageFiles[i].FullName, diffName
                            )) {
                            var diffFile = new FileInfo(diffName);
                            differentPagesFail += "\nPlease, examine " + FILE_PROTOCOL + UrlUtil.ToNormalizedURI(diffFile).AbsolutePath
                                 + " for more details.";
                        }
                    }
                    Console.Out.WriteLine(differentPagesFail);
                }
                else {
                    Console.Out.WriteLine(" done.");
                }
            }
            if (differentPagesFail != null) {
                var errorMessage = DIFFERENT_PAGES.Replace("<filename>", UrlUtil.ToNormalizedURI(outPdf).AbsolutePath).
                    Replace("<pagenumber>", ListDiffPagesAsString(diffPages));
                if (!compareExecIsOk) {
                    errorMessage += "\n" + imageMagickInitError;
                }
                return errorMessage;
            }

            if (bUnexpectedNumberOfPages) {
	            return UNEXPECTED_NUMBER_OF_PAGES.Replace("<filename>", outPdf);
            }
            return null;
        }

        private string ListDiffPagesAsString(IList<int> diffPages) {
            var sb = new StringBuilder("[");
            for (var i = 0; i < diffPages.Count; i++) {
                sb.Append(diffPages[i]);
                if (i < diffPages.Count - 1) {
                    sb.Append(", ");
                }
            }
            sb.Append("]");
            return sb.ToString();
        }

        private void CreateIgnoredAreasPdfs(string outPath, IDictionary<int, IList<Rectangle>> ignoredAreas) {
            var outWriter = new PdfWriter(outPath + IGNORED_AREAS_PREFIX + outPdfName);
            var cmpWriter = new PdfWriter(outPath + IGNORED_AREAS_PREFIX + cmpPdfName);
            var properties = new StampingProperties();
            properties.SetEventCountingMetaInfo(metaInfo);
            var pdfOutDoc = new PdfDocument(new PdfReader(outPdf), outWriter, properties);
            var pdfCmpDoc = new PdfDocument(new PdfReader(cmpPdf), cmpWriter, properties);
            foreach (var entry in ignoredAreas) {
                var pageNumber = entry.Key;
                var rectangles = entry.Value;
                if (rectangles != null && !rectangles.IsEmpty()) {
                    var outCanvas = new PdfCanvas(pdfOutDoc.GetPage(pageNumber));
                    var cmpCanvas = new PdfCanvas(pdfCmpDoc.GetPage(pageNumber));
                    outCanvas.SaveState();
                    cmpCanvas.SaveState();
                    foreach (var rect in rectangles) {
                        outCanvas.Rectangle(rect).Fill();
                        cmpCanvas.Rectangle(rect).Fill();
                    }
                    outCanvas.RestoreState();
                    cmpCanvas.RestoreState();
                }
            }
            pdfOutDoc.Close();
            pdfCmpDoc.Close();
            Init(outPath + IGNORED_AREAS_PREFIX + outPdfName, outPath + IGNORED_AREAS_PREFIX + cmpPdfName);
        }

        private void PrepareOutputDirs(string outPath, string differenceImagePrefix) {
            FileInfo[] imageFiles;
            FileInfo[] cmpImageFiles;
            FileInfo[] diffFiles;
            if (!FileUtil.DirectoryExists(outPath)) {
                FileUtil.CreateDirectories(outPath);
            }
            else {
                imageFiles = FileUtil.ListFilesInDirectoryByFilter(outPath, new PngFileFilter(this));
                foreach (var file in imageFiles) {
                    file.Delete();
                }
                cmpImageFiles = FileUtil.ListFilesInDirectoryByFilter(outPath, new CmpPngFileFilter(this));
                foreach (var file in cmpImageFiles) {
                    file.Delete();
                }
                diffFiles = FileUtil.ListFilesInDirectoryByFilter(outPath, new DiffPngFileFilter(this, differenceImagePrefix
                    ));
                foreach (var file in diffFiles) {
                    file.Delete();
                }
            }
        }

        private void PrintOutCmpDirectories() {
            Console.Out.WriteLine("Out file folder: " + FILE_PROTOCOL + UrlUtil.ToNormalizedURI(new FileInfo(outPdf
                ).DirectoryName).AbsolutePath);
            Console.Out.WriteLine("Cmp file folder: " + FILE_PROTOCOL + UrlUtil.ToNormalizedURI(new FileInfo(cmpPdf
                ).DirectoryName).AbsolutePath);
        }

        private string CompareByContent(string outPath, string differenceImagePrefix, IDictionary<int, IList<Rectangle
            >> ignoredAreas) {
            PrintOutCmpDirectories();
            Console.Out.Write("Comparing by content..........");
            PdfDocument outDocument;
            try {
                outDocument = new PdfDocument(new PdfReader(outPdf, GetOutReaderProperties()), new DocumentProperties().SetEventCountingMetaInfo
                    (metaInfo));
            }
            catch (IOException e) {
                throw new IOException("File \"" + outPdf + "\" not found", e);
            }
            IList<PdfDictionary> outPages = new List<PdfDictionary>();
            outPagesRef = new List<PdfIndirectReference>();
            LoadPagesFromReader(outDocument, outPages, outPagesRef);
            PdfDocument cmpDocument;
            try {
                cmpDocument = new PdfDocument(new PdfReader(cmpPdf, GetCmpReaderProperties()), new DocumentProperties().SetEventCountingMetaInfo
                    (metaInfo));
            }
            catch (IOException e) {
                throw new IOException("File \"" + cmpPdf + "\" not found", e);
            }
            IList<PdfDictionary> cmpPages = new List<PdfDictionary>();
            cmpPagesRef = new List<PdfIndirectReference>();
            LoadPagesFromReader(cmpDocument, cmpPages, cmpPagesRef);
            if (outPages.Count != cmpPages.Count) {
                return CompareVisuallyAndCombineReports("Documents have different numbers of pages.", outPath, differenceImagePrefix
                    , ignoredAreas, null);
            }
            var compareResult = new CompareResult(this, compareByContentErrorsLimit);
            IList<int> equalPages = new List<int>(cmpPages.Count);
            for (var i = 0; i < cmpPages.Count; i++) {
                var currentPath = new ObjectPath(cmpPagesRef[i], outPagesRef[i]);
                if (CompareDictionariesExtended(outPages[i], cmpPages[i], currentPath, compareResult)) {
                    equalPages.Add(i);
                }
            }
            var catalogPath = new ObjectPath(cmpDocument.GetCatalog().GetPdfObject().GetIndirectReference
                (), outDocument.GetCatalog().GetPdfObject().GetIndirectReference());
            ICollection<PdfName> ignoredCatalogEntries = new LinkedHashSet<PdfName>(JavaUtil.ArraysAsList(PdfName.Pages
                , PdfName.Metadata));
            CompareDictionariesExtended(outDocument.GetCatalog().GetPdfObject(), cmpDocument.GetCatalog().GetPdfObject
                (), catalogPath, compareResult, ignoredCatalogEntries);
            if (encryptionCompareEnabled) {
                CompareDocumentsEncryption(outDocument, cmpDocument, compareResult);
            }
            outDocument.Close();
            cmpDocument.Close();
            if (generateCompareByContentXmlReport) {
                var outPdfName = new FileInfo(outPdf).Name;
                var xml = new FileStream(outPath + "/" + outPdfName.JSubstring(0, outPdfName.Length - 3) + "report.xml"
                    , FileMode.Create);
                try {
                    compareResult.WriteReportToXml(xml);
                }
                catch (Exception e) {
                    throw new Exception(e.Message, e);
                }
                finally {
                    xml.Dispose();
                }
            }
            if (equalPages.Count == cmpPages.Count && compareResult.IsOk()) {
                Console.Out.WriteLine("OK");
                Console.Out.Flush();
                return null;
            }

            return CompareVisuallyAndCombineReports(compareResult.GetReport(), outPath, differenceImagePrefix, ignoredAreas
	            , equalPages);
        }

        private string CompareVisuallyAndCombineReports(string compareByFailContentReason, string outPath, string 
            differenceImagePrefix, IDictionary<int, IList<Rectangle>> ignoredAreas, IList<int> equalPages) {
            Console.Out.WriteLine("Fail");
            Console.Out.Flush();
            var compareByContentReport = "Compare by content report:\n" + compareByFailContentReason;
            Console.Out.WriteLine(compareByContentReport);
            Console.Out.Flush();
            var message = CompareVisually(outPath, differenceImagePrefix, ignoredAreas, equalPages);
            if (message == null || message.Length == 0) {
                return "Compare by content fails. No visual differences";
            }
            return message;
        }

        private void LoadPagesFromReader(PdfDocument doc, IList<PdfDictionary> pages, IList<PdfIndirectReference> 
            pagesRef) {
            var numOfPages = doc.GetNumberOfPages();
            for (var i = 0; i < numOfPages; ++i) {
                pages.Add(doc.GetPage(i + 1).GetPdfObject());
                pagesRef.Add(pages[i].GetIndirectReference());
            }
        }

        private void CompareDocumentsEncryption(PdfDocument outDocument, PdfDocument cmpDocument, CompareResult
             compareResult) {
            var outEncrypt = outDocument.GetTrailer().GetAsDictionary(PdfName.Encrypt);
            var cmpEncrypt = cmpDocument.GetTrailer().GetAsDictionary(PdfName.Encrypt);
            if (outEncrypt == null && cmpEncrypt == null) {
                return;
            }
            var trailerPath = new TrailerPath(cmpDocument, outDocument);
            if (outEncrypt == null) {
                compareResult.AddError(trailerPath, "Expected encrypted document.");
                return;
            }
            if (cmpEncrypt == null) {
                compareResult.AddError(trailerPath, "Expected not encrypted document.");
                return;
            }
            ICollection<PdfName> ignoredEncryptEntries = new LinkedHashSet<PdfName>(JavaUtil.ArraysAsList(PdfName.O, PdfName
                .U, PdfName.OE, PdfName.UE, PdfName.Perms, PdfName.CF, PdfName.Recipients));
            var objectPath = new ObjectPath(outEncrypt.GetIndirectReference(), cmpEncrypt
                .GetIndirectReference());
            CompareDictionariesExtended(outEncrypt, cmpEncrypt, objectPath, compareResult, ignoredEncryptEntries);
            var outCfDict = outEncrypt.GetAsDictionary(PdfName.CF);
            var cmpCfDict = cmpEncrypt.GetAsDictionary(PdfName.CF);
            if (cmpCfDict != null || outCfDict != null) {
                if (cmpCfDict != null && outCfDict == null || cmpCfDict == null) {
                    compareResult.AddError(objectPath, "One of the dictionaries is null, the other is not.");
                }
                else {
                    ICollection<PdfName> mergedKeys = new SortedSet<PdfName>(outCfDict.KeySet());
                    mergedKeys.AddAll(cmpCfDict.KeySet());
                    foreach (var key in mergedKeys) {
                        objectPath.PushDictItemToPath(key);
                        var excludedKeys = new LinkedHashSet<PdfName>(JavaUtil.ArraysAsList(PdfName.Recipients)
                            );
                        CompareDictionariesExtended(outCfDict.GetAsDictionary(key), cmpCfDict.GetAsDictionary(key), objectPath, compareResult
                            , excludedKeys);
                        objectPath.Pop();
                    }
                }
            }
        }

        private bool CompareStreams(Stream is1, Stream is2) {
            var buffer1 = new byte[64 * 1024];
            var buffer2 = new byte[64 * 1024];
            int len1;
            int len2;
            for (; ; ) {
                len1 = is1.Read(buffer1);
                len2 = is2.Read(buffer2);
                if (len1 != len2) {
                    return false;
                }
                if (!JavaUtil.ArraysEquals(buffer1, buffer2)) {
                    return false;
                }
                if (len1 == -1) {
                    break;
                }
            }
            return true;
        }

        private bool CompareDictionariesExtended(PdfDictionary outDict, PdfDictionary cmpDict, ObjectPath
             currentPath, CompareResult compareResult) {
            return CompareDictionariesExtended(outDict, cmpDict, currentPath, compareResult, null);
        }

        private bool CompareDictionariesExtended(PdfDictionary outDict, PdfDictionary cmpDict, ObjectPath
             currentPath, CompareResult compareResult, ICollection<PdfName> excludedKeys) {
            if (cmpDict != null && outDict == null || outDict != null && cmpDict == null) {
                compareResult.AddError(currentPath, "One of the dictionaries is null, the other is not.");
                return false;
            }
            var dictsAreSame = true;
            // Iterate through the union of the keys of the cmp and out dictionaries
            ICollection<PdfName> mergedKeys = new SortedSet<PdfName>(cmpDict.KeySet());
            mergedKeys.AddAll(outDict.KeySet());
            foreach (var key in mergedKeys) {
                if (!dictsAreSame && (currentPath == null || compareResult == null || compareResult.IsMessageLimitReached(
                    ))) {
                    return false;
                }
                if (excludedKeys != null && excludedKeys.Contains(key)) {
                    continue;
                }
                if (key.Equals(PdfName.Parent) || key.Equals(PdfName.P) || key.Equals(PdfName.ModDate)) {
                    continue;
                }
                if (outDict.IsStream() && cmpDict.IsStream() && (key.Equals(PdfName.Filter) || key.Equals(PdfName.Length))
                    ) {
                    continue;
                }
                if (key.Equals(PdfName.BaseFont) || key.Equals(PdfName.FontName)) {
                    var cmpObj = cmpDict.Get(key);
                    if (cmpObj != null && cmpObj.IsName() && cmpObj.ToString().IndexOf('+') > 0) {
                        var outObj = outDict.Get(key);
                        if (!outObj.IsName() || outObj.ToString().IndexOf('+') == -1) {
                            if (compareResult != null && currentPath != null) {
                                compareResult.AddError(currentPath, MessageFormatUtil.Format("PdfDictionary {0} entry: Expected: {1}. Found: {2}"
                                    , key.ToString(), cmpObj.ToString(), outObj.ToString()));
                            }
                            dictsAreSame = false;
                        }
                        else {
                            var cmpName = cmpObj.ToString().Substring(cmpObj.ToString().IndexOf('+'));
                            var outName = outObj.ToString().Substring(outObj.ToString().IndexOf('+'));
                            if (!cmpName.Equals(outName)) {
                                if (compareResult != null && currentPath != null) {
                                    compareResult.AddError(currentPath, MessageFormatUtil.Format("PdfDictionary {0} entry: Expected: {1}. Found: {2}"
                                        , key.ToString(), cmpObj.ToString(), outObj.ToString()));
                                }
                                dictsAreSame = false;
                            }
                        }
                        continue;
                    }
                }
                // A number tree can be stored in multiple, semantically equivalent ways.
                // Flatten to a single array, in order to get a canonical representation.
                if (key.Equals(PdfName.ParentTree) || key.Equals(PdfName.PageLabels)) {
                    if (currentPath != null) {
                        currentPath.PushDictItemToPath(key);
                    }
                    var outNumTree = outDict.GetAsDictionary(key);
                    var cmpNumTree = cmpDict.GetAsDictionary(key);
                    var outItems = new LinkedList<PdfObject>();
                    var cmpItems = new LinkedList<PdfObject>();
                    var outLeftover = FlattenNumTree(outNumTree, null, outItems);
                    var cmpLeftover = FlattenNumTree(cmpNumTree, null, cmpItems);
                    if (outLeftover != null) {
                        LogManager.GetLogger(typeof(CompareTool)).Warn(LogMessageConstant.NUM_TREE_SHALL_NOT_END_WITH_KEY
                            );
                        if (cmpLeftover == null) {
                            if (compareResult != null && currentPath != null) {
                                compareResult.AddError(currentPath, "Number tree unexpectedly ends with a key");
                            }
                            dictsAreSame = false;
                        }
                    }
                    if (cmpLeftover != null) {
                        LogManager.GetLogger(typeof(CompareTool)).Warn(LogMessageConstant.NUM_TREE_SHALL_NOT_END_WITH_KEY
                            );
                        if (outLeftover == null) {
                            if (compareResult != null && currentPath != null) {
                                compareResult.AddError(currentPath, "Number tree was expected to end with a key (although it is invalid according to the specification), but ended with a value"
                                    );
                            }
                            dictsAreSame = false;
                        }
                    }
                    if (outLeftover != null && cmpLeftover != null && !CompareNumbers(outLeftover, cmpLeftover)) {
                        if (compareResult != null && currentPath != null) {
                            compareResult.AddError(currentPath, "Number tree was expected to end with a different key (although it is invalid according to the specification)"
                                );
                        }
                        dictsAreSame = false;
                    }
                    var outArray = new PdfArray(outItems, outItems.Count);
                    var cmpArray = new PdfArray(cmpItems, cmpItems.Count);
                    if (!CompareArraysExtended(outArray, cmpArray, currentPath, compareResult)) {
                        if (compareResult != null && currentPath != null) {
                            compareResult.AddError(currentPath, "Number trees were flattened, compared and found to be different.");
                        }
                        dictsAreSame = false;
                    }
                    if (currentPath != null) {
                        currentPath.Pop();
                    }
                    continue;
                }
                if (currentPath != null) {
                    currentPath.PushDictItemToPath(key);
                }
                dictsAreSame = CompareObjects(outDict.Get(key, false), cmpDict.Get(key, false), currentPath, compareResult
                    ) && dictsAreSame;
                if (currentPath != null) {
                    currentPath.Pop();
                }
            }
            return dictsAreSame;
        }

        private PdfNumber FlattenNumTree(PdfDictionary dictionary, PdfNumber leftOver, LinkedList<PdfObject> items
            ) {
            /*Map<PdfNumber, PdfObject> items*/
            var nums = dictionary.GetAsArray(PdfName.Nums);
            if (nums != null) {
                for (var k = 0; k < nums.Size(); k++) {
                    PdfNumber number;
                    if (leftOver == null) {
                        number = nums.GetAsNumber(k++);
                    }
                    else {
                        number = leftOver;
                        leftOver = null;
                    }
                    if (k < nums.Size()) {
                        items.AddLast(number);
                        items.AddLast(nums.Get(k, false));
                    }
                    else {
                        return number;
                    }
                }
            }
            else {
                if ((nums = dictionary.GetAsArray(PdfName.Kids)) != null) {
                    for (var k = 0; k < nums.Size(); k++) {
                        var kid = nums.GetAsDictionary(k);
                        leftOver = FlattenNumTree(kid, leftOver, items);
                    }
                }
            }
            return null;
        }

        protected internal virtual bool CompareObjects(PdfObject outObj, PdfObject cmpObj, ObjectPath 
            currentPath, CompareResult compareResult) {
            PdfObject outDirectObj = null;
            PdfObject cmpDirectObj = null;
            if (outObj != null) {
                outDirectObj = outObj.IsIndirectReference() ? ((PdfIndirectReference)outObj).GetRefersTo(false) : outObj;
            }
            if (cmpObj != null) {
                cmpDirectObj = cmpObj.IsIndirectReference() ? ((PdfIndirectReference)cmpObj).GetRefersTo(false) : cmpObj;
            }
            if (cmpDirectObj == null && outDirectObj == null) {
                return true;
            }
            if (outDirectObj == null) {
                compareResult.AddError(currentPath, "Expected object was not found.");
                return false;
            }

            if (cmpDirectObj == null) {
	            compareResult.AddError(currentPath, "Found object which was not expected to be found.");
	            return false;
            }

            if (cmpDirectObj.GetObjectType() != outDirectObj.GetObjectType()) {
	            compareResult.AddError(currentPath, MessageFormatUtil.Format("Types do not match. Expected: {0}. Found: {1}."
		            , cmpDirectObj.GetType().Name, outDirectObj.GetType().Name));
	            return false;
            }

            if (cmpObj.IsIndirectReference() && !outObj.IsIndirectReference()) {
	            compareResult.AddError(currentPath, "Expected indirect object.");
	            return false;
            }

            if (!cmpObj.IsIndirectReference() && outObj.IsIndirectReference()) {
	            compareResult.AddError(currentPath, "Expected direct object.");
	            return false;
            }
            if (currentPath != null && cmpObj.IsIndirectReference() && outObj.IsIndirectReference()) {
                if (currentPath.IsComparing((PdfIndirectReference)cmpObj, (PdfIndirectReference)outObj)) {
                    return true;
                }
                currentPath = currentPath.ResetDirectPath((PdfIndirectReference)cmpObj, (PdfIndirectReference)outObj);
            }
            if (cmpDirectObj.IsDictionary() && PdfName.Page.Equals(((PdfDictionary)cmpDirectObj).GetAsName(PdfName.Type
                )) && useCachedPagesForComparison) {
                if (!outDirectObj.IsDictionary() || !PdfName.Page.Equals(((PdfDictionary)outDirectObj).GetAsName(PdfName.Type
                    ))) {
                    if (compareResult != null && currentPath != null) {
                        compareResult.AddError(currentPath, "Expected a page. Found not a page.");
                    }
                    return false;
                }
                var cmpRefKey = cmpObj.IsIndirectReference() ? (PdfIndirectReference)cmpObj : cmpObj.GetIndirectReference
                    ();
                var outRefKey = outObj.IsIndirectReference() ? (PdfIndirectReference)outObj : outObj.GetIndirectReference
                    ();
                // References to the same page
                if (cmpPagesRef == null) {
                    cmpPagesRef = new List<PdfIndirectReference>();
                    for (var i = 1; i <= cmpRefKey.GetDocument().GetNumberOfPages(); ++i) {
                        cmpPagesRef.Add(cmpRefKey.GetDocument().GetPage(i).GetPdfObject().GetIndirectReference());
                    }
                }
                if (outPagesRef == null) {
                    outPagesRef = new List<PdfIndirectReference>();
                    for (var i = 1; i <= outRefKey.GetDocument().GetNumberOfPages(); ++i) {
                        outPagesRef.Add(outRefKey.GetDocument().GetPage(i).GetPdfObject().GetIndirectReference());
                    }
                }
                // If at least one of the page dictionaries is in the document's page tree, we don't proceed with deep comparison,
                // because pages are compared at different level, so we compare only their index.
                // However only if both page dictionaries are not in the document's page trees, we continue to comparing them as normal dictionaries.
                if (cmpPagesRef.Contains(cmpRefKey) || outPagesRef.Contains(outRefKey)) {
                    if (cmpPagesRef.Contains(cmpRefKey) && cmpPagesRef.IndexOf(cmpRefKey) == outPagesRef.IndexOf(outRefKey)) {
                        return true;
                    }
                    if (compareResult != null && currentPath != null) {
                        compareResult.AddError(currentPath, MessageFormatUtil.Format("The dictionaries refer to different pages. Expected page number: {0}. Found: {1}"
                            , cmpPagesRef.IndexOf(cmpRefKey) + 1, outPagesRef.IndexOf(outRefKey) + 1));
                    }
                    return false;
                }
            }
            if (cmpDirectObj.IsDictionary()) {
                return CompareDictionariesExtended((PdfDictionary)outDirectObj, (PdfDictionary)cmpDirectObj, currentPath, 
                    compareResult);
            }

            if (cmpDirectObj.IsStream()) {
	            return CompareStreamsExtended((PdfStream)outDirectObj, (PdfStream)cmpDirectObj, currentPath, compareResult
	            );
            }

            if (cmpDirectObj.IsArray()) {
	            return CompareArraysExtended((PdfArray)outDirectObj, (PdfArray)cmpDirectObj, currentPath, compareResult);
            }

            if (cmpDirectObj.IsName()) {
	            return CompareNamesExtended((PdfName)outDirectObj, (PdfName)cmpDirectObj, currentPath, compareResult);
            }

            if (cmpDirectObj.IsNumber()) {
	            return CompareNumbersExtended((PdfNumber)outDirectObj, (PdfNumber)cmpDirectObj, currentPath, compareResult
	            );
            }

            if (cmpDirectObj.IsString()) {
	            return CompareStringsExtended((PdfString)outDirectObj, (PdfString)cmpDirectObj, currentPath, compareResult
	            );
            }

            if (cmpDirectObj.IsBoolean()) {
	            return CompareBooleansExtended((PdfBoolean)outDirectObj, (PdfBoolean)cmpDirectObj, currentPath, compareResult
	            );
            }

            if (outDirectObj.IsNull() && cmpDirectObj.IsNull()) {
	            return true;
            }

            throw new NotSupportedException();
        }

        private bool CompareStreamsExtended(PdfStream outStream, PdfStream cmpStream, ObjectPath currentPath
            , CompareResult compareResult) {
            var toDecode = PdfName.FlateDecode.Equals(outStream.Get(PdfName.Filter));
            var outStreamBytes = outStream.GetBytes(toDecode);
            var cmpStreamBytes = cmpStream.GetBytes(toDecode);
            if (JavaUtil.ArraysEquals(outStreamBytes, cmpStreamBytes)) {
                return CompareDictionariesExtended(outStream, cmpStream, currentPath, compareResult);
            }

            var errorMessage = new StringBuilder();
            if (cmpStreamBytes.Length != outStreamBytes.Length) {
	            errorMessage.Append(MessageFormatUtil.Format("PdfStream. Lengths are different. Expected: {0}. Found: {1}\n"
		            , cmpStreamBytes.Length, outStreamBytes.Length));
            }
            else {
	            errorMessage.Append("PdfStream. Bytes are different.\n");
            }
            var firstDifferenceOffset = FindBytesDifference(outStreamBytes, cmpStreamBytes, errorMessage);
            if (compareResult != null && currentPath != null) {
	            currentPath.PushOffsetToPath(firstDifferenceOffset);
	            compareResult.AddError(currentPath, errorMessage.ToString());
	            currentPath.Pop();
            }
            return false;
        }

        /// <returns>first difference offset</returns>
        private int FindBytesDifference(byte[] outStreamBytes, byte[] cmpStreamBytes, StringBuilder errorMessage) {
            var numberOfDifferentBytes = 0;
            var firstDifferenceOffset = 0;
            var minLength = Math.Min(cmpStreamBytes.Length, outStreamBytes.Length);
            for (var i = 0; i < minLength; i++) {
                if (cmpStreamBytes[i] != outStreamBytes[i]) {
                    ++numberOfDifferentBytes;
                    if (numberOfDifferentBytes == 1) {
                        firstDifferenceOffset = i;
                    }
                }
            }
            string bytesDifference = null;
            if (numberOfDifferentBytes > 0) {
                var diffBytesAreaL = 10;
                var diffBytesAreaR = 10;
                var lCmp = Math.Max(0, firstDifferenceOffset - diffBytesAreaL);
                var rCmp = Math.Min(cmpStreamBytes.Length, firstDifferenceOffset + diffBytesAreaR);
                var lOut = Math.Max(0, firstDifferenceOffset - diffBytesAreaL);
                var rOut = Math.Min(outStreamBytes.Length, firstDifferenceOffset + diffBytesAreaR);
                var cmpByte = JavaUtil.GetStringForBytes(new[] { cmpStreamBytes[firstDifferenceOffset
                    ] }, EncodingUtil.ISO_8859_1);
                var cmpByteNeighbours = StringUtil.ReplaceAll(JavaUtil.GetStringForBytes(cmpStreamBytes
                    , lCmp, rCmp - lCmp, EncodingUtil.ISO_8859_1), NEW_LINES, " ");
                var outByte = JavaUtil.GetStringForBytes(new[] { outStreamBytes[firstDifferenceOffset
                    ] }, EncodingUtil.ISO_8859_1);
                var outBytesNeighbours = StringUtil.ReplaceAll(JavaUtil.GetStringForBytes(outStreamBytes
                    , lOut, rOut - lOut, EncodingUtil.ISO_8859_1), NEW_LINES, " ");
                bytesDifference = MessageFormatUtil.Format("First bytes difference is encountered at index {0}. Expected: {1} ({2}). Found: {3} ({4}). Total number of different bytes: {5}"
                    , JavaUtil.IntegerToString(Convert.ToInt32(firstDifferenceOffset)), cmpByte, cmpByteNeighbours, outByte
                    , outBytesNeighbours, numberOfDifferentBytes);
            }
            else {
                // lengths are different
                firstDifferenceOffset = minLength;
                bytesDifference = MessageFormatUtil.Format("Bytes of the shorter array are the same as the first {0} bytes of the longer one."
                    , minLength);
            }
            errorMessage.Append(bytesDifference);
            return firstDifferenceOffset;
        }

        private bool CompareArraysExtended(PdfArray outArray, PdfArray cmpArray, ObjectPath currentPath
            , CompareResult compareResult) {
            if (outArray == null) {
                if (compareResult != null && currentPath != null) {
                    compareResult.AddError(currentPath, "Found null. Expected PdfArray.");
                }
                return false;
            }

            if (outArray.Size() != cmpArray.Size()) {
	            if (compareResult != null && currentPath != null) {
		            compareResult.AddError(currentPath, MessageFormatUtil.Format("PdfArrays. Lengths are different. Expected: {0}. Found: {1}."
			            , cmpArray.Size(), outArray.Size()));
	            }
	            return false;
            }
            var arraysAreEqual = true;
            for (var i = 0; i < cmpArray.Size(); i++) {
                if (currentPath != null) {
                    currentPath.PushArrayItemToPath(i);
                }
                arraysAreEqual = CompareObjects(outArray.Get(i, false), cmpArray.Get(i, false), currentPath, compareResult
                    ) && arraysAreEqual;
                if (currentPath != null) {
                    currentPath.Pop();
                }
                if (!arraysAreEqual && (currentPath == null || compareResult == null || compareResult.IsMessageLimitReached
                    ())) {
                    return false;
                }
            }
            return arraysAreEqual;
        }

        private bool CompareNamesExtended(PdfName outName, PdfName cmpName, ObjectPath currentPath, CompareResult
             compareResult)
        {
	        if (cmpName.Equals(outName)) {
                return true;
            }

	        if (compareResult != null && currentPath != null) {
		        compareResult.AddError(currentPath, MessageFormatUtil.Format("PdfName. Expected: {0}. Found: {1}", cmpName
			        .ToString(), outName.ToString()));
	        }
	        return false;
        }

        private bool CompareNumbersExtended(PdfNumber outNumber, PdfNumber cmpNumber, ObjectPath currentPath
            , CompareResult compareResult)
        {
	        if (cmpNumber.GetValue() == outNumber.GetValue()) {
                return true;
            }

	        if (compareResult != null && currentPath != null) {
		        compareResult.AddError(currentPath, MessageFormatUtil.Format("PdfNumber. Expected: {0}. Found: {1}", cmpNumber
			        , outNumber));
	        }
	        return false;
        }

        private bool CompareStringsExtended(PdfString outString, PdfString cmpString, ObjectPath currentPath
            , CompareResult compareResult)
        {
	        if (JavaUtil.ArraysEquals(ConvertPdfStringToBytes(cmpString), ConvertPdfStringToBytes(outString))) {
                return true;
            }

	        var cmpStr = cmpString.ToUnicodeString();
	        var outStr = outString.ToUnicodeString();
	        var errorMessage = new StringBuilder();
	        if (cmpStr.Length != outStr.Length) {
		        errorMessage.Append(MessageFormatUtil.Format("PdfString. Lengths are different. Expected: {0}. Found: {1}\n"
			        , cmpStr.Length, outStr.Length));
	        }
	        else {
		        errorMessage.Append("PdfString. Characters are different.\n");
	        }
	        var firstDifferenceOffset = FindStringDifference(outStr, cmpStr, errorMessage);
	        if (compareResult != null && currentPath != null) {
		        currentPath.PushOffsetToPath(firstDifferenceOffset);
		        compareResult.AddError(currentPath, errorMessage.ToString());
		        currentPath.Pop();
	        }
	        return false;
        }

        private int FindStringDifference(string outString, string cmpString, StringBuilder errorMessage) {
            var numberOfDifferentChars = 0;
            var firstDifferenceOffset = 0;
            var minLength = Math.Min(cmpString.Length, outString.Length);
            for (var i = 0; i < minLength; i++) {
                if (cmpString[i] != outString[i]) {
                    ++numberOfDifferentChars;
                    if (numberOfDifferentChars == 1) {
                        firstDifferenceOffset = i;
                    }
                }
            }
            string stringDifference = null;
            if (numberOfDifferentChars > 0) {
                var diffBytesAreaL = 15;
                var diffBytesAreaR = 15;
                var lCmp = Math.Max(0, firstDifferenceOffset - diffBytesAreaL);
                var rCmp = Math.Min(cmpString.Length, firstDifferenceOffset + diffBytesAreaR);
                var lOut = Math.Max(0, firstDifferenceOffset - diffBytesAreaL);
                var rOut = Math.Min(outString.Length, firstDifferenceOffset + diffBytesAreaR);
                var cmpByte = cmpString[firstDifferenceOffset].ToString();
                var cmpByteNeighbours = StringUtil.ReplaceAll(cmpString.JSubstring(lCmp, rCmp), NEW_LINES
                    , " ");
                var outByte = outString[firstDifferenceOffset].ToString();
                var outBytesNeighbours = StringUtil.ReplaceAll(outString.JSubstring(lOut, rOut), NEW_LINES
                    , " ");
                stringDifference = MessageFormatUtil.Format("First characters difference is encountered at index {0}.\nExpected: {1} ({2}).\nFound: {3} ({4}).\nTotal number of different characters: {5}"
                    , JavaUtil.IntegerToString(Convert.ToInt32(firstDifferenceOffset)), cmpByte, cmpByteNeighbours, outByte
                    , outBytesNeighbours, numberOfDifferentChars);
            }
            else {
                // lengths are different
                firstDifferenceOffset = minLength;
                stringDifference = MessageFormatUtil.Format("All characters of the shorter string are the same as the first {0} characters of the longer one."
                    , minLength);
            }
            errorMessage.Append(stringDifference);
            return firstDifferenceOffset;
        }

        private byte[] ConvertPdfStringToBytes(PdfString pdfString) {
            byte[] bytes;
            var value = pdfString.GetValue();
            var encoding = pdfString.GetEncoding();
            if (encoding != null && PdfEncodings.UNICODE_BIG.Equals(encoding) && PdfEncodings.IsPdfDocEncoding(value)) {
                bytes = PdfEncodings.ConvertToBytes(value, PdfEncodings.PDF_DOC_ENCODING);
            }
            else {
                bytes = PdfEncodings.ConvertToBytes(value, encoding);
            }
            return bytes;
        }

        private bool CompareBooleansExtended(PdfBoolean outBoolean, PdfBoolean cmpBoolean, ObjectPath 
            currentPath, CompareResult compareResult)
        {
	        if (cmpBoolean.GetValue() == outBoolean.GetValue()) {
                return true;
            }

	        if (compareResult != null && currentPath != null) {
		        compareResult.AddError(currentPath, MessageFormatUtil.Format("PdfBoolean. Expected: {0}. Found: {1}.", cmpBoolean
			        .GetValue(), outBoolean.GetValue()));
	        }
	        return false;
        }

        private IList<PdfLinkAnnotation> GetLinkAnnotations(int pageNum, PdfDocument document) {
            IList<PdfLinkAnnotation> linkAnnotations = new List<PdfLinkAnnotation>();
            var annotations = document.GetPage(pageNum).GetAnnotations();
            foreach (var annotation in annotations) {
                if (PdfName.Link.Equals(annotation.GetSubtype())) {
                    linkAnnotations.Add((PdfLinkAnnotation)annotation);
                }
            }
            return linkAnnotations;
        }

        private bool CompareLinkAnnotations(PdfLinkAnnotation cmpLink, PdfLinkAnnotation outLink, PdfDocument cmpDocument
            , PdfDocument outDocument) {
            // Compare link rectangles, page numbers the links refer to, and simple parameters (non-indirect, non-arrays, non-dictionaries)
            var cmpDestObject = cmpLink.GetDestinationObject();
            var outDestObject = outLink.GetDestinationObject();
            if (cmpDestObject != null && outDestObject != null)
            {
	            if (cmpDestObject.GetObjectType() != outDestObject.GetObjectType()) {
                    return false;
                }

	            PdfArray explicitCmpDest = null;
	            PdfArray explicitOutDest = null;
	            var cmpNamedDestinations = cmpDocument.GetCatalog().GetNameTree(PdfName.Dests).
		            GetNames();
	            var outNamedDestinations = outDocument.GetCatalog().GetNameTree(PdfName.Dests).
		            GetNames();
	            switch (cmpDestObject.GetObjectType()) {
		            case PdfObject.ARRAY: {
			            explicitCmpDest = (PdfArray)cmpDestObject;
			            explicitOutDest = (PdfArray)outDestObject;
			            break;
		            }

		            case PdfObject.NAME: {
			            explicitCmpDest = (PdfArray)cmpNamedDestinations.Get(((PdfName)cmpDestObject).GetValue());
			            explicitOutDest = (PdfArray)outNamedDestinations.Get(((PdfName)outDestObject).GetValue());
			            break;
		            }

		            case PdfObject.STRING: {
			            explicitCmpDest = (PdfArray)cmpNamedDestinations.Get(((PdfString)cmpDestObject).ToUnicodeString());
			            explicitOutDest = (PdfArray)outNamedDestinations.Get(((PdfString)outDestObject).ToUnicodeString());
			            break;
		            }
	            }
	            if (GetExplicitDestinationPageNum(explicitCmpDest) != GetExplicitDestinationPageNum(explicitOutDest)) {
		            return false;
	            }
            }
            var cmpDict = cmpLink.GetPdfObject();
            var outDict = outLink.GetPdfObject();
            if (cmpDict.Size() != outDict.Size()) {
                return false;
            }
            var cmpRect = cmpDict.GetAsRectangle(PdfName.Rect);
            var outRect = outDict.GetAsRectangle(PdfName.Rect);
            if (cmpRect.GetHeight() != outRect.GetHeight() || cmpRect.GetWidth() != outRect.GetWidth() || cmpRect.GetX
                () != outRect.GetX() || cmpRect.GetY() != outRect.GetY()) {
                return false;
            }
            foreach (var cmpEntry in cmpDict.EntrySet()) {
                var cmpObj = cmpEntry.Value;
                if (!outDict.ContainsKey(cmpEntry.Key)) {
                    return false;
                }
                var outObj = outDict.Get(cmpEntry.Key);
                if (cmpObj.GetObjectType() != outObj.GetObjectType()) {
                    return false;
                }
                switch (cmpObj.GetObjectType()) {
                    case PdfObject.NULL:
                    case PdfObject.BOOLEAN:
                    case PdfObject.NUMBER:
                    case PdfObject.STRING:
                    case PdfObject.NAME: {
                        if (!cmpObj.ToString().Equals(outObj.ToString())) {
                            return false;
                        }
                        break;
                    }
                }
            }
            return true;
        }

        private int GetExplicitDestinationPageNum(PdfArray explicitDest) {
            var pageReference = (PdfIndirectReference)explicitDest.Get(0, false);
            var doc = pageReference.GetDocument();
            for (var i = 1; i <= doc.GetNumberOfPages(); ++i) {
                if (doc.GetPage(i).GetPdfObject().GetIndirectReference().Equals(pageReference)) {
                    return i;
                }
            }
            throw new ArgumentException("PdfLinkAnnotation comparison: Page not found.");
        }

        private class PngFileFilter : FileUtil.IFileFilter {
            public virtual bool Accept(FileInfo pathname) {
                var ap = pathname.Name;
                var b1 = ap.EndsWith(".png");
                var b2 = ap.Contains("cmp_");
                return b1 && !b2 && ap.Contains(_enclosing.outPdfName);
            }

            internal PngFileFilter(CompareTool _enclosing) {
                this._enclosing = _enclosing;
            }

            private readonly CompareTool _enclosing;
        }

        private class CmpPngFileFilter : FileUtil.IFileFilter {
            public virtual bool Accept(FileInfo pathname) {
                var ap = pathname.Name;
                var b1 = ap.EndsWith(".png");
                var b2 = ap.Contains("cmp_");
                return b1 && b2 && ap.Contains(_enclosing.cmpPdfName);
            }

            internal CmpPngFileFilter(CompareTool _enclosing) {
                this._enclosing = _enclosing;
            }

            private readonly CompareTool _enclosing;
        }

        private class DiffPngFileFilter : FileUtil.IFileFilter {
            private string differenceImagePrefix;

            public DiffPngFileFilter(CompareTool _enclosing, string differenceImagePrefix) {
                this._enclosing = _enclosing;
                this.differenceImagePrefix = differenceImagePrefix;
            }

            public virtual bool Accept(FileInfo pathname) {
                var ap = pathname.Name;
                var b1 = ap.EndsWith(".png");
                var b2 = ap.StartsWith(differenceImagePrefix);
                return b1 && b2;
            }

            private readonly CompareTool _enclosing;
        }

        private class ImageNameComparator : IComparer<FileInfo> {
            public virtual int Compare(FileInfo f1, FileInfo f2) {
                var f1Name = f1.Name;
                var f2Name = f2.Name;
                return string.CompareOrdinal(f1Name, f2Name);
            }

            internal ImageNameComparator(CompareTool _enclosing) {
                this._enclosing = _enclosing;
            }

            private readonly CompareTool _enclosing;
        }

        /// <summary>Class containing results of the comparison of two documents.</summary>
        public class CompareResult {
            // LinkedHashMap to retain order. HashMap has different order in Java6/7 and Java8
            protected internal IDictionary<ObjectPath, string> differences = new LinkedDictionary<ObjectPath
                , string>();

            protected internal int messageLimit = 1;

            /// <summary>Creates new empty instance of CompareResult with given limit of difference messages.</summary>
            /// <param name="messageLimit">maximum number of difference messages to be handled by this CompareResult.</param>
            public CompareResult(CompareTool _enclosing, int messageLimit) {
                this._enclosing = _enclosing;
                this.messageLimit = messageLimit;
            }

            /// <summary>Verifies if documents are considered equal after comparison.</summary>
            /// <returns>true if documents are equal, false otherwise.</returns>
            public virtual bool IsOk() {
                return differences.Count == 0;
            }

            /// <summary>Returns number of differences between two documents detected during comparison.</summary>
            /// <returns>number of differences.</returns>
            public virtual int GetErrorCount() {
                return differences.Count;
            }

            /// <summary>Converts this CompareResult into text form.</summary>
            /// <returns>text report on the differences between two documents.</returns>
            public virtual string GetReport() {
                var sb = new StringBuilder();
                var firstEntry = true;
                foreach (var entry in differences) {
                    if (!firstEntry) {
                        sb.Append("-----------------------------").Append("\n");
                    }
                    var diffPath = entry.Key;
                    sb.Append(entry.Value).Append("\n").Append(diffPath).Append("\n");
                    firstEntry = false;
                }
                return sb.ToString();
            }

            /// <summary>
            /// Returns map with
            /// <see cref="ObjectPath"/>
            /// as keys and difference descriptions as values.
            /// </summary>
            /// <returns>differences map which could be used to find in the document the objects that are different.</returns>
            public virtual IDictionary<ObjectPath, string> GetDifferences() {
                return differences;
            }

            /// <summary>Converts this CompareResult into xml form.</summary>
            /// <param name="stream">output stream to which xml report will be written.</param>
            public virtual void WriteReportToXml(Stream stream) {
                var xmlReport = XmlUtils.InitNewXmlDocument();
                var root = xmlReport.CreateElement("report");
                var errors = xmlReport.CreateElement("errors");
                errors.SetAttribute("count", differences.Count.ToString());
                root.AppendChild(errors);
                foreach (var entry in differences) {
                    XmlNode errorNode = xmlReport.CreateElement("error");
                    XmlNode message = xmlReport.CreateElement("message");
                    message.AppendChild(xmlReport.CreateTextNode(entry.Value));
                    XmlNode path = entry.Key.ToXmlNode(xmlReport);
                    errorNode.AppendChild(message);
                    errorNode.AppendChild(path);
                    errors.AppendChild(errorNode);
                }
                xmlReport.AppendChild(root);
                XmlUtils.WriteXmlDocToStream(xmlReport, stream);
            }

            protected internal virtual bool IsMessageLimitReached() {
                return differences.Count >= messageLimit;
            }

            protected internal virtual void AddError(ObjectPath path, string message) {
                if (differences.Count < messageLimit) {
                    differences.Put(((ObjectPath)path.Clone()), message);
                }
            }

            private readonly CompareTool _enclosing;
        }

        /// <summary>
        /// Class that helps to find two corresponding objects in the compared documents and also keeps track of the
        /// already met during comparing process parent indirect objects.
        /// </summary>
        /// <remarks>
        /// Class that helps to find two corresponding objects in the compared documents and also keeps track of the
        /// already met during comparing process parent indirect objects.
        /// <para />
        /// You could say that ObjectPath instance consists of two parts: direct path and indirect path. Direct path defines
        /// path to the currently comparing objects in relation to base objects. It could be empty, which would mean that
        /// currently comparing objects are base objects themselves. Base objects are the two indirect objects from the comparing
        /// documents which are in the same position in the pdf trees. Another part, indirect path, defines which indirect
        /// objects were met during comparison process to get to the current base objects. Indirect path is needed to avoid
        /// infinite loops during comparison.
        /// </remarks>
        public class ObjectPath {
            protected internal PdfIndirectReference baseCmpObject;

            protected internal PdfIndirectReference baseOutObject;

            protected internal Stack<LocalPathItem> path = new Stack<LocalPathItem
                >();

            protected internal Stack<IndirectPathItem> indirects = new Stack<IndirectPathItem
                >();

            /// <summary>Creates empty ObjectPath.</summary>
            public ObjectPath() {
            }

            /// <summary>Creates ObjectPath with corresponding base objects in two documents.</summary>
            /// <param name="baseCmpObject">base object in cmp document.</param>
            /// <param name="baseOutObject">base object in out document.</param>
            public ObjectPath(PdfIndirectReference baseCmpObject, PdfIndirectReference baseOutObject) {
                this.baseCmpObject = baseCmpObject;
                this.baseOutObject = baseOutObject;
                indirects.Push(new IndirectPathItem(this, baseCmpObject, baseOutObject));
            }

            public ObjectPath(PdfIndirectReference baseCmpObject, PdfIndirectReference baseOutObject, Stack<LocalPathItem
                > path, Stack<IndirectPathItem> indirects) {
                this.baseCmpObject = baseCmpObject;
                this.baseOutObject = baseOutObject;
                this.path = path;
                this.indirects = indirects;
            }

            /// <summary>
            /// Creates a new ObjectPath instance with two new given base objects, which are supposed to be nested in the base
            /// objects of the current instance of the ObjectPath.
            /// </summary>
            /// <remarks>
            /// Creates a new ObjectPath instance with two new given base objects, which are supposed to be nested in the base
            /// objects of the current instance of the ObjectPath. This method is used to avoid infinite loop in case of
            /// circular references in pdf documents objects structure.
            /// <para />
            /// Basically, this method creates copy of the current ObjectPath instance, but resets information of the direct
            /// paths, and also adds current ObjectPath instance base objects to the indirect references chain that denotes
            /// a path to the new base objects.
            /// </remarks>
            /// <param name="baseCmpObject">new base object in cmp document.</param>
            /// <param name="baseOutObject">new base object in out document.</param>
            /// <returns>
            /// new ObjectPath instance, which stores chain of the indirect references which were already met to get
            /// to the new base objects.
            /// </returns>
            public virtual ObjectPath ResetDirectPath(PdfIndirectReference baseCmpObject, PdfIndirectReference
                 baseOutObject) {
                var newPath = new ObjectPath(baseCmpObject, baseOutObject, new Stack<LocalPathItem
                    >(), indirects.Clone());
                newPath.indirects.Push(new IndirectPathItem(this, baseCmpObject, baseOutObject));
                return newPath;
            }

            /// <summary>This method is used to define if given objects were already met in the path to the current base objects.
            ///     </summary>
            /// <remarks>
            /// This method is used to define if given objects were already met in the path to the current base objects.
            /// If this method returns true it basically means that we found a loop in the objects structure and that we
            /// already compared these objects.
            /// </remarks>
            /// <param name="cmpObject">cmp object to check if it was already met in base objects path.</param>
            /// <param name="outObject">out object to check if it was already met in base objects path.</param>
            /// <returns>true if given objects are contained in the path and therefore were already compared.</returns>
            public virtual bool IsComparing(PdfIndirectReference cmpObject, PdfIndirectReference outObject) {
                return indirects.Contains(new IndirectPathItem(this, cmpObject, outObject));
            }

            /// <summary>Adds array item to the direct path.</summary>
            /// <remarks>
            /// Adds array item to the direct path. See
            /// <see cref="ArrayPathItem"/>.
            /// </remarks>
            /// <param name="index">index in the array of the direct object to be compared.</param>
            public virtual void PushArrayItemToPath(int index) {
                path.Push(new ArrayPathItem(index));
            }

            /// <summary>Adds dictionary item to the direct path.</summary>
            /// <remarks>
            /// Adds dictionary item to the direct path. See
            /// <see cref="DictPathItem"/>.
            /// </remarks>
            /// <param name="key">key in the dictionary to which corresponds direct object to be compared.</param>
            public virtual void PushDictItemToPath(PdfName key) {
                path.Push(new DictPathItem(key));
            }

            /// <summary>Adds offset item to the direct path.</summary>
            /// <remarks>
            /// Adds offset item to the direct path. See
            /// <see cref="OffsetPathItem"/>.
            /// </remarks>
            /// <param name="offset">offset to the specific byte in the stream that is compared.</param>
            public virtual void PushOffsetToPath(int offset) {
                path.Push(new OffsetPathItem(offset));
            }

            /// <summary>Removes the last path item from the direct path.</summary>
            public virtual void Pop() {
                path.Pop();
            }

            /// <summary>
            /// Gets local (or direct) path that denotes sequence of the path items from base object to the comparing
            /// direct object.
            /// </summary>
            /// <returns>direct path to the comparing object.</returns>
            public virtual Stack<LocalPathItem> GetLocalPath() {
                return path;
            }

            /// <summary>
            /// Gets indirect path which denotes sequence of the indirect references that were passed in comparing process
            /// to get to the current base objects.
            /// </summary>
            /// <returns>indirect path to the current base objects.</returns>
            public virtual Stack<IndirectPathItem> GetIndirectPath() {
                return indirects;
            }

            /// <returns>current base object in the cmp document.</returns>
            public virtual PdfIndirectReference GetBaseCmpObject() {
                return baseCmpObject;
            }

            /// <returns>current base object in the out document.</returns>
            public virtual PdfIndirectReference GetBaseOutObject() {
                return baseOutObject;
            }

            /// <summary>Creates an xml node that describes a direct path stored in this ObjectPath instance.</summary>
            /// <param name="document">xml document, to which this xml node will be added.</param>
            /// <returns>an xml node describing direct path.</returns>
            public virtual XmlElement ToXmlNode(XmlDocument document) {
                var element = document.CreateElement("path");
                var baseNode = document.CreateElement("base");
                baseNode.SetAttribute("cmp", MessageFormatUtil.Format("{0} {1} obj", baseCmpObject.GetObjNumber(), baseCmpObject
                    .GetGenNumber()));
                baseNode.SetAttribute("out", MessageFormatUtil.Format("{0} {1} obj", baseOutObject.GetObjNumber(), baseOutObject
                    .GetGenNumber()));
                element.AppendChild(baseNode);
                var pathClone = path.
	                Clone();
                IList<LocalPathItem> localPathItems = new List<LocalPathItem
                    >(path.Count);
                for (var i = 0; i < path.Count; ++i) {
                    localPathItems.Add(pathClone.Pop());
                }
                for (var i = localPathItems.Count - 1; i >= 0; --i) {
                    element.AppendChild(localPathItems[i].ToXmlNode(document));
                }
                return element;
            }

            /// <returns>string representation of the direct path stored in this ObjectPath instance.</returns>
            public override string ToString() {
                var sb = new StringBuilder();
                sb.Append(MessageFormatUtil.Format("Base cmp object: {0} obj. Base out object: {1} obj", baseCmpObject, baseOutObject
                    ));
                var pathClone = path.
	                Clone();
                IList<LocalPathItem> localPathItems = new List<LocalPathItem
                    >(path.Count);
                for (var i = 0; i < path.Count; ++i) {
                    localPathItems.Add(pathClone.Pop());
                }
                for (var i = localPathItems.Count - 1; i >= 0; --i) {
                    sb.Append("\n");
                    sb.Append(localPathItems[i]);
                }
                return sb.ToString();
            }

            public override int GetHashCode() {
                // TODO: DEVSIX-4756 indirect reference hashCode should use hashCode method of indirect
                //  reference. For now we need to write custom logic as some tests rely on sequential
                //  reopening of the same document which affects with not equal indirect reference
                //  hashCodes (after the update which starts counting the document in indirect reference
                //  hashCode)
                var baseCmpObjectHashCode = 0;
                if (baseCmpObject != null) {
                    baseCmpObjectHashCode = baseCmpObject.GetObjNumber() * 31 + baseCmpObject.GetGenNumber();
                }
                var baseOutObjectHashCode = 0;
                if (baseOutObject != null) {
                    baseOutObjectHashCode = baseOutObject.GetObjNumber() * 31 + baseOutObject.GetGenNumber();
                }
                var hashCode = baseCmpObjectHashCode * 31 + baseOutObjectHashCode;
                foreach (var pathItem in path) {
                    hashCode *= 31;
                    hashCode += pathItem.GetHashCode();
                }
                return hashCode;
            }

            public override bool Equals(object obj) {
                if (this == obj) {
                    return true;
                }
                if (obj == null || GetType() != obj.GetType()) {
                    return false;
                }
                var that = (ObjectPath)obj;
                // TODO: DEVSIX-4756 indirect reference comparing should use equals method of indirect
                //  reference. For now we need to write custom logic as some tests rely on sequential
                //  reopening of the same document which affects with not equal indirect references
                //  (after the update which starts counting the document in indirect reference equality)
                bool isBaseCmpObjectEqual;
                if (baseCmpObject == that.baseCmpObject) {
                    isBaseCmpObjectEqual = true;
                }
                else {
                    if (baseCmpObject == null || that.baseCmpObject == null || baseCmpObject.GetType() != that.baseCmpObject.GetType
                        ()) {
                        isBaseCmpObjectEqual = false;
                    }
                    else {
                        isBaseCmpObjectEqual = baseCmpObject.GetObjNumber() == that.baseCmpObject.GetObjNumber() && baseCmpObject.
                            GetGenNumber() == that.baseCmpObject.GetGenNumber();
                    }
                }
                bool isBaseOutObjectEqual;
                if (baseOutObject == that.baseOutObject) {
                    isBaseOutObjectEqual = true;
                }
                else {
                    if (baseOutObject == null || that.baseOutObject == null || baseOutObject.GetType() != that.baseOutObject.GetType
                        ()) {
                        isBaseOutObjectEqual = false;
                    }
                    else {
                        isBaseOutObjectEqual = baseOutObject.GetObjNumber() == that.baseOutObject.GetObjNumber() && baseOutObject.
                            GetGenNumber() == that.baseOutObject.GetGenNumber();
                    }
                }
                return isBaseCmpObjectEqual && isBaseOutObjectEqual && path.SequenceEqual(((ObjectPath
                    )obj).path);
            }

            protected internal virtual object Clone() {
                return new ObjectPath(baseCmpObject, baseOutObject, path.Clone(), indirects.Clone());
            }

            /// <summary>
            /// An item in the indirect path (see
            /// <see cref="ObjectPath"/>.
            /// </summary>
            /// <remarks>
            /// An item in the indirect path (see
            /// <see cref="ObjectPath"/>
            /// . It encapsulates two corresponding objects from the two
            /// comparing documents that were met to get to the path base objects during comparing process.
            /// </remarks>
            public class IndirectPathItem {
                private PdfIndirectReference cmpObject;

                private PdfIndirectReference outObject;

                /// <summary>Creates IndirectPathItem instance for two corresponding objects from two comparing documents.</summary>
                /// <param name="cmpObject">an object from the cmp document.</param>
                /// <param name="outObject">an object from the out document.</param>
                public IndirectPathItem(ObjectPath _enclosing, PdfIndirectReference cmpObject, PdfIndirectReference outObject
                    ) {
                    this._enclosing = _enclosing;
                    this.cmpObject = cmpObject;
                    this.outObject = outObject;
                }

                /// <returns>an object from the cmp object that was met to get to the path base objects during comparing process.
                ///     </returns>
                public virtual PdfIndirectReference GetCmpObject() {
                    return cmpObject;
                }

                /// <returns>an object from the out object that was met to get to the path base objects during comparing process.
                ///     </returns>
                public virtual PdfIndirectReference GetOutObject() {
                    return outObject;
                }

                public override int GetHashCode() {
                    return cmpObject.GetHashCode() * 31 + outObject.GetHashCode();
                }

                public override bool Equals(object obj) {
                    return (obj.GetType() == GetType() && cmpObject.Equals(((IndirectPathItem
                        )obj).cmpObject) && outObject.Equals(((IndirectPathItem)obj).outObject));
                }

                private readonly ObjectPath _enclosing;
            }

            /// <summary>
            /// An abstract class for the items in the direct path (see
            /// <see cref="ObjectPath"/>.
            /// </summary>
            public abstract class LocalPathItem {
                /// <summary>Creates an xml node that describes this direct path item.</summary>
                /// <param name="document">xml document, to which this xml node will be added.</param>
                /// <returns>an xml node describing direct path item.</returns>
                protected internal abstract XmlElement ToXmlNode(XmlDocument document);
            }

            /// <summary>
            /// Direct path item (see
            /// <see cref="ObjectPath"/>
            /// , which describes transition to the
            /// <see cref="PdfDictionary"/>
            /// entry which value is now a currently comparing direct object.
            /// </summary>
            public class DictPathItem : LocalPathItem {
                internal PdfName key;

                /// <summary>
                /// Creates an instance of the
                /// <see cref="DictPathItem"/>.
                /// </summary>
                /// <param name="key">
                /// the key which defines to which entry of the
                /// <see cref="PdfDictionary"/>
                /// the transition was performed.
                /// </param>
                public DictPathItem(PdfName key) {
                    this.key = key;
                }

                public override string ToString() {
                    return "Dict key: " + key;
                }

                public override int GetHashCode() {
                    return key.GetHashCode();
                }

                public override bool Equals(object obj) {
                    return obj.GetType() == GetType() && key.Equals(((DictPathItem)obj).key);
                }

                /// <summary>
                /// The key which defines to which entry of the
                /// <see cref="PdfDictionary"/>
                /// the transition was performed.
                /// </summary>
                /// <remarks>
                /// The key which defines to which entry of the
                /// <see cref="PdfDictionary"/>
                /// the transition was performed.
                /// See
                /// <see cref="DictPathItem"/>
                /// for more info.
                /// </remarks>
                /// <returns>
                /// a
                /// <see cref="PdfName"/>
                /// which is the key which defines to which entry of the dictionary
                /// the transition was performed.
                /// </returns>
                public virtual PdfName GetKey() {
                    return key;
                }

                protected internal override XmlElement ToXmlNode(XmlDocument document) {
                    var element = document.CreateElement("dictKey");
                    element.AppendChild(document.CreateTextNode(key.ToString()));
                    return element;
                }
            }

            /// <summary>
            /// Direct path item (see
            /// <see cref="ObjectPath"/>
            /// , which describes transition to the
            /// <see cref="PdfArray"/>
            /// element which is now a currently comparing direct object.
            /// </summary>
            public class ArrayPathItem : LocalPathItem {
                internal int index;

                /// <summary>
                /// Creates an instance of the
                /// <see cref="ArrayPathItem"/>.
                /// </summary>
                /// <param name="index">
                /// the index which defines element of the
                /// <see cref="PdfArray"/>
                /// to which
                /// the transition was performed.
                /// </param>
                public ArrayPathItem(int index) {
                    this.index = index;
                }

                public override string ToString() {
                    return "Array index: " + index;
                }

                public override int GetHashCode() {
                    return index;
                }

                public override bool Equals(object obj) {
                    return obj.GetType() == GetType() && index == ((ArrayPathItem)obj).index;
                }

                /// <summary>
                /// The index which defines element of the
                /// <see cref="PdfArray"/>
                /// to which the transition was performed.
                /// </summary>
                /// <remarks>
                /// The index which defines element of the
                /// <see cref="PdfArray"/>
                /// to which the transition was performed.
                /// See
                /// <see cref="ArrayPathItem"/>
                /// for more info.
                /// </remarks>
                /// <returns>the index which defines element of the array to which the transition was performed</returns>
                public virtual int GetIndex() {
                    return index;
                }

                protected internal override XmlElement ToXmlNode(XmlDocument document) {
                    var element = document.CreateElement("arrayIndex");
                    element.AppendChild(document.CreateTextNode(index.ToString()));
                    return element;
                }
            }

            /// <summary>
            /// Direct path item (see
            /// <see cref="ObjectPath"/>
            /// , which describes transition to the
            /// specific position in
            /// <see cref="PdfStream"/>.
            /// </summary>
            public class OffsetPathItem : LocalPathItem {
                internal int offset;

                /// <summary>
                /// Creates an instance of the
                /// <see cref="OffsetPathItem"/>.
                /// </summary>
                /// <param name="offset">
                /// bytes offset to the specific position in
                /// <see cref="PdfStream"/>.
                /// </param>
                public OffsetPathItem(int offset) {
                    this.offset = offset;
                }

                /// <summary>
                /// The bytes offset of the stream which defines specific position in the
                /// <see cref="PdfStream"/>
                /// , to which transition
                /// was performed.
                /// </summary>
                /// <returns>an integer defining bytes offset to the specific position in stream.</returns>
                public virtual int GetOffset() {
                    return offset;
                }

                public override string ToString() {
                    return "Offset: " + offset;
                }

                public override int GetHashCode() {
                    return offset;
                }

                public override bool Equals(object obj) {
                    return obj.GetType() == GetType() && offset == ((OffsetPathItem)obj).offset;
                }

                protected internal override XmlElement ToXmlNode(XmlDocument document) {
                    var element = document.CreateElement("offset");
                    element.AppendChild(document.CreateTextNode(offset.ToString()));
                    return element;
                }
            }
        }

        private class TrailerPath : ObjectPath {
            private PdfDocument outDocument;

            private PdfDocument cmpDocument;

            public TrailerPath(PdfDocument cmpDoc, PdfDocument outDoc) {
                outDocument = outDoc;
                cmpDocument = cmpDoc;
            }

            public TrailerPath(PdfDocument cmpDoc, PdfDocument outDoc, Stack<LocalPathItem> path
                ) {
                outDocument = outDoc;
                cmpDocument = cmpDoc;
                this.path = path;
            }

            public override XmlElement ToXmlNode(XmlDocument document) {
                var element = document.CreateElement("path");
                var baseNode = document.CreateElement("base");
                baseNode.SetAttribute("cmp", "trailer");
                baseNode.SetAttribute("out", "trailer");
                element.AppendChild(baseNode);
                foreach (var pathItem in path) {
                    element.AppendChild(pathItem.ToXmlNode(document));
                }
                return element;
            }

            public override string ToString() {
                var sb = new StringBuilder();
                sb.Append("Base cmp object: trailer. Base out object: trailer");
                foreach (var pathItem in path) {
                    sb.Append("\n");
                    sb.Append(pathItem);
                }
                return sb.ToString();
            }

            public override int GetHashCode() {
                var hashCode = outDocument.GetHashCode() * 31 + cmpDocument.GetHashCode();
                foreach (var pathItem in path) {
                    hashCode *= 31;
                    hashCode += pathItem.GetHashCode();
                }
                return hashCode;
            }

            public override bool Equals(object obj) {
                return obj.GetType() == GetType() && outDocument.Equals(((TrailerPath)obj).outDocument) && cmpDocument
                    .Equals(((TrailerPath)obj).cmpDocument) && path.SequenceEqual(((ObjectPath
                    )obj).path);
            }

            protected internal override object Clone() {
                return new TrailerPath(cmpDocument, outDocument, path.Clone());
            }
        }

        /// <summary>
        /// Exceptions thrown when errors occur during generation and comparison of images obtained on the basis of pdf
        /// files.
        /// </summary>
        public class CompareToolExecutionException : Exception {
            /// <summary>
            /// Creates a new
            /// <see cref="CompareToolExecutionException"/>.
            /// </summary>
            /// <param name="msg">the detail message.</param>
            public CompareToolExecutionException(CompareTool _enclosing, string msg)
                : base(msg) {
                this._enclosing = _enclosing;
            }

            private readonly CompareTool _enclosing;
        }
    }
}
