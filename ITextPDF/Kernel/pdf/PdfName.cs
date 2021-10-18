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
using IText.IO.Source;
using IText.IO.Util;

namespace IText.Kernel.Pdf {
    public class PdfName : PdfPrimitiveObject, IComparable<PdfName> {
        //  ' '
        private static readonly byte[] space = ByteUtils.GetIsoBytes("#20");

        //  '%'
        private static readonly byte[] percent = ByteUtils.GetIsoBytes("#25");

        //  '('
        private static readonly byte[] leftParenthesis = ByteUtils.GetIsoBytes("#28");

        //  ')'
        private static readonly byte[] rightParenthesis = ByteUtils.GetIsoBytes("#29");

        //  '<'
        private static readonly byte[] lessThan = ByteUtils.GetIsoBytes("#3c");

        //  '>'
        private static readonly byte[] greaterThan = ByteUtils.GetIsoBytes("#3e");

        //  '['
        private static readonly byte[] leftSquare = ByteUtils.GetIsoBytes("#5b");

        //  ']'
        private static readonly byte[] rightSquare = ByteUtils.GetIsoBytes("#5d");

        //  '{'
        private static readonly byte[] leftCurlyBracket = ByteUtils.GetIsoBytes("#7b");

        //  '}'
        private static readonly byte[] rightCurlyBracket = ByteUtils.GetIsoBytes("#7d");

        //  '/'
        private static readonly byte[] solidus = ByteUtils.GetIsoBytes("#2f");

        //  '#'
        private static readonly byte[] numberSign = ByteUtils.GetIsoBytes("#23");

        public static readonly PdfName _3D = CreateDirectName("3D");

        public static readonly PdfName _3DA = CreateDirectName("3DA");

        public static readonly PdfName _3DB = CreateDirectName("3DB");

        public static readonly PdfName _3DCrossSection = CreateDirectName("3DCrossSection");

        public static readonly PdfName _3DD = CreateDirectName("3DD");

        public static readonly PdfName _3DI = CreateDirectName("3DI");

        public static readonly PdfName _3DV = CreateDirectName("3DV");

        public static readonly PdfName _3DView = CreateDirectName("3DView");

        public static readonly PdfName a = CreateDirectName("a");

        public static readonly PdfName A = CreateDirectName("A");

        public static readonly PdfName A85 = CreateDirectName("A85");

        public static readonly PdfName AA = CreateDirectName("AA");

        public static readonly PdfName AbsoluteColorimetric = CreateDirectName("AbsoluteColorimetric"
            );

        public static readonly PdfName AcroForm = CreateDirectName("AcroForm");

        public static readonly PdfName Action = CreateDirectName("Action");

        public static readonly PdfName ActualText = CreateDirectName("ActualText");

        public static readonly PdfName ADBE = CreateDirectName("ADBE");

        public static readonly PdfName Adbe_pkcs7_detached = CreateDirectName("adbe.pkcs7.detached"
            );

        public static readonly PdfName Adbe_pkcs7_s4 = CreateDirectName("adbe.pkcs7.s4");

        public static readonly PdfName Adbe_pkcs7_s5 = CreateDirectName("adbe.pkcs7.s5");

        public static readonly PdfName Adbe_pkcs7_sha1 = CreateDirectName("adbe.pkcs7.sha1");

        public static readonly PdfName Adbe_x509_rsa_sha1 = CreateDirectName("adbe.x509.rsa_sha1"
            );

        public static readonly PdfName Adobe_PPKLite = CreateDirectName("Adobe.PPKLite");

        public static readonly PdfName Adobe_PPKMS = CreateDirectName("Adobe.PPKMS");

        public static readonly PdfName Adobe_PubSec = CreateDirectName("Adobe.PubSec");

        public static readonly PdfName AESV2 = CreateDirectName("AESV2");

        public static readonly PdfName AESV3 = CreateDirectName("AESV3");

        public static readonly PdfName AF = CreateDirectName("AF");

        public static readonly PdfName AFRelationship = CreateDirectName("AFRelationship");

        public static readonly PdfName After = CreateDirectName("After");

        public static readonly PdfName AHx = CreateDirectName("AHx");

        public static readonly PdfName AIS = CreateDirectName("AIS");

        public static readonly PdfName Alaw = CreateDirectName("ALaw");

        public static readonly PdfName All = CreateDirectName("All");

        public static readonly PdfName AllOff = CreateDirectName("AllOff");

        public static readonly PdfName AllOn = CreateDirectName("AllOn");

        public static readonly PdfName Alt = CreateDirectName("Alt");

        public static readonly PdfName Alternate = CreateDirectName("Alternate");

        public static readonly PdfName Alternates = CreateDirectName("Alternates");

        public static readonly PdfName AlternatePresentations = CreateDirectName("AlternatePresentations"
            );

        public static readonly PdfName Alternative = CreateDirectName("Alternative");

        public static readonly PdfName AN = CreateDirectName("AN");

        public static readonly PdfName And = CreateDirectName("And");

        public static readonly PdfName Annot = CreateDirectName("Annot");

        public static readonly PdfName Annots = CreateDirectName("Annots");

        public static readonly PdfName Annotation = CreateDirectName("Annotation");

        public static readonly PdfName AnnotStates = CreateDirectName("AnnotStates");

        public static readonly PdfName AnyOff = CreateDirectName("AnyOff");

        public static readonly PdfName AnyOn = CreateDirectName("AnyOn");

        public static readonly PdfName AP = CreateDirectName("AP");

        public static readonly PdfName App = CreateDirectName("App");

        public static readonly PdfName AppDefault = CreateDirectName("AppDefault");

        public static readonly PdfName ApplicationOctetStream = CreateDirectName("application/octet-stream"
            );

        public static readonly PdfName ApplicationPdf = CreateDirectName("application/pdf");

        public static readonly PdfName ApplicationXml = CreateDirectName("application/xml");

        public static readonly PdfName Approved = CreateDirectName("Approved");

        public static readonly PdfName Art = CreateDirectName("Art");

        public static readonly PdfName ArtBox = CreateDirectName("ArtBox");

        public static readonly PdfName Artifact = CreateDirectName("Artifact");

        public static readonly PdfName AS = CreateDirectName("AS");

        public static readonly PdfName Ascent = CreateDirectName("Ascent");

        public static readonly PdfName ASCII85Decode = CreateDirectName("ASCII85Decode");

        public static readonly PdfName ASCIIHexDecode = CreateDirectName("ASCIIHexDecode");

        public static readonly PdfName Aside = CreateDirectName("Aside");

        public static readonly PdfName AsIs = CreateDirectName("AsIs");

        public static readonly PdfName AuthEvent = CreateDirectName("AuthEvent");

        public static readonly PdfName Author = CreateDirectName("Author");

        public static readonly PdfName B = CreateDirectName("B");

        public static readonly PdfName BackgroundColor = CreateDirectName("BackgroundColor");

        public static readonly PdfName BaseFont = CreateDirectName("BaseFont");

        public static readonly PdfName BaseEncoding = CreateDirectName("BaseEncoding");

        public static readonly PdfName BaselineShift = CreateDirectName("BaselineShift");

        public static readonly PdfName BaseState = CreateDirectName("BaseState");

        public static readonly PdfName BaseVersion = CreateDirectName("BaseVersion");

        public static readonly PdfName Bates = CreateDirectName("Bates");

        public static readonly PdfName BBox = CreateDirectName("BBox");

        public static readonly PdfName BE = CreateDirectName("BE");

        public static readonly PdfName Before = CreateDirectName("Before");

        public static readonly PdfName BC = CreateDirectName("BC");

        public static readonly PdfName BG = CreateDirectName("BG");

        public static readonly PdfName BG2 = CreateDirectName("BG2");

        public static readonly PdfName BibEntry = CreateDirectName("BibEntry");

        public static readonly PdfName BitsPerComponent = CreateDirectName("BitsPerComponent");

        public static readonly PdfName BitsPerCoordinate = CreateDirectName("BitsPerCoordinate");

        public static readonly PdfName BitsPerFlag = CreateDirectName("BitsPerFlag");

        public static readonly PdfName BitsPerSample = CreateDirectName("BitsPerSample");

        public static readonly PdfName Bl = CreateDirectName("Bl");

        public static readonly PdfName BlackIs1 = CreateDirectName("BlackIs1");

        public static readonly PdfName BlackPoint = CreateDirectName("BlackPoint");

        public static readonly PdfName BleedBox = CreateDirectName("BleedBox");

