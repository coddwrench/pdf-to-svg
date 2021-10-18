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

using IText.IO;
using IText.Kernel.Geom;
using IText.Kernel.Pdf.Action;
using IText.Kernel.Pdf.Navigation;
using IText.Logger;

namespace IText.Kernel.Pdf.Annot {
    /// <summary>
    /// A link annotation represents either a hypertext link to a destination elsewhere in the document
    /// or an
    /// <see cref="PdfAction"/>
    /// to be performed.
    /// </summary>
    /// <remarks>
    /// A link annotation represents either a hypertext link to a destination elsewhere in the document
    /// or an
    /// <see cref="PdfAction"/>
    /// to be performed. See also ISO-320001 12.5.6.5, "Link Annotations".
    /// </remarks>
    public class PdfLinkAnnotation : PdfAnnotation {
        private static readonly ILog logger = LogManager.GetLogger(typeof(PdfLinkAnnotation
            ));

        /// <summary>Highlight modes.</summary>
        public static readonly PdfName None = PdfName.N;

        public static readonly PdfName Invert = PdfName.I;

        public static readonly PdfName Outline = PdfName.O;

        public static readonly PdfName Push = PdfName.P;

        /// <summary>
        /// Creates a new
        /// <see cref="PdfLinkAnnotation"/>
        /// instance based on
        /// <see cref="PdfDictionary"/>
        /// instance, that represents existing annotation object in the document.
        /// </summary>
        /// <param name="pdfObject">
        /// the
        /// <see cref="PdfDictionary"/>
        /// representing annotation object
        /// </param>
        /// <seealso cref="PdfAnnotation.MakeAnnotation(PdfObject)"/>
        protected internal PdfLinkAnnotation(PdfDictionary pdfObject)
            : base(pdfObject) {
        }

        /// <summary>
        /// Creates a new
        /// <see cref="PdfLinkAnnotation"/>
        /// instance based on
        /// <see cref="Rectangle"/>
        /// instance, that define the location of the annotation on the page in default user space units.
        /// </summary>
        /// <param name="rect">
        /// the
        /// <see cref="Rectangle"/>
        /// that define the location of the annotation
        /// </param>
        public PdfLinkAnnotation(Rectangle rect)
            : base(rect) {
        }

        public override PdfName GetSubtype() {
            return PdfName.Link;
        }

        /// <summary>
        /// Gets the annotation destination as
        /// <see cref="PdfObject"/>
        /// instance.
        /// </summary>
        /// <remarks>
        /// Gets the annotation destination as
        /// <see cref="PdfObject"/>
        /// instance.
        /// <para />
        /// Destination shall be displayed when the annotation is activated. See also ISO-320001, Table 173.
        /// </remarks>
        /// <returns>
        /// the annotation destination as
        /// <see cref="PdfObject"/>
        /// instance
        /// </returns>
        public virtual PdfObject GetDestinationObject() {
            return GetPdfObject().Get(PdfName.Dest);
        }

        /// <summary>
        /// Sets the annotation destination as
        /// <see cref="PdfObject"/>
        /// instance.
        /// </summary>
        /// <remarks>
        /// Sets the annotation destination as
        /// <see cref="PdfObject"/>
        /// instance.
        /// <para />
        /// Destination shall be displayed when the annotation is activated. See also ISO-320001, Table 173.
        /// </remarks>
        /// <param name="destination">
        /// the destination to be set as
        /// <see cref="PdfObject"/>
        /// instance
        /// </param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance
        /// </returns>
        public virtual PdfLinkAnnotation SetDestination(PdfObject destination) {
            if (GetPdfObject().ContainsKey(PdfName.A)) {
                GetPdfObject().Remove(PdfName.A);
                logger.Warn(LogMessageConstant.DESTINATION_NOT_PERMITTED_WHEN_ACTION_IS_SET);
            }
            if (destination.IsArray() && ((PdfArray)destination).Get(0).IsNumber()) {
                LogManager.GetLogger(typeof(PdfLinkAnnotation)).Warn(LogMessageConstant.INVALID_DESTINATION_TYPE
                    );
            }
            return (PdfLinkAnnotation)Put(PdfName.Dest, destination);
        }

        /// <summary>
        /// Sets the annotation destination as
        /// <see cref="PdfDestination"/>
        /// instance.
        /// </summary>
        /// <remarks>
        /// Sets the annotation destination as
        /// <see cref="PdfDestination"/>
        /// instance.
        /// <para />
        /// Destination shall be displayed when the annotation is activated. See also ISO-320001, Table 173.
        /// </remarks>
        /// <param name="destination">
        /// the destination to be set as
        /// <see cref="PdfDestination"/>
        /// instance
        /// </param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance
        /// </returns>
        public virtual PdfLinkAnnotation SetDestination(PdfDestination destination) {
            return SetDestination(destination.GetPdfObject());
        }

        /// <summary>Removes the annotation destination.</summary>
        /// <remarks>
        /// Removes the annotation destination.
        /// <para />
        /// Destination shall be displayed when the annotation is activated. See also ISO-320001, Table 173.
        /// </remarks>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance
        /// </returns>
        public virtual PdfLinkAnnotation RemoveDestination() {
            GetPdfObject().Remove(PdfName.Dest);
            return this;
        }

        /// <summary>
        /// An
        /// <see cref="PdfAction"/>
        /// to perform, such as launching an application, playing a sound,
        /// changing an annotation’s appearance state etc, when the annotation is activated.
        /// </summary>
        /// <returns>
        /// 
        /// <see cref="PdfDictionary"/>
        /// which defines the characteristics and behaviour of an action
        /// </returns>
        public virtual PdfDictionary GetAction() {
            return GetPdfObject().GetAsDictionary(PdfName.A);
        }

        /// <summary>
        /// Sets a
        /// <see cref="PdfDictionary"/>
        /// representing action to this annotation which will be performed
        /// when the annotation is activated.
        /// </summary>
        /// <param name="action">
        /// 
        /// <see cref="PdfDictionary"/>
        /// that represents action to set to this annotation
        /// </param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance
        /// </returns>
        public virtual PdfLinkAnnotation SetAction(PdfDictionary action) {
            return (PdfLinkAnnotation)Put(PdfName.A, action);
        }

        /// <summary>
        /// Sets a
        /// <see cref="PdfAction"/>
        /// to this annotation which will be performed when the annotation is activated.
        /// </summary>
        /// <param name="action">
        /// 
        /// <see cref="PdfAction"/>
        /// to set to this annotation
        /// </param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance
        /// </returns>
        public virtual PdfLinkAnnotation SetAction(PdfAction action) {
            if (GetDestinationObject() != null) {
                RemoveDestination();
                logger.Warn(LogMessageConstant.ACTION_WAS_SET_TO_LINK_ANNOTATION_WITH_DESTINATION);
            }
            return (PdfLinkAnnotation)Put(PdfName.A, action.GetPdfObject());
        }

        /// <summary>
        /// Removes a
        /// <see cref="PdfAction"/>
        /// from this annotation.
        /// </summary>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance
        /// </returns>
        public virtual PdfLinkAnnotation RemoveAction() {
            GetPdfObject().Remove(PdfName.A);
            return this;
        }

        /// <summary>Gets the annotation highlight mode.</summary>
        /// <remarks>
        /// Gets the annotation highlight mode.
        /// <para />
        /// The annotation’s highlighting mode is the visual effect that shall be used when the mouse
        /// button is pressed or held down inside its active area. See also ISO-320001, Table 173.
        /// </remarks>
        /// <returns>the name of visual effect</returns>
        public virtual PdfName GetHighlightMode() {
            return GetPdfObject().GetAsName(PdfName.H);
        }

        /// <summary>Sets the annotation highlight mode.</summary>
        /// <remarks>
        /// Sets the annotation highlight mode.
        /// <para />
        /// The annotation’s highlighting mode is the visual effect that shall be used when the mouse
        /// button is pressed or held down inside its active area. See also ISO-320001, Table 173.
        /// </remarks>
        /// <param name="hlMode">the name of visual effect to be set</param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance
        /// </returns>
        public virtual PdfLinkAnnotation SetHighlightMode(PdfName hlMode) {
            return (PdfLinkAnnotation)Put(PdfName.H, hlMode);
        }