        public static readonly PdfName Block = CreateDirectName("Block");

        public static readonly PdfName BlockAlign = CreateDirectName("BlockAlign");

        public static readonly PdfName BlockQuote = CreateDirectName("BlockQuote");

        public static readonly PdfName BM = CreateDirectName("BM");

        public static readonly PdfName Book = CreateDirectName("Book");

        public static readonly PdfName Border = CreateDirectName("Border");

        public static readonly PdfName BorderColor = CreateDirectName("BorderColor");

        public static readonly PdfName BorderStyle = CreateDirectName("BorderStyle");

        public static readonly PdfName BorderThickness = CreateDirectName("BorderThickness");

        public static readonly PdfName Both = CreateDirectName("Both");

        public static readonly PdfName Bounds = CreateDirectName("Bounds");

        public static readonly PdfName BS = CreateDirectName("BS");

        public static readonly PdfName Btn = CreateDirectName("Btn");

        public static readonly PdfName Butt = CreateDirectName("Butt");

        public static readonly PdfName ByteRange = CreateDirectName("ByteRange");

        public static readonly PdfName C = CreateDirectName("C");

        public static readonly PdfName C0 = CreateDirectName("C0");

        public static readonly PdfName C1 = CreateDirectName("C1");

        public static readonly PdfName CA = CreateDirectName("CA");

        public static readonly PdfName ca = CreateDirectName("ca");

        public static readonly PdfName CalGray = CreateDirectName("CalGray");

        public static readonly PdfName CalRGB = CreateDirectName("CalRGB");

        public static readonly PdfName CapHeight = CreateDirectName("CapHeight");

        public static readonly PdfName Cap = CreateDirectName("Cap");

        public static readonly PdfName Caption = CreateDirectName("Caption");

        public static readonly PdfName Caret = CreateDirectName("Caret");

        public static readonly PdfName Catalog = CreateDirectName("Catalog");

        public static readonly PdfName Category = CreateDirectName("Category");

        public static readonly PdfName CCITTFaxDecode = CreateDirectName("CCITTFaxDecode");

        public static readonly PdfName Center = CreateDirectName("Center");

        public static readonly PdfName CenterWindow = CreateDirectName("CenterWindow");

        public static readonly PdfName Cert = CreateDirectName("Cert");

        public static readonly PdfName Certs = CreateDirectName("Certs");

        public static readonly PdfName CF = CreateDirectName("CF");

        public static readonly PdfName CFM = CreateDirectName("CFM");

        public static readonly PdfName Ch = CreateDirectName("Ch");

        public static readonly PdfName CI = CreateDirectName("CI");

        public static readonly PdfName CIDFontType0 = CreateDirectName("CIDFontType0");

        public static readonly PdfName CIDFontType2 = CreateDirectName("CIDFontType2");

        public static readonly PdfName CIDSet = CreateDirectName("CIDSet");

        public static readonly PdfName CIDSystemInfo = CreateDirectName("CIDSystemInfo");

        public static readonly PdfName CIDToGIDMap = CreateDirectName("CIDToGIDMap");

        public static readonly PdfName Circle = CreateDirectName("Circle");

        public static readonly PdfName CL = CreateDirectName("CL");

        public static readonly PdfName ClosedArrow = CreateDirectName("ClosedArrow");

        public static readonly PdfName CMapName = CreateDirectName("CMapName");

        public static readonly PdfName CO = CreateDirectName("CO");

        public static readonly PdfName Code = CreateDirectName("Code");

        public static readonly PdfName Collection = CreateDirectName("Collection");

        public static readonly PdfName ColSpan = CreateDirectName("ColSpan");

        public static readonly PdfName ColumnCount = CreateDirectName("ColumnCount");

        public static readonly PdfName ColumnGap = CreateDirectName("ColumnGap");

        public static readonly PdfName ColumnWidths = CreateDirectName("ColumnWidths");

        public static readonly PdfName ContactInfo = CreateDirectName("ContactInfo");

        public static readonly PdfName CharProcs = CreateDirectName("CharProcs");

        public static readonly PdfName Color = CreateDirectName("Color");

        public static readonly PdfName ColorBurn = CreateDirectName("ColorBurn");

        public static readonly PdfName ColorDodge = CreateDirectName("ColorDodge");

        public static readonly PdfName Colorants = CreateDirectName("Colorants");

        public static readonly PdfName Colors = CreateDirectName("Colors");

        public static readonly PdfName ColorSpace = CreateDirectName("ColorSpace");

        public static readonly PdfName ColorTransform = CreateDirectName("ColorTransform");

        public static readonly PdfName Column = CreateDirectName("Column");

        public static readonly PdfName Columns = CreateDirectName("Columns");

        public static readonly PdfName Compatible = CreateDirectName("Compatible");

        public static readonly PdfName Confidential = CreateDirectName("Confidential");

        public static readonly PdfName Configs = CreateDirectName("Configs");

        public static readonly PdfName Contents = CreateDirectName("Contents");

        public static readonly PdfName Coords = CreateDirectName("Coords");

        public static readonly PdfName Count = CreateDirectName("Count");

        public static readonly PdfName CP = CreateDirectName("CP");

        public static readonly PdfName CRL = CreateDirectName("CRL");

        public static readonly PdfName CRLs = CreateDirectName("CRLs");

        public static readonly PdfName CreationDate = CreateDirectName("CreationDate");

        public static readonly PdfName Creator = CreateDirectName("Creator");

        public static readonly PdfName CreatorInfo = CreateDirectName("CreatorInfo");

        public static readonly PdfName CropBox = CreateDirectName("CropBox");

        public static readonly PdfName Crypt = CreateDirectName("Crypt");

        public static readonly PdfName CS = CreateDirectName("CS");

        public static readonly PdfName CT = CreateDirectName("CT");

        public static readonly PdfName D = CreateDirectName("D");

        public static readonly PdfName DA = CreateDirectName("DA");

        public static readonly PdfName Darken = CreateDirectName("Darken");

        public static readonly PdfName Dashed = CreateDirectName("Dashed");

        public static readonly PdfName Data = CreateDirectName("Data");

        public static readonly PdfName DCTDecode = CreateDirectName("DCTDecode");

        public static readonly PdfName Decimal = CreateDirectName("Decimal");

        public static readonly PdfName Decode = CreateDirectName("Decode");

        public static readonly PdfName DecodeParms = CreateDirectName("DecodeParms");

        public static readonly PdfName Default = CreateDirectName("Default");

        public static readonly PdfName DefaultCMYK = CreateDirectName("DefaultCMYK");

        public static readonly PdfName DefaultCryptFilter = CreateDirectName("DefaultCryptFilter"
            );

        public static readonly PdfName DefaultGray = CreateDirectName("DefaultGray");

        public static readonly PdfName DefaultRGB = CreateDirectName("DefaultRGB");

        public static readonly PdfName Departmental = CreateDirectName("Departmental");

        public static readonly PdfName DescendantFonts = CreateDirectName("DescendantFonts");

        public static readonly PdfName Desc = CreateDirectName("Desc");

        public static readonly PdfName Descent = CreateDirectName("Descent");

        public static readonly PdfName Design = CreateDirectName("Design");

        public static readonly PdfName Dest = CreateDirectName("Dest");

        public static readonly PdfName DestOutputProfile = CreateDirectName("DestOutputProfile");

        public static readonly PdfName Dests = CreateDirectName("Dests");

        public static readonly PdfName DeviceCMY = CreateDirectName("DeviceCMY");

        public static readonly PdfName DeviceCMYK = CreateDirectName("DeviceCMYK");

        public static readonly PdfName DeviceGray = CreateDirectName("DeviceGray");

        public static readonly PdfName DeviceN = CreateDirectName("DeviceN");

        public static readonly PdfName DeviceRGB = CreateDirectName("DeviceRGB");

        public static readonly PdfName DeviceRGBK = CreateDirectName("DeviceRGBK");

        public static readonly PdfName Diamond = CreateDirectName("Diamond");

        public static readonly PdfName Difference = CreateDirectName("Difference");

        public static readonly PdfName Differences = CreateDirectName("Differences");

        public static readonly PdfName Div = CreateDirectName("Div");

        public static readonly PdfName DigestLocation = CreateDirectName("DigestLocation");

        public static readonly PdfName DigestMethod = CreateDirectName("DigestMethod");

        public static readonly PdfName DigestValue = CreateDirectName("DigestValue");

        public static readonly PdfName Direction = CreateDirectName("Direction");