        /// <summary>
        /// Gets the annotation URI action as
        /// <see cref="PdfDictionary"/>.
        /// </summary>
        /// <remarks>
        /// Gets the annotation URI action as
        /// <see cref="PdfDictionary"/>.
        /// <para />
        /// When Web Capture (see ISO-320001 14.10, “Web Capture”) changes an annotation from a URI to a
        /// go-to action, it uses this entry to save the data from the original URI action so that it can
        /// be changed back in case the target page for the go-to action is subsequently deleted. See also
        /// ISO-320001, Table 173.
        /// </remarks>
        /// <returns>the URI action as pdfDictionary</returns>
        public virtual PdfDictionary GetUriActionObject() {
            return GetPdfObject().GetAsDictionary(PdfName.PA);
        }

        /// <summary>
        /// Sets the annotation URI action as
        /// <see cref="PdfDictionary"/>
        /// instance.
        /// </summary>
        /// <remarks>
        /// Sets the annotation URI action as
        /// <see cref="PdfDictionary"/>
        /// instance.
        /// <para />
        /// When Web Capture (see ISO-320001 14.10, “Web Capture”) changes an annotation from a URI to a
        /// go-to action, it uses this entry to save the data from the original URI action so that it can
        /// be changed back in case the target page for the go-to action is subsequently deleted. See also
        /// ISO-320001, Table 173.
        /// </remarks>
        /// <param name="action">the action to be set</param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance
        /// </returns>
        public virtual PdfLinkAnnotation SetUriAction(PdfDictionary action) {
            return (PdfLinkAnnotation)Put(PdfName.PA, action);
        }

        /// <summary>
        /// Sets the annotation URI action as
        /// <see cref="PdfAction"/>
        /// instance.
        /// </summary>
        /// <remarks>
        /// Sets the annotation URI action as
        /// <see cref="PdfAction"/>
        /// instance.
        /// <para />
        /// A URI action (see ISO-320001 12.6.4.7, “URI Actions”) formerly associated with this annotation.
        /// When Web Capture (see ISO-320001 14.10, “Web Capture”) changes an annotation from a URI to a
        /// go-to action, it uses this entry to save the data from the original URI action so that it can
        /// be changed back in case the target page for the go-to action is subsequently deleted. See also
        /// ISO-320001, Table 173.
        /// </remarks>
        /// <param name="action">the action to be set</param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance
        /// </returns>
        public virtual PdfLinkAnnotation SetUriAction(PdfAction action) {
            return (PdfLinkAnnotation)Put(PdfName.PA, action.GetPdfObject());
        }

        /// <summary>An array of 8 × n numbers specifying the coordinates of n quadrilaterals in default user space.</summary>
        /// <remarks>
        /// An array of 8 × n numbers specifying the coordinates of n quadrilaterals in default user space.
        /// Quadrilaterals are used to define regions inside annotation rectangle
        /// in which the link annotation should be activated.
        /// </remarks>
        /// <returns>
        /// an
        /// <see cref="PdfArray"/>
        /// of 8 × n numbers specifying the coordinates of n quadrilaterals.
        /// </returns>
        public virtual PdfArray GetQuadPoints() {
            return GetPdfObject().GetAsArray(PdfName.QuadPoints);
        }

        /// <summary>
        /// Sets n quadrilaterals in default user space by passing an
        /// <see cref="PdfArray"/>
        /// of 8 × n numbers.
        /// </summary>
        /// <remarks>
        /// Sets n quadrilaterals in default user space by passing an
        /// <see cref="PdfArray"/>
        /// of 8 × n numbers.
        /// Quadrilaterals are used to define regions inside annotation rectangle
        /// in which the link annotation should be activated.
        /// </remarks>
        /// <param name="quadPoints">
        /// an
        /// <see cref="PdfArray"/>
        /// of 8 × n numbers specifying the coordinates of n quadrilaterals.
        /// </param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance.
        /// </returns>
        public virtual PdfLinkAnnotation SetQuadPoints(PdfArray quadPoints) {
            return (PdfLinkAnnotation)Put(PdfName.QuadPoints, quadPoints);
        }