        public static readonly PdfName Disc = CreateDirectName("Disc");

        public static readonly PdfName DisplayDocTitle = CreateDirectName("DisplayDocTitle");

        public static readonly PdfName DocMDP = CreateDirectName("DocMDP");

        public static readonly PdfName DocOpen = CreateDirectName("DocOpen");

        public static readonly PdfName DocTimeStamp = CreateDirectName("DocTimeStamp");

        public static readonly PdfName Document = CreateDirectName("Document");

        public static readonly PdfName DocumentFragment = CreateDirectName("DocumentFragment");

        public static readonly PdfName Domain = CreateDirectName("Domain");

        public static readonly PdfName Dotted = CreateDirectName("Dotted");

        public static readonly PdfName Double = CreateDirectName("Double");

        public static readonly PdfName DP = CreateDirectName("DP");

        public static readonly PdfName Dp = CreateDirectName("Dp");

        public static readonly PdfName DPart = CreateDirectName("DPart");

        public static readonly PdfName DR = CreateDirectName("DR");

        public static readonly PdfName Draft = CreateDirectName("Draft");

        public static readonly PdfName DS = CreateDirectName("DS");

        public static readonly PdfName DSS = CreateDirectName("DSS");

        public static readonly PdfName Duplex = CreateDirectName("Duplex");

        public static readonly PdfName DuplexFlipShortEdge = CreateDirectName("DuplexFlipShortEdge"
            );

        public static readonly PdfName DuplexFlipLongEdge = CreateDirectName("DuplexFlipLongEdge"
            );

        public static readonly PdfName DV = CreateDirectName("DV");

        public static readonly PdfName DW = CreateDirectName("DW");

        public static readonly PdfName E = CreateDirectName("E");

        public static readonly PdfName EF = CreateDirectName("EF");

        public static readonly PdfName EFF = CreateDirectName("EFF");

        public static readonly PdfName EFOpen = CreateDirectName("EFOpen");

        public static readonly PdfName Em = CreateDirectName("Em");

        public static readonly PdfName EmbeddedFile = CreateDirectName("EmbeddedFile");

        public static readonly PdfName EmbeddedFiles = CreateDirectName("EmbeddedFiles");

        public static readonly PdfName Encode = CreateDirectName("Encode");

        public static readonly PdfName EncodedByteAlign = CreateDirectName("EncodedByteAlign");

        public static readonly PdfName Encoding = CreateDirectName("Encoding");

        public static readonly PdfName Encrypt = CreateDirectName("Encrypt");

        public static readonly PdfName EncryptMetadata = CreateDirectName("EncryptMetadata");

        public static readonly PdfName EncryptedPayload = CreateDirectName("EncryptedPayload");

        public static readonly PdfName End = CreateDirectName("End");

        public static readonly PdfName EndIndent = CreateDirectName("EndIndent");

        public static readonly PdfName EndOfBlock = CreateDirectName("EndOfBlock");

        public static readonly PdfName EndOfLine = CreateDirectName("EndOfLine");

        public static readonly PdfName Enforce = CreateDirectName("Enforce");

        public static readonly PdfName EP = CreateDirectName("EP");

        public static readonly PdfName ESIC = CreateDirectName("ESIC");

        public static readonly PdfName ETSI_CAdES_DETACHED = CreateDirectName("ETSI.CAdES.detached"
            );

        public static readonly PdfName ETSI_RFC3161 = CreateDirectName("ETSI.RFC3161");

        public static readonly PdfName Event = CreateDirectName("Event");

        public static readonly PdfName Exclude = CreateDirectName("Exclude");

        public static readonly PdfName Exclusion = CreateDirectName("Exclusion");

        public static readonly PdfName ExData = CreateDirectName("ExData");

        public static readonly PdfName Experimental = CreateDirectName("Experimental");

        public static readonly PdfName Expired = CreateDirectName("Expired");

        public static readonly PdfName Export = CreateDirectName("Export");

        public static readonly PdfName ExportState = CreateDirectName("ExportState");

        public static readonly PdfName Extend = CreateDirectName("Extend");

        public static readonly PdfName Extends = CreateDirectName("Extends");

        public static readonly PdfName Extensions = CreateDirectName("Extensions");

        public static readonly PdfName ExtensionLevel = CreateDirectName("ExtensionLevel");

        public static readonly PdfName ExtGState = CreateDirectName("ExtGState");

        public static readonly PdfName F = CreateDirectName("F");

        public static readonly PdfName False = CreateDirectName("false");

        public static readonly PdfName Ff = CreateDirectName("Ff");

        public static readonly PdfName FieldMDP = CreateDirectName("FieldMDP");

        public static readonly PdfName Fields = CreateDirectName("Fields");

        public static readonly PdfName Figure = CreateDirectName("Figure");

        public static readonly PdfName FileAttachment = CreateDirectName("FileAttachment");

        public static readonly PdfName Filespec = CreateDirectName("Filespec");

        public static readonly PdfName Filter = CreateDirectName("Filter");

        public static readonly PdfName FFilter = CreateDirectName("FFilter");

        public static readonly PdfName FDecodeParams = CreateDirectName("FDecodeParams");

        public static readonly PdfName FENote = CreateDirectName("FENote");

        public static readonly PdfName Final = CreateDirectName("Final");

        public static readonly PdfName First = CreateDirectName("First");

        public static readonly PdfName FirstChar = CreateDirectName("FirstChar");

        public static readonly PdfName FirstPage = CreateDirectName("FirstPage");

        public static readonly PdfName Fit = CreateDirectName("Fit");

        public static readonly PdfName FitB = CreateDirectName("FitB");

        public static readonly PdfName FitBH = CreateDirectName("FitBH");

        public static readonly PdfName FitBV = CreateDirectName("FitBV");

        public static readonly PdfName FitH = CreateDirectName("FitH");

        public static readonly PdfName FitR = CreateDirectName("FitR");

        public static readonly PdfName FitV = CreateDirectName("FitV");

        public static readonly PdfName FitWindow = CreateDirectName("FitWindow");

        public static readonly PdfName FixedPrint = CreateDirectName("FixedPrint");

        /// <summary>PdfName for the abbreviation of FlateDecode.</summary>
        /// <remarks>
        /// PdfName for the abbreviation of FlateDecode. For the Flatness Tolerance PdfName use
        /// <see cref="FL"/>
        /// (Uppercase 'L')
        /// </remarks>
        public static readonly PdfName Fl = CreateDirectName("Fl");

        /// <summary>PdfName for Flatness Tolerance.</summary>
        /// <remarks>
        /// PdfName for Flatness Tolerance. For the PdfName with the FlateDecode abbreviation use
        /// <see cref="Fl"/>
        /// (Lowercase 'L')
        /// </remarks>
        public static readonly PdfName FL = CreateDirectName("FL");

        public static readonly PdfName Flags = CreateDirectName("Flags");

        public static readonly PdfName FlateDecode = CreateDirectName("FlateDecode");

        public static readonly PdfName Fo = CreateDirectName("Fo");

        public static readonly PdfName Font = CreateDirectName("Font");

        public static readonly PdfName FontBBox = CreateDirectName("FontBBox");

        public static readonly PdfName FontDescriptor = CreateDirectName("FontDescriptor");

        public static readonly PdfName FontFamily = CreateDirectName("FontFamily");

        public static readonly PdfName FontFauxing = CreateDirectName("FontFauxing");

        public static readonly PdfName FontFile = CreateDirectName("FontFile");

        public static readonly PdfName FontFile2 = CreateDirectName("FontFile2");

        public static readonly PdfName FontFile3 = CreateDirectName("FontFile3");

        public static readonly PdfName FontMatrix = CreateDirectName("FontMatrix");

        public static readonly PdfName FontName = CreateDirectName("FontName");

        public static readonly PdfName FontWeight = CreateDirectName("FontWeight");

        public static readonly PdfName FontStretch = CreateDirectName("FontStretch");

        public static readonly PdfName Footer = CreateDirectName("Footer");

        public static readonly PdfName ForComment = CreateDirectName("ForComment");

        public static readonly PdfName Form = CreateDirectName("Form");

        public static readonly PdfName FormData = CreateDirectName("FormData");

        public static readonly PdfName ForPublicRelease = CreateDirectName("ForPublicRelease");

        public static readonly PdfName FormType = CreateDirectName("FormType");

        public static readonly PdfName FreeText = CreateDirectName("FreeText");