        /// <summary>
        /// BS entry specifies a border style dictionary that has more settings than the array specified for the Border
        /// entry (see
        /// <see cref="PdfAnnotation.GetBorder()"/>
        /// ).
        /// </summary>
        /// <remarks>
        /// BS entry specifies a border style dictionary that has more settings than the array specified for the Border
        /// entry (see
        /// <see cref="PdfAnnotation.GetBorder()"/>
        /// ). If an annotation dictionary includes the BS entry, then the Border
        /// entry is ignored. If annotation includes AP (see
        /// <see cref="PdfAnnotation.GetAppearanceDictionary()"/>
        /// ) it takes
        /// precedence over the BS entry. For more info on BS entry see ISO-320001, Table 166.
        /// </remarks>
        /// <returns>
        /// 
        /// <see cref="PdfDictionary"/>
        /// which is a border style dictionary or null if it is not specified.
        /// </returns>
        public virtual PdfDictionary GetBorderStyle() {
            return GetPdfObject().GetAsDictionary(PdfName.BS);
        }

        /// <summary>
        /// Sets border style dictionary that has more settings than the array specified for the Border entry (
        /// <see cref="PdfAnnotation.GetBorder()"/>
        /// ).
        /// </summary>
        /// <remarks>
        /// Sets border style dictionary that has more settings than the array specified for the Border entry (
        /// <see cref="PdfAnnotation.GetBorder()"/>
        /// ).
        /// See ISO-320001, Table 166 and
        /// <see cref="GetBorderStyle()"/>
        /// for more info.
        /// </remarks>
        /// <param name="borderStyle">
        /// a border style dictionary specifying the line width and dash pattern that shall be used
        /// in drawing the annotation’s border.
        /// </param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance.
        /// </returns>
        public virtual PdfLinkAnnotation SetBorderStyle(PdfDictionary borderStyle) {
            return (PdfLinkAnnotation)Put(PdfName.BS, borderStyle);
        }

        /// <summary>Setter for the annotation's preset border style.</summary>
        /// <remarks>
        /// Setter for the annotation's preset border style. Possible values are
        /// <list type="bullet">
        /// <item><description>
        /// <see cref="PdfAnnotation.STYLE_SOLID"/>
        /// - A solid rectangle surrounding the annotation.
        /// </description></item>
        /// <item><description>
        /// <see cref="PdfAnnotation.STYLE_DASHED"/>
        /// - A dashed rectangle surrounding the annotation.
        /// </description></item>
        /// <item><description>
        /// <see cref="PdfAnnotation.STYLE_BEVELED"/>
        /// - A simulated embossed rectangle that appears to be raised above the surface of the page.
        /// </description></item>
        /// <item><description>
        /// <see cref="PdfAnnotation.STYLE_INSET"/>
        /// - A simulated engraved rectangle that appears to be recessed below the surface of the page.
        /// </description></item>
        /// <item><description>
        /// <see cref="PdfAnnotation.STYLE_UNDERLINE"/>
        /// - A single line along the bottom of the annotation rectangle.
        /// </description></item>
        /// </list>
        /// See also ISO-320001, Table 166.
        /// </remarks>
        /// <param name="style">The new value for the annotation's border style.</param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance.
        /// </returns>
        /// <seealso cref="GetBorderStyle()"/>
        public virtual PdfLinkAnnotation SetBorderStyle(PdfName style) {
            return SetBorderStyle(BorderStyleUtil.SetStyle(GetBorderStyle(), style));
        }

        /// <summary>Setter for the annotation's preset dashed border style.</summary>
        /// <remarks>
        /// Setter for the annotation's preset dashed border style. This property has affect only if
        /// <see cref="PdfAnnotation.STYLE_DASHED"/>
        /// style was used for the annotation border style (see
        /// <see cref="SetBorderStyle(PdfName)"/>.
        /// See ISO-320001 8.4.3.6, "Line Dash Pattern" for the format in which dash pattern shall be specified.
        /// </remarks>
        /// <param name="dashPattern">
        /// a dash array defining a pattern of dashes and gaps that
        /// shall be used in drawing a dashed border.
        /// </param>
        /// <returns>
        /// this
        /// <see cref="PdfLinkAnnotation"/>
        /// instance.
        /// </returns>
        public virtual PdfLinkAnnotation SetDashPattern(PdfArray dashPattern) {
            return SetBorderStyle(BorderStyleUtil.SetDashPattern(GetBorderStyle(), dashPattern));
        }
    }
}