        public static readonly PdfName FreeTextCallout = CreateDirectName("FreeTextCallout");

        public static readonly PdfName FreeTextTypeWriter = CreateDirectName("FreeTextTypeWriter"
            );

        public static readonly PdfName FS = CreateDirectName("FS");

        public static readonly PdfName Formula = CreateDirectName("Formula");

        public static readonly PdfName FT = CreateDirectName("FT");

        public static readonly PdfName FullScreen = CreateDirectName("FullScreen");

        public static readonly PdfName Function = CreateDirectName("Function");

        public static readonly PdfName Functions = CreateDirectName("Functions");

        public static readonly PdfName FunctionType = CreateDirectName("FunctionType");

        public static readonly PdfName Gamma = CreateDirectName("Gamma");

        public static readonly PdfName GlyphOrientationVertical = CreateDirectName("GlyphOrientationVertical"
            );

        public static readonly PdfName GoTo = CreateDirectName("GoTo");

        public static readonly PdfName GoTo3DView = CreateDirectName("GoTo3DView");

        public static readonly PdfName GoToDp = CreateDirectName("GoToDp");

        public static readonly PdfName GoToE = CreateDirectName("GoToE");

        public static readonly PdfName GoToR = CreateDirectName("GoToR");

        public static readonly PdfName Graph = CreateDirectName("Graph");

        public static readonly PdfName Group = CreateDirectName("Group");

        public static readonly PdfName Groove = CreateDirectName("Groove");

        public static readonly PdfName GTS_PDFA1 = CreateDirectName("GTS_PDFA1");

        public static readonly PdfName H = CreateDirectName("H");

        public static readonly PdfName H1 = CreateDirectName("H1");

        public static readonly PdfName H2 = CreateDirectName("H2");

        public static readonly PdfName H3 = CreateDirectName("H3");

        public static readonly PdfName H4 = CreateDirectName("H4");

        public static readonly PdfName H5 = CreateDirectName("H5");

        public static readonly PdfName H6 = CreateDirectName("H6");

        public static readonly PdfName HalftoneType = CreateDirectName("HalftoneType");

        public static readonly PdfName HalftoneName = CreateDirectName("HalftoneName");

        public static readonly PdfName HardLight = CreateDirectName("HardLight");

        public static readonly PdfName Header = CreateDirectName("Header");

        public static readonly PdfName Headers = CreateDirectName("Headers");

        public static readonly PdfName Height = CreateDirectName("Height");

        public static readonly PdfName Hide = CreateDirectName("Hide");

        public static readonly PdfName Hidden = CreateDirectName("Hidden");

        public static readonly PdfName HideMenubar = CreateDirectName("HideMenubar");

        public static readonly PdfName HideToolbar = CreateDirectName("HideToolbar");

        public static readonly PdfName HideWindowUI = CreateDirectName("HideWindowUI");

        public static readonly PdfName Highlight = CreateDirectName("Highlight");

        public static readonly PdfName HT = CreateDirectName("HT");

        public static readonly PdfName HTO = CreateDirectName("HTO");

        public static readonly PdfName HTP = CreateDirectName("HTP");

        public static readonly PdfName Hue = CreateDirectName("Hue");

        public static readonly PdfName I = CreateDirectName("I");

        public static readonly PdfName IC = CreateDirectName("IC");

        public static readonly PdfName ICCBased = CreateDirectName("ICCBased");

        public static readonly PdfName ID = CreateDirectName("ID");

        public static readonly PdfName IDS = CreateDirectName("IDS");

        public static readonly PdfName Identity = CreateDirectName("Identity");

        public static readonly PdfName IdentityH = CreateDirectName("Identity-H");

        public static readonly PdfName Inset = CreateDirectName("Inset");

        public static readonly PdfName Image = CreateDirectName("Image");

        public static readonly PdfName ImageMask = CreateDirectName("ImageMask");

        public static readonly PdfName ImportData = CreateDirectName("ImportData");

        public static readonly PdfName ipa = CreateDirectName("ipa");

        public static readonly PdfName Include = CreateDirectName("Include");

        public static readonly PdfName Index = CreateDirectName("Index");

        public static readonly PdfName Indexed = CreateDirectName("Indexed");

        public static readonly PdfName Info = CreateDirectName("Info");

        public static readonly PdfName Inline = CreateDirectName("Inline");

        public static readonly PdfName InlineAlign = CreateDirectName("InlineAlign");

        public static readonly PdfName Ink = CreateDirectName("Ink");

        public static readonly PdfName InkList = CreateDirectName("InkList");

        public static readonly PdfName Intent = CreateDirectName("Intent");

        public static readonly PdfName Interpolate = CreateDirectName("Interpolate");

        public static readonly PdfName IRT = CreateDirectName("IRT");

        public static readonly PdfName IsMap = CreateDirectName("IsMap");

        public static readonly PdfName ItalicAngle = CreateDirectName("ItalicAngle");

        public static readonly PdfName IT = CreateDirectName("IT");

        public static readonly PdfName JavaScript = CreateDirectName("JavaScript");

        public static readonly PdfName JBIG2Decode = CreateDirectName("JBIG2Decode");

        public static readonly PdfName JBIG2Globals = CreateDirectName("JBIG2Globals");

        public static readonly PdfName JPXDecode = CreateDirectName("JPXDecode");

        public static readonly PdfName JS = CreateDirectName("JS");

        public static readonly PdfName Justify = CreateDirectName("Justify");

        public static readonly PdfName K = CreateDirectName("K");

        public static readonly PdfName Keywords = CreateDirectName("Keywords");

        public static readonly PdfName Kids = CreateDirectName("Kids");

        public static readonly PdfName L2R = CreateDirectName("L2R");

        public static readonly PdfName L = CreateDirectName("L");

        public static readonly PdfName Lab = CreateDirectName("Lab");

        public static readonly PdfName Lang = CreateDirectName("Lang");

        public static readonly PdfName Language = CreateDirectName("Language");

        public static readonly PdfName Last = CreateDirectName("Last");

        public static readonly PdfName LastChar = CreateDirectName("LastChar");

        public static readonly PdfName LastModified = CreateDirectName("LastModified");

        public static readonly PdfName LastPage = CreateDirectName("LastPage");

        public static readonly PdfName Launch = CreateDirectName("Launch");

        public static readonly PdfName Layout = CreateDirectName("Layout");

        public static readonly PdfName Lbl = CreateDirectName("Lbl");

        public static readonly PdfName LBody = CreateDirectName("LBody");

        public static readonly PdfName LC = CreateDirectName("LC");

        public static readonly PdfName Leading = CreateDirectName("Leading");

        public static readonly PdfName LE = CreateDirectName("LE");

        public static readonly PdfName Length = CreateDirectName("Length");

        public static readonly PdfName Length1 = CreateDirectName("Length1");

        public static readonly PdfName LI = CreateDirectName("LI");

        public static readonly PdfName Lighten = CreateDirectName("Lighten");

        public static readonly PdfName Limits = CreateDirectName("Limits");

        public static readonly PdfName Line = CreateDirectName("Line");

        public static readonly PdfName LineArrow = CreateDirectName("LineArrow");

        public static readonly PdfName LineHeight = CreateDirectName("LineHeight");

        public static readonly PdfName LineNum = CreateDirectName("LineNum");

        public static readonly PdfName LineThrough = CreateDirectName("LineThrough");

        public static readonly PdfName Link = CreateDirectName("Link");

        public static readonly PdfName List = CreateDirectName("List");

        public static readonly PdfName ListMode = CreateDirectName("ListMode");

        public static readonly PdfName ListNumbering = CreateDirectName("ListNumbering");

        public static readonly PdfName LJ = CreateDirectName("LJ");

        public static readonly PdfName LL = CreateDirectName("LL");

        public static readonly PdfName LLE = CreateDirectName("LLE");

        public static readonly PdfName LLO = CreateDirectName("LLO");

        public static readonly PdfName Lock = CreateDirectName("Lock");

        public static readonly PdfName Locked = CreateDirectName("Locked");

        public static readonly PdfName Location = CreateDirectName("Location");

        public static readonly PdfName LowerAlpha = CreateDirectName("LowerAlpha");

        public static readonly PdfName LowerRoman = CreateDirectName("LowerRoman");

        public static readonly PdfName Luminosity = CreateDirectName("Luminosity");

        public static readonly PdfName LW = CreateDirectName("LW");

        public static readonly PdfName LZWDecode = CreateDirectName("LZWDecode");

        public static readonly PdfName M = CreateDirectName("M");

        public static readonly PdfName MacExpertEncoding = CreateDirectName("MacExpertEncoding");

        public static readonly PdfName MacRomanEncoding = CreateDirectName("MacRomanEncoding");

        public static readonly PdfName Marked = CreateDirectName("Marked");

        public static readonly PdfName MarkInfo = CreateDirectName("MarkInfo");

        public static readonly PdfName Markup = CreateDirectName("Markup");

        public static readonly PdfName Markup3D = CreateDirectName("Markup3D");

        public static readonly PdfName MarkStyle = CreateDirectName("MarkStyle");

        public static readonly PdfName Mask = CreateDirectName("Mask");

        public static readonly PdfName Matrix = CreateDirectName("Matrix");

        public static readonly PdfName max = CreateDirectName("max");

        public static readonly PdfName MaxLen = CreateDirectName("MaxLen");

        public static readonly PdfName MCD = CreateDirectName("MCD");

        public static readonly PdfName MCID = CreateDirectName("MCID");

        public static readonly PdfName MCR = CreateDirectName("MCR");

        public static readonly PdfName MD5 = CreateDirectName("MD5");

        public static readonly PdfName Measure = CreateDirectName("Measure");

        public static readonly PdfName MediaBox = CreateDirectName("MediaBox");

        public static readonly PdfName MediaClip = CreateDirectName("MediaClip");

        public static readonly PdfName Metadata = CreateDirectName("Metadata");

        public static readonly PdfName Middle = CreateDirectName("Middle");

        public static readonly PdfName min = CreateDirectName("min");

        public static readonly PdfName Mix = CreateDirectName("Mix");

        public static readonly PdfName MissingWidth = CreateDirectName("MissingWidth");

        public static readonly PdfName MK = CreateDirectName("MK");

        public static readonly PdfName ML = CreateDirectName("ML");

        public static readonly PdfName MMType1 = CreateDirectName("MMType1");

        public static readonly PdfName MN = CreateDirectName("ML");

        public static readonly PdfName ModDate = CreateDirectName("ModDate");

        public static readonly PdfName Movie = CreateDirectName("Movie");

        public static readonly PdfName MR = CreateDirectName("MR");

        public static readonly PdfName MuLaw = CreateDirectName("muLaw");

        public static readonly PdfName Multiply = CreateDirectName("Multiply");

        public static readonly PdfName N = CreateDirectName("N");

        public static readonly PdfName NA = CreateDirectName("NA");

        public static readonly PdfName Name = CreateDirectName("Name");

        public static readonly PdfName Named = CreateDirectName("Named");

        public static readonly PdfName Names = CreateDirectName("Names");

        public static readonly PdfName Namespace = CreateDirectName("Namespace");

        public static readonly PdfName Namespaces = CreateDirectName("Namespaces");

        public static readonly PdfName NeedAppearances = CreateDirectName("NeedAppearances");

        public static readonly PdfName NeedsRendering = CreateDirectName("NeedsRendering");

        public static readonly PdfName NewWindow = CreateDirectName("NewWindow");

        public static readonly PdfName Next = CreateDirectName("Next");

        public static readonly PdfName NextPage = CreateDirectName("NextPage");

        public static readonly PdfName NM = CreateDirectName("NM");

        public static readonly PdfName NonFullScreenPageMode = CreateDirectName("NonFullScreenPageMode"
            );

        public static readonly PdfName None = CreateDirectName("None");

        public static readonly PdfName NonStruct = CreateDirectName("NonStruct");

        public static readonly PdfName NoOp = CreateDirectName("NoOp");

        public static readonly PdfName Normal = CreateDirectName("Normal");

        public static readonly PdfName Not = CreateDirectName("Not");

        public static readonly PdfName NotApproved = CreateDirectName("NotApproved");

        public static readonly PdfName Note = CreateDirectName("Note");

        public static readonly PdfName NotForPublicRelease = CreateDirectName("NotForPublicRelease"
            );

        public static readonly PdfName NS = CreateDirectName("NS");

        public static readonly PdfName NSO = CreateDirectName("NSO");

        public static readonly PdfName NumCopies = CreateDirectName("NumCopies");

        public static readonly PdfName Nums = CreateDirectName("Nums");

        public static readonly PdfName O = CreateDirectName("O");

        public static readonly PdfName Obj = CreateDirectName("Obj");

        public static readonly PdfName OBJR = CreateDirectName("OBJR");

        public static readonly PdfName ObjStm = CreateDirectName("ObjStm");

        public static readonly PdfName OC = CreateDirectName("OC");

        public static readonly PdfName OCG = CreateDirectName("OCG");

        public static readonly PdfName OCGs = CreateDirectName("OCGs");

        public static readonly PdfName OCMD = CreateDirectName("OCMD");

        public static readonly PdfName OCProperties = CreateDirectName("OCProperties");

        public static readonly PdfName OCSP = CreateDirectName("OCSP");

        public static readonly PdfName OCSPs = CreateDirectName("OCSPs");

        public static readonly PdfName OE = CreateDirectName("OE");

        public static readonly PdfName OFF = CreateDirectName("OFF");

        public static readonly PdfName ON = CreateDirectName("ON");

        public static readonly PdfName OneColumn = CreateDirectName("OneColumn");

        public static readonly PdfName OP = CreateDirectName("OP");

        public static readonly PdfName op = CreateDirectName("op");

        public static readonly PdfName Open = CreateDirectName("Open");

        public static readonly PdfName OpenAction = CreateDirectName("OpenAction");

        public static readonly PdfName OpenArrow = CreateDirectName("OpenArrow");

        public static readonly PdfName Operation = CreateDirectName("Operation");

        public static readonly PdfName OPI = CreateDirectName("OPI");

        public static readonly PdfName OPM = CreateDirectName("OPM");

        public static readonly PdfName Opt = CreateDirectName("Opt");

        public static readonly PdfName Or = CreateDirectName("Or");

        public static readonly PdfName Order = CreateDirectName("Order");

        public static readonly PdfName Ordered = CreateDirectName("Ordered");

        public static readonly PdfName Ordering = CreateDirectName("Ordering");

        public static readonly PdfName Outlines = CreateDirectName("Outlines");

        public static readonly PdfName OutputCondition = CreateDirectName("OutputCondition");

        public static readonly PdfName OutputConditionIdentifier = CreateDirectName("OutputConditionIdentifier"
            );

        public static readonly PdfName OutputIntent = CreateDirectName("OutputIntent");

        public static readonly PdfName OutputIntents = CreateDirectName("OutputIntents");

        public static readonly PdfName Outset = CreateDirectName("Outset");

        public static readonly PdfName Overlay = CreateDirectName("Overlay");

        public static readonly PdfName OverlayText = CreateDirectName("OverlayText");

        public static readonly PdfName P = CreateDirectName("P");

        public static readonly PdfName PA = CreateDirectName("PA");

        public static readonly PdfName Padding = CreateDirectName("Padding");

        public static readonly PdfName Page = CreateDirectName("Page");

        public static readonly PdfName PageElement = CreateDirectName("PageElement");

        public static readonly PdfName PageLabels = CreateDirectName("PageLabels");

        public static readonly PdfName PageLayout = CreateDirectName("PageLayout");

        public static readonly PdfName PageMode = CreateDirectName("PageMode");

        public static readonly PdfName PageNum = CreateDirectName("PageNum");

        public static readonly PdfName Pages = CreateDirectName("Pages");

        public static readonly PdfName Pagination = CreateDirectName("Pagination");

        public static readonly PdfName PaintType = CreateDirectName("PaintType");

        public static readonly PdfName Panose = CreateDirectName("Panose");

        public static readonly PdfName Paperclip = CreateDirectName("Paperclip");

        public static readonly PdfName Params = CreateDirectName("Params");

        public static readonly PdfName Parent = CreateDirectName("Parent");

        public static readonly PdfName ParentTree = CreateDirectName("ParentTree");

        public static readonly PdfName ParentTreeNextKey = CreateDirectName("ParentTreeNextKey");

        public static readonly PdfName Part = CreateDirectName("Part");

        public static readonly PdfName Path = CreateDirectName("Path");

        public static readonly PdfName Pattern = CreateDirectName("Pattern");

        public static readonly PdfName PatternType = CreateDirectName("PatternType");

        public static readonly PdfName Pause = CreateDirectName("Pause");

        public static readonly PdfName Perceptual = CreateDirectName("Perceptual");

        public static readonly PdfName Perms = CreateDirectName("Perms");

        public static readonly PdfName PC = CreateDirectName("PC");

        public static readonly PdfName PCM = CreateDirectName("PCM");

        public static readonly PdfName Pdf_Version_1_2 = CreateDirectName("1.2");

        public static readonly PdfName Pdf_Version_1_3 = CreateDirectName("1.3");

        public static readonly PdfName Pdf_Version_1_4 = CreateDirectName("1.4");

        public static readonly PdfName Pdf_Version_1_5 = CreateDirectName("1.5");

        public static readonly PdfName Pdf_Version_1_6 = CreateDirectName("1.6");

        public static readonly PdfName Pdf_Version_1_7 = CreateDirectName("1.7");

        public static readonly PdfName Pg = CreateDirectName("Pg");

        public static readonly PdfName PI = CreateDirectName("PI");

        public static readonly PdfName PickTrayByPDFSize = CreateDirectName("PickTrayByPDFSize");

        public static readonly PdfName Placement = CreateDirectName("Placement");

        public static readonly PdfName Play = CreateDirectName("Play");

        public static readonly PdfName PO = CreateDirectName("PO");

        public static readonly PdfName Polygon = CreateDirectName("Polygon");

        public static readonly PdfName PolyLine = CreateDirectName("PolyLine");

        public static readonly PdfName Popup = CreateDirectName("Popup");

        public static readonly PdfName Predictor = CreateDirectName("Predictor");

        public static readonly PdfName Preferred = CreateDirectName("Preferred");

        public static readonly PdfName PreserveRB = CreateDirectName("PreserveRB");

        public static readonly PdfName PresSteps = CreateDirectName("PresSteps");

        public static readonly PdfName Prev = CreateDirectName("Prev");

        public static readonly PdfName PrevPage = CreateDirectName("PrevPage");

        public static readonly PdfName Print = CreateDirectName("Print");

        public static readonly PdfName PrintArea = CreateDirectName("PrintArea");

        public static readonly PdfName PrintClip = CreateDirectName("PrintClip");

        public static readonly PdfName PrinterMark = CreateDirectName("PrinterMark");

        public static readonly PdfName PrintPageRange = CreateDirectName("PrintPageRange");

        public static readonly PdfName PrintScaling = CreateDirectName("PrintScaling");

        public static readonly PdfName PrintState = CreateDirectName("PrintState");

        public static readonly PdfName Private = CreateDirectName("Private");

        public static readonly PdfName ProcSet = CreateDirectName("ProcSet");

        public static readonly PdfName Producer = CreateDirectName("Producer");

        public static readonly PdfName PronunciationLexicon = CreateDirectName("PronunciationLexicon"
            );

        public static readonly PdfName Prop_Build = CreateDirectName("Prop_Build");

        public static readonly PdfName Properties = CreateDirectName("Properties");

        public static readonly PdfName PS = CreateDirectName("PS");

        public static readonly PdfName Pushpin = CreateDirectName("PushPin");

        public static readonly PdfName PV = CreateDirectName("PV");

        public static readonly PdfName Q = CreateDirectName("Q");

        public static readonly PdfName Quote = CreateDirectName("Quote");

        public static readonly PdfName QuadPoints = CreateDirectName("QuadPoints");

        public static readonly PdfName r = CreateDirectName("r");

        public static readonly PdfName R = CreateDirectName("R");

        public static readonly PdfName R2L = CreateDirectName("R2L");

        public static readonly PdfName Range = CreateDirectName("Range");

        public static readonly PdfName Raw = CreateDirectName("Raw");

        public static readonly PdfName RB = CreateDirectName("RB");

        public static readonly PdfName RBGroups = CreateDirectName("RBGroups");

        public static readonly PdfName RC = CreateDirectName("RC");

        public static readonly PdfName RClosedArrow = CreateDirectName("RClosedArrow");

        public static readonly PdfName RD = CreateDirectName("RD");

        public static readonly PdfName Reason = CreateDirectName("Reason");

        public static readonly PdfName Recipients = CreateDirectName("Recipients");

        public static readonly PdfName Rect = CreateDirectName("Rect");

        public static readonly PdfName Redact = CreateDirectName("Redact");

        public static readonly PdfName Redaction = CreateDirectName("Redaction");

        public static readonly PdfName Reference = CreateDirectName("Reference");

        public static readonly PdfName Registry = CreateDirectName("Registry");

        public static readonly PdfName RegistryName = CreateDirectName("RegistryName");

        public static readonly PdfName RelativeColorimetric = CreateDirectName("RelativeColorimetric"
            );

        public static readonly PdfName Rendition = CreateDirectName("Rendition");

        public static readonly PdfName Renditions = CreateDirectName("Renditions");

        public static readonly PdfName Repeat = CreateDirectName("Repeat");

        public static readonly PdfName ResetForm = CreateDirectName("ResetForm");

        public static readonly PdfName Resume = CreateDirectName("Resume");

        public static readonly PdfName Requirement = CreateDirectName("Requirement");

        public static readonly PdfName Requirements = CreateDirectName("Requirements");

        public static readonly PdfName Resources = CreateDirectName("Resources");

        public static readonly PdfName ReversedChars = CreateDirectName("ReversedChars");

        public static readonly PdfName Phoneme = CreateDirectName("Phoneme");

        public static readonly PdfName PhoneticAlphabet = CreateDirectName("PhoneticAlphabet");

        public static readonly PdfName Ref = CreateDirectName("Ref");

        public static readonly PdfName RI = CreateDirectName("RI");

        public static readonly PdfName RichMedia = CreateDirectName("RichMedia");

        public static readonly PdfName Ridge = CreateDirectName("Ridge");

        public static readonly PdfName RO = CreateDirectName("RO");

        public static readonly PdfName RoleMap = CreateDirectName("RoleMap");

        public static readonly PdfName RoleMapNS = CreateDirectName("RoleMapNS");

        public static readonly PdfName ROpenArrow = CreateDirectName("ROpenArrow");

        public static readonly PdfName Root = CreateDirectName("Root");

        public static readonly PdfName Rotate = CreateDirectName("Rotate");

        public static readonly PdfName Row = CreateDirectName("Row");

        public static readonly PdfName Rows = CreateDirectName("Rows");

        public static readonly PdfName RowSpan = CreateDirectName("RowSpan");

        public static readonly PdfName RP = CreateDirectName("RP");

        public static readonly PdfName RT = CreateDirectName("RT");

        public static readonly PdfName Ruby = CreateDirectName("Ruby");

        public static readonly PdfName RubyAlign = CreateDirectName("RubyAlign");

        public static readonly PdfName RubyPosition = CreateDirectName("RubyPosition");

        public static readonly PdfName RunLengthDecode = CreateDirectName("RunLengthDecode");

        public static readonly PdfName RV = CreateDirectName("RV");

        public static readonly PdfName Stream = CreateDirectName("Stream");

        public static readonly PdfName S = CreateDirectName("S");

        public static readonly PdfName SA = CreateDirectName("SA");

        public static readonly PdfName Saturation = CreateDirectName("Saturation");

        public static readonly PdfName Schema = CreateDirectName("Schema");

        public static readonly PdfName Scope = CreateDirectName("Scope");

        public static readonly PdfName Screen = CreateDirectName("Screen");

        public static readonly PdfName SD = CreateDirectName("SD");

        public static readonly PdfName Sect = CreateDirectName("Sect");

        public static readonly PdfName Separation = CreateDirectName("Separation");

        public static readonly PdfName SeparationColorNames = CreateDirectName("SeparationColorNames"
            );

        public static readonly PdfName SeparationInfo = CreateDirectName("SeparationInfo");

        public static readonly PdfName Shading = CreateDirectName("Shading");

        public static readonly PdfName ShadingType = CreateDirectName("ShadingType");

        public static readonly PdfName SetOCGState = CreateDirectName("SetOCGState");

        public static readonly PdfName SetState = CreateDirectName("SetState");

        public static readonly PdfName Short = CreateDirectName("Short");

        public static readonly PdfName Sig = CreateDirectName("Sig");

        public static readonly PdfName SigFieldLock = CreateDirectName("SigFieldLock");

        public static readonly PdfName SigFlags = CreateDirectName("SigFlags");

        public static readonly PdfName Signed = CreateDirectName("Signed");

        public static readonly PdfName SigRef = CreateDirectName("SigRef");

        public static readonly PdfName Simplex = CreateDirectName("Simplex");

        public static readonly PdfName SinglePage = CreateDirectName("SinglePage");

        public static readonly PdfName Size = CreateDirectName("Size");

        public static readonly PdfName Slash = CreateDirectName("Slash");

        public static readonly PdfName SM = CreateDirectName("SM");

        public static readonly PdfName SMask = CreateDirectName("SMask");

        public static readonly PdfName SMaskInData = CreateDirectName("SMaskInData");

        public static readonly PdfName SoftLight = CreateDirectName("SoftLight");

        public static readonly PdfName Sold = CreateDirectName("Sold");

        public static readonly PdfName Solid = CreateDirectName("Solid");

        public static readonly PdfName Sort = CreateDirectName("Sort");

        public static readonly PdfName Sound = CreateDirectName("Sound");

        public static readonly PdfName Source = CreateDirectName("Source");

        public static readonly PdfName Span = CreateDirectName("Span");

        public static readonly PdfName SpaceBefore = CreateDirectName("SpaceBefore");

        public static readonly PdfName SpaceAfter = CreateDirectName("SpaceAfter");

        public static readonly PdfName Square = CreateDirectName("Square");

        public static readonly PdfName Squiggly = CreateDirectName("Squiggly");

        public static readonly PdfName St = CreateDirectName("St");

        public static readonly PdfName Stamp = CreateDirectName("Stamp");

        public static readonly PdfName StampImage = CreateDirectName("StampImage");

        public static readonly PdfName StampSnapshot = CreateDirectName("StampSnapshot");

        public static readonly PdfName Standard = CreateDirectName("Standard");

        public static readonly PdfName Start = CreateDirectName("Start");

        public static readonly PdfName StartIndent = CreateDirectName("StartIndent");

        public static readonly PdfName State = CreateDirectName("State");

        public static readonly PdfName StateModel = CreateDirectName("StateModel");

        public static readonly PdfName StdCF = CreateDirectName("StdCF");

        public static readonly PdfName StemV = CreateDirectName("StemV");

        public static readonly PdfName StemH = CreateDirectName("StemH");

        public static readonly PdfName Stop = CreateDirectName("Stop");

        public static readonly PdfName Stm = CreateDirectName("Stm");

        public static readonly PdfName StmF = CreateDirectName("StmF");

        public static readonly PdfName StrF = CreateDirectName("StrF");

        public static readonly PdfName StrikeOut = CreateDirectName("StrikeOut");

        public static readonly PdfName Strong = CreateDirectName("Strong");

        public static readonly PdfName StructElem = CreateDirectName("StructElem");

        public static readonly PdfName StructParent = CreateDirectName("StructParent");

        public static readonly PdfName StructParents = CreateDirectName("StructParents");

        public static readonly PdfName StructTreeRoot = CreateDirectName("StructTreeRoot");

        public static readonly PdfName Style = CreateDirectName("Style");

        public static readonly PdfName Sub = CreateDirectName("Sub");

        public static readonly PdfName SubFilter = CreateDirectName("SubFilter");

        public static readonly PdfName Subj = CreateDirectName("Subj");

        public static readonly PdfName Subject = CreateDirectName("Subject");

        public static readonly PdfName SubmitForm = CreateDirectName("SubmitForm");

        public static readonly PdfName Subtype = CreateDirectName("Subtype");

        public static readonly PdfName Subtype2 = CreateDirectName("Subtype2");

        public static readonly PdfName Supplement = CreateDirectName("Supplement");

        public static readonly PdfName Sy = CreateDirectName("Sy");

        public static readonly PdfName Symbol = CreateDirectName("Symbol");

        public static readonly PdfName Synchronous = CreateDirectName("Synchronous");

        public static readonly PdfName T = CreateDirectName("T");

        public static readonly PdfName Tag = CreateDirectName("Tag");

        public static readonly PdfName TBorderStyle = CreateDirectName("TBorderStyle");

        public static readonly PdfName TA = CreateDirectName("TA");

        public static readonly PdfName Table = CreateDirectName("Table");

        public static readonly PdfName Tabs = CreateDirectName("Tabs");

        public static readonly PdfName TBody = CreateDirectName("TBody");

        public static readonly PdfName TD = CreateDirectName("TD");

        public static readonly PdfName Templates = CreateDirectName("Templates");

        public static readonly PdfName Text = CreateDirectName("Text");

        public static readonly PdfName TextAlign = CreateDirectName("TextAlign");

        public static readonly PdfName TextDecorationColor = CreateDirectName("TextDecorationColor"
            );

        public static readonly PdfName TextDecorationThickness = CreateDirectName("TextDecorationThickness"
            );

        public static readonly PdfName TextDecorationType = CreateDirectName("TextDecorationType"
            );

        public static readonly PdfName TextIndent = CreateDirectName("TextIndent");

        public static readonly PdfName TF = CreateDirectName("TF");

        public static readonly PdfName TFoot = CreateDirectName("TFoot");

        public static readonly PdfName TH = CreateDirectName("TH");

        public static readonly PdfName THead = CreateDirectName("THead");

        public static readonly PdfName Thumb = CreateDirectName("Thumb");

        public static readonly PdfName TI = CreateDirectName("TI");

        public static readonly PdfName TilingType = CreateDirectName("TilingType");

        public static readonly PdfName Title = CreateDirectName("Title");

        public static readonly PdfName TPadding = CreateDirectName("TPadding");

        public static readonly PdfName TrimBox = CreateDirectName("TrimBox");

        public static readonly PdfName TK = CreateDirectName("TK");

        public static readonly PdfName TM = CreateDirectName("TM");

        public static readonly PdfName TOC = CreateDirectName("TOC");

        public static readonly PdfName TOCI = CreateDirectName("TOCI");

        public static readonly PdfName TP = CreateDirectName("TP");

        public static readonly PdfName Toggle = CreateDirectName("Toggle");

        public static readonly PdfName Top = CreateDirectName("Top");

        public static readonly PdfName TopSecret = CreateDirectName("TopSecret");

        public static readonly PdfName ToUnicode = CreateDirectName("ToUnicode");

        public static readonly PdfName TR = CreateDirectName("TR");

        public static readonly PdfName TR2 = CreateDirectName("TR2");

        public static readonly PdfName Trans = CreateDirectName("Trans");

        public static readonly PdfName TransformMethod = CreateDirectName("TransformMethod");

        public static readonly PdfName TransformParams = CreateDirectName("TransformParams");

        public static readonly PdfName Transparency = CreateDirectName("Transparency");

        public static readonly PdfName TrapNet = CreateDirectName("TrapNet");

        public static readonly PdfName Trapped = CreateDirectName("Trapped");

        public static readonly PdfName TrapRegions = CreateDirectName("TrapRegions");

        public static readonly PdfName TrapStyles = CreateDirectName("TrapStyles");

        public static readonly PdfName True = CreateDirectName("true");

        public static readonly PdfName TrueType = CreateDirectName("TrueType");

        public static readonly PdfName TU = CreateDirectName("TU");

        public static readonly PdfName TwoColumnLeft = CreateDirectName("TwoColumnLeft");

        public static readonly PdfName TwoColumnRight = CreateDirectName("TwoColumnRight");

        public static readonly PdfName TwoPageLeft = CreateDirectName("TwoPageLeft");

        public static readonly PdfName TwoPageRight = CreateDirectName("TwoPageRight");

        public static readonly PdfName Tx = CreateDirectName("Tx");

        public static readonly PdfName Type = CreateDirectName("Type");

        public static readonly PdfName Type0 = CreateDirectName("Type0");

        public static readonly PdfName Type1 = CreateDirectName("Type1");

        public static readonly PdfName Type3 = CreateDirectName("Type3");

        public static readonly PdfName U = CreateDirectName("U");

        public static readonly PdfName UCR = CreateDirectName("UCR");

        public static readonly PdfName UR3 = CreateDirectName("UR3");

        public static readonly PdfName UCR2 = CreateDirectName("UCR2");

        public static readonly PdfName UE = CreateDirectName("UE");

        public static readonly PdfName UF = CreateDirectName("UF");

        public static readonly PdfName Underline = CreateDirectName("Underline");

        public static readonly PdfName Unordered = CreateDirectName("Unordered");

        public static readonly PdfName Unspecified = CreateDirectName("Unspecified");

        public static readonly PdfName UpperAlpha = CreateDirectName("UpperAlpha");

        public static readonly PdfName UpperRoman = CreateDirectName("UpperRoman");

        public static readonly PdfName URI = CreateDirectName("URI");

        public static readonly PdfName URL = CreateDirectName("URL");

        public static readonly PdfName URLS = CreateDirectName("URLS");

        public static readonly PdfName Usage = CreateDirectName("Usage");

        public static readonly PdfName UseAttachments = CreateDirectName("UseAttachments");

        public static readonly PdfName UseBlackPtComp = CreateDirectName("UseBlackPtComp");

        public static readonly PdfName UseNone = CreateDirectName("UseNone");

        public static readonly PdfName UseOC = CreateDirectName("UseOC");

        public static readonly PdfName UseOutlines = CreateDirectName("UseOutlines");

        public static readonly PdfName UseThumbs = CreateDirectName("UseThumbs");

        public static readonly PdfName User = CreateDirectName("User");

        public static readonly PdfName UserProperties = CreateDirectName("UserProperties");

        public static readonly PdfName UserUnit = CreateDirectName("UserUnit");

        public static readonly PdfName V = CreateDirectName("V");

        public static readonly PdfName V2 = CreateDirectName("V2");

        public static readonly PdfName VE = CreateDirectName("VE");

        public static readonly PdfName Version = CreateDirectName("Version");

        public static readonly PdfName Vertices = CreateDirectName("Vertices");

        public static readonly PdfName VerticesPerRow = CreateDirectName("VerticesPerRow");

        public static readonly PdfName View = CreateDirectName("View");

        public static readonly PdfName ViewArea = CreateDirectName("ViewArea");

        public static readonly PdfName ViewerPreferences = CreateDirectName("ViewerPreferences");

        public static readonly PdfName ViewClip = CreateDirectName("ViewClip");

        public static readonly PdfName ViewState = CreateDirectName("ViewState");

        public static readonly PdfName VisiblePages = CreateDirectName("VisiblePages");

        public static readonly PdfName Volatile = CreateDirectName("Volatile");

        public static readonly PdfName Volume = CreateDirectName("Volume");

        public static readonly PdfName VRI = CreateDirectName("VRI");

        public static readonly PdfName W = CreateDirectName("W");

        public static readonly PdfName W2 = CreateDirectName("W2");

        public static readonly PdfName Warichu = CreateDirectName("Warichu");

        public static readonly PdfName Watermark = CreateDirectName("Watermark");

        public static readonly PdfName WC = CreateDirectName("WC");

        public static readonly PdfName WhitePoint = CreateDirectName("WhitePoint");

        public static readonly PdfName Width = CreateDirectName("Width");

        public static readonly PdfName Widths = CreateDirectName("Widths");

        public static readonly PdfName Widget = CreateDirectName("Widget");

        public static readonly PdfName Win = CreateDirectName("Win");

        public static readonly PdfName WinAnsiEncoding = CreateDirectName("WinAnsiEncoding");

        public static readonly PdfName WritingMode = CreateDirectName("WritingMode");

        public static readonly PdfName WP = CreateDirectName("WP");

        public static readonly PdfName WS = CreateDirectName("WS");

        public static readonly PdfName WT = CreateDirectName("WT");

        public static readonly PdfName X = CreateDirectName("X");

        public static readonly PdfName x_sampa = CreateDirectName("x-sampa");

        public static readonly PdfName XFA = CreateDirectName("XFA");

        public static readonly PdfName XML = CreateDirectName("XML");

        public static readonly PdfName XObject = CreateDirectName("XObject");

        public static readonly PdfName XHeight = CreateDirectName("XHeight");

        public static readonly PdfName XRef = CreateDirectName("XRef");

        public static readonly PdfName XRefStm = CreateDirectName("XRefStm");

        public static readonly PdfName XStep = CreateDirectName("XStep");

        public static readonly PdfName XYZ = CreateDirectName("XYZ");

        public static readonly PdfName YStep = CreateDirectName("YStep");

        public static readonly PdfName ZapfDingbats = CreateDirectName("ZapfDingbats");

        public static readonly PdfName zh_Latn_pinyin = CreateDirectName("zh-Latn-pinyin");

        public static readonly PdfName zh_Latn_wadegile = CreateDirectName("zh-Latn-wadegile");

        public static readonly PdfName Zoom = CreateDirectName("Zoom");

        protected internal string value;

        /// <summary>map strings to all known static names</summary>
        public static IDictionary<string, PdfName> staticNames;

        static PdfName() {
            staticNames = PdfNameLoader.LoadNames();
        }

        private static PdfName CreateDirectName(string name) {
            return new PdfName(name, true);
        }

        /// <summary>Create a PdfName from the passed string</summary>
        /// <param name="value">string value, shall not be null.</param>
        public PdfName(string value)
        {
            Debug.Assert(value != null);
            this.value = value;
        }

        private PdfName(string value, bool directOnly)
            : base(directOnly) {
            this.value = value;
        }

        /// <summary>Create a PdfName from the passed bytes</summary>
        /// <param name="content">byte content, shall not be null.</param>
        public PdfName(byte[] content)
            : base(content) {
        }

        private PdfName()
        {
        }

        public override byte GetObjectType() {
            return NAME;
        }

        public virtual string GetValue() {
            if (value == null) {
                GenerateValue();
            }
            return value;
        }

        /// <summary>Compare this PdfName to o.</summary>
        /// <param name="o">PdfName to compare this object to/</param>
        /// <returns>Comparison between both values or, if one of the values is null, Comparison between contents. If one of the values and one of the contents are equal to null, generate values and compare those.
        ///     </returns>
        public virtual int CompareTo(PdfName o) {
            return string.CompareOrdinal(GetValue(), o.GetValue());
        }

        public override bool Equals(object o) {
            if (this == o) {
                return true;
            }
            if (o == null || GetType() != o.GetType()) {
                return false;
            }
            var pdfName = (PdfName)o;
            return CompareTo(pdfName) == 0;
        }

        public override int GetHashCode() {
            return GetValue().GetHashCode();
        }

        protected internal virtual void GenerateValue() {
            var buf = new StringBuilder();
            try {
                for (var k = 0; k < content.Length; ++k) {
                    var c = (char)content[k];
                    if (c == '#') {
                        var c1 = content[k + 1];
                        var c2 = content[k + 2];
                        c = (char)((ByteBuffer.GetHex(c1) << 4) + ByteBuffer.GetHex(c2));
                        k += 2;
                    }
                    buf.Append(c);
                }
            }
            catch (IndexOutOfRangeException) {
            }
            // empty on purpose
            value = buf.ToString();
        }

        protected internal override void GenerateContent() {
            var length = value.Length;
            var buf = new ByteBuffer(length + 20);
            char c;
            var chars = value.ToCharArray();
            for (var k = 0; k < length; k++) {
                c = (char)(chars[k] & 0xff);
                // Escape special characters
                switch (c) {
                    case ' ': {
                        buf.Append(space);
                        break;
                    }

                    case '%': {
                        buf.Append(percent);
                        break;
                    }

                    case '(': {
                        buf.Append(leftParenthesis);
                        break;
                    }

                    case ')': {
                        buf.Append(rightParenthesis);
                        break;
                    }

                    case '<': {
                        buf.Append(lessThan);
                        break;
                    }

                    case '>': {
                        buf.Append(greaterThan);
                        break;
                    }

                    case '[': {
                        buf.Append(leftSquare);
                        break;
                    }

                    case ']': {
                        buf.Append(rightSquare);
                        break;
                    }

                    case '{': {
                        buf.Append(leftCurlyBracket);
                        break;
                    }

                    case '}': {
                        buf.Append(rightCurlyBracket);
                        break;
                    }

                    case '/': {
                        buf.Append(solidus);
                        break;
                    }

                    case '#': {
                        buf.Append(numberSign);
                        break;
                    }

                    default: {
                        if (c >= 32 && c <= 126) {
                            buf.Append(c);
                        }
                        else {
                            buf.Append('#');
                            if (c < 16) {
                                buf.Append('0');
                            }
                            buf.Append(JavaUtil.IntegerToHexString(c));
                        }
                        break;
                    }
                }
            }
            content = buf.ToByteArray();
        }

        public override string ToString()
        {
	        if (content != null) {
                return "/" + JavaUtil.GetStringForBytes(content, EncodingUtil.ISO_8859_1);
            }

	        return "/" + GetValue();
        }

        protected internal override PdfObject NewInstance() {
            return new PdfName();
        }

        protected internal override void CopyContent(PdfObject from, PdfDocument document) {
            base.CopyContent(from, document);
            var name = (PdfName)from;
            value = name.value;
        }
    }
}
