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
using System.Collections;
using System.Collections.Generic;
using IText.IO;
using IText.IO.Source;
using IText.IO.Util;
using IText.Kernel.Colors;
using IText.Kernel.Font;
using IText.Kernel.Geom;
using IText.Kernel.Pdf.Canvas.Parser.Data;
using IText.Kernel.Pdf.Canvas.Parser.Listener;
using IText.Kernel.Pdf.Canvas.Parser.Util;
using IText.Kernel.Pdf.Colorspace;
using IText.Kernel.Pdf.Extgstate;
using IText.Logger;
using IOException = System.IO.IOException;
using Path = IText.Kernel.Geom.Path;

namespace IText.Kernel.Pdf.Canvas.Parser
{
	/// <summary>Processor for a PDF content stream.</summary>
	public class PdfCanvasProcessor
	{
		public const string DEFAULT_OPERATOR = "DefaultOperator";

		/// <summary>Listener that will be notified of render events</summary>
		protected internal readonly IEventListener eventListener;

		/// <summary>
		/// Cache supported events in case the user's
		/// <see cref="IEventListener.GetSupportedEvents()"/>
		/// method is not very efficient
		/// </summary>
		protected internal readonly ICollection<EventType> supportedEvents;

		protected internal Path currentPath = new Path();

		/// <summary>
		/// Indicates whether the current clipping path should be modified by
		/// intersecting it with the current path.
		/// </summary>
		protected internal bool isClip;

		/// <summary>
		/// Specifies the filling rule which should be applied while calculating
		/// new clipping path.
		/// </summary>
		protected internal int clippingRule;

		/// <summary>A map with all supported operators (PDF syntax).</summary>
		private IDictionary<string, IContentOperator> operators;

		/// <summary>Resources for the content stream.</summary>
		/// <remarks>
		/// Resources for the content stream.
		/// Current resources are always at the top of the stack.
		/// Stack is needed in case if some "inner" content stream with it's own resources
		/// is encountered (like Form XObject).
		/// </remarks>
		private Stack<PdfResources> resourcesStack;

		/// <summary>Stack keeping track of the graphics state.</summary>
		private readonly Stack<ParserGraphicsState> gsStack = new Stack<ParserGraphicsState>();

		private Matrix textMatrix;

		private Matrix textLineMatrix;

		/// <summary>A map with all supported XObject handlers</summary>
		private IDictionary<PdfName, IXObjectDoHandler> xobjectDoHandlers;

		/// <summary>The font cache</summary>
		private IDictionary<int, WeakReference> cachedFonts = new Dictionary<int, WeakReference>();

		/// <summary>A stack containing marked content info.</summary>
		private Stack<CanvasTag> markedContentStack = new Stack<CanvasTag>();

		/// <summary>
		/// Creates a new PDF Content Stream Processor that will send its output to the
		/// designated render listener.
		/// </summary>
		/// <param name="eventListener">
		/// the
		/// <see cref="IEventListener"/>
		/// that will receive rendering notifications
		/// </param>
		public PdfCanvasProcessor(IEventListener eventListener)
		{
			this.eventListener = eventListener;
			supportedEvents = eventListener.GetSupportedEvents();
			operators = new Dictionary<string, IContentOperator>();
			PopulateOperators();
			xobjectDoHandlers = new Dictionary<PdfName, IXObjectDoHandler>();
			PopulateXObjectDoHandlers();
			Reset();
		}

		/// <summary>
		/// Creates a new PDF Content Stream Processor that will send its output to the
		/// designated render listener.
		/// </summary>
		/// <remarks>
		/// Creates a new PDF Content Stream Processor that will send its output to the
		/// designated render listener.
		/// Also allows registration of custom IContentOperators that can influence
		/// how (and whether or not) the PDF instructions will be parsed.
		/// </remarks>
		/// <param name="eventListener">
		/// the
		/// <see cref="IEventListener"/>
		/// that will receive rendering notifications
		/// </param>
		/// <param name="additionalContentOperators">
		/// an optional map of custom
		/// <see cref="IContentOperator"/>
		/// s for rendering instructions
		/// </param>
		public PdfCanvasProcessor(IEventListener eventListener, IDictionary<string, IContentOperator> additionalContentOperators
			)
			: this(eventListener)
		{
			foreach (var entry in additionalContentOperators)
			{
				RegisterContentOperator(entry.Key, entry.Value);
			}
		}

		/// <summary>Registers a Do handler that will be called when Do for the provided XObject subtype is encountered during content processing.
		///     </summary>
		/// <remarks>
		/// Registers a Do handler that will be called when Do for the provided XObject subtype is encountered during content processing.
		/// <br />
		/// If you register a handler, it is a very good idea to pass the call on to the existing registered handler (returned by this call), otherwise you
		/// may inadvertently change the internal behavior of the processor.
		/// </remarks>
		/// <param name="xobjectSubType">the XObject subtype this handler will process, or PdfName.DEFAULT for a catch-all handler
		///     </param>
		/// <param name="handler">the handler that will receive notification when the Do operator for the specified subtype is encountered
		///     </param>
		/// <returns>the existing registered handler, if any</returns>
		public virtual IXObjectDoHandler RegisterXObjectDoHandler(PdfName xobjectSubType, IXObjectDoHandler handler
			)
		{
			return xobjectDoHandlers.Put(xobjectSubType, handler);
		}

		/// <summary>Registers a content operator that will be called when the specified operator string is encountered during content processing.
		///     </summary>
		/// <remarks>
		/// Registers a content operator that will be called when the specified operator string is encountered during content processing.
		/// <br />
		/// If you register an operator, it is a very good idea to pass the call on to the existing registered operator (returned by this call), otherwise you
		/// may inadvertently change the internal behavior of the processor.
		/// </remarks>
		/// <param name="operatorString">the operator id, or DEFAULT_OPERATOR for a catch-all operator</param>
		/// <param name="operator">the operator that will receive notification when the operator is encountered</param>
		/// <returns>the existing registered operator, if any</returns>
		public virtual IContentOperator RegisterContentOperator(string operatorString, IContentOperator @operator)
		{
			return operators.Put(operatorString, @operator);
		}

		/// <summary>
		/// Gets the
		/// <see cref="ICollection"/>
		/// containing all the registered operators strings.
		/// </summary>
		/// <returns>
		/// 
		/// <see cref="ICollection"/>
		/// containing all the registered operators strings.
		/// </returns>
		public virtual ICollection<string> GetRegisteredOperatorStrings()
		{
			return new List<string>(operators.Keys);
		}

		/// <summary>Resets the graphics state stack, matrices and resources.</summary>
		public virtual void Reset()
		{
			gsStack.Clear();
			gsStack.Push(new ParserGraphicsState());
			textMatrix = null;
			textLineMatrix = null;
			resourcesStack = new Stack<PdfResources>();
			isClip = false;
			currentPath = new Path();
		}

		/// <summary>
		/// Gets the current
		/// <see cref="ParserGraphicsState"/>
		/// </summary>
		/// <returns>
		/// the current
		/// <see cref="ParserGraphicsState"/>
		/// </returns>
		public virtual ParserGraphicsState GetGraphicsState()
		{
			return gsStack.Peek();
		}

		/// <summary>Processes PDF syntax.</summary>
		/// <remarks>
		/// Processes PDF syntax.
		/// <b>Note:</b> If you re-use a given
		/// <see cref="PdfCanvasProcessor"/>
		/// , you must call
		/// <see cref="Reset()"/>
		/// </remarks>
		/// <param name="contentBytes">the bytes of a content stream</param>
		/// <param name="resources">the resources of the content stream. Must not be null.</param>
		public virtual void ProcessContent(byte[] contentBytes, PdfResources resources)
		{
			if (resources == null)
			{
				throw new PdfException(PdfException.ResourcesCannotBeNull);
			}
			resourcesStack.Push(resources);
			var tokeniser = new PdfTokenizer(new RandomAccessFileOrArray(new RandomAccessSourceFactory().CreateSource
				(contentBytes)));
			var ps = new PdfCanvasParser(tokeniser, resources);
			IList<PdfObject> operands = new List<PdfObject>();
			try
			{
				while (ps.Parse(operands).Count > 0)
				{
					var @operator = (PdfLiteral)operands[operands.Count - 1];
					InvokeOperator(@operator, operands);
				}
			}
			catch (IOException e)
			{
				throw new PdfException(PdfException.CannotParseContentStream, e);
			}
			resourcesStack.Pop();
		}

		/// <summary>Processes PDF syntax.</summary>
		/// <remarks>
		/// Processes PDF syntax.
		/// <strong>Note:</strong> If you re-use a given
		/// <see cref="PdfCanvasProcessor"/>
		/// , you must call
		/// <see cref="Reset()"/>
		/// </remarks>
		/// <param name="page">the page to process</param>
		public virtual void ProcessPageContent(PdfPage page)
		{
			InitClippingPath(page);
			var gs = GetGraphicsState();
			EventOccurred(new ClippingPathInfo(gs, gs.GetClippingPath(), gs.GetCtm()), EventType.CLIP_PATH_CHANGED);
			ProcessContent(page.GetContentBytes(), page.GetResources());
		}

		/// <summary>
		/// Accessor method for the
		/// <see cref="IEventListener"/>
		/// object maintained in this class.
		/// </summary>
		/// <remarks>
		/// Accessor method for the
		/// <see cref="IEventListener"/>
		/// object maintained in this class.
		/// Necessary for implementing custom ContentOperator implementations.
		/// </remarks>
		/// <returns>the renderListener</returns>
		public virtual IEventListener GetEventListener()
		{
			return eventListener;
		}

		/// <summary>Loads all the supported graphics and text state operators in a map.</summary>
		protected internal virtual void PopulateOperators()
		{
			RegisterContentOperator(DEFAULT_OPERATOR, new IgnoreOperator());
			RegisterContentOperator("q", new PushGraphicsStateOperator());
			RegisterContentOperator("Q", new PopGraphicsStateOperator());
			RegisterContentOperator("cm", new ModifyCurrentTransformationMatrixOperator());
			RegisterContentOperator("Do", new DoOperator());
			RegisterContentOperator("BMC", new BeginMarkedContentOperator());
			RegisterContentOperator("BDC", new BeginMarkedContentDictionaryOperator());
			RegisterContentOperator("EMC", new EndMarkedContentOperator());
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_TEXT) || supportedEvents.Contains
				(EventType.RENDER_PATH) || supportedEvents.Contains(EventType.CLIP_PATH_CHANGED))
			{
				RegisterContentOperator("g", new SetGrayFillOperator());
				RegisterContentOperator("G", new SetGrayStrokeOperator());
				RegisterContentOperator("rg", new SetRGBFillOperator());
				RegisterContentOperator("RG", new SetRGBStrokeOperator());
				RegisterContentOperator("k", new SetCMYKFillOperator());
				RegisterContentOperator("K", new SetCMYKStrokeOperator());
				RegisterContentOperator("cs", new SetColorSpaceFillOperator());
				RegisterContentOperator("CS", new SetColorSpaceStrokeOperator());
				RegisterContentOperator("sc", new SetColorFillOperator());
				RegisterContentOperator("SC", new SetColorStrokeOperator());
				RegisterContentOperator("scn", new SetColorFillOperator());
				RegisterContentOperator("SCN", new SetColorStrokeOperator());
				RegisterContentOperator("gs", new ProcessGraphicsStateResourceOperator());
			}
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_IMAGE))
			{
				RegisterContentOperator("EI", new EndImageOperator());
			}
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_TEXT) || supportedEvents.Contains
				(EventType.BEGIN_TEXT) || supportedEvents.Contains(EventType.END_TEXT))
			{
				RegisterContentOperator("BT", new BeginTextOperator());
				RegisterContentOperator("ET", new EndTextOperator());
			}
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_TEXT))
			{
				var tcOperator = new SetTextCharacterSpacingOperator
					();
				RegisterContentOperator("Tc", tcOperator);
				var twOperator = new SetTextWordSpacingOperator
					();
				RegisterContentOperator("Tw", twOperator);
				RegisterContentOperator("Tz", new SetTextHorizontalScalingOperator());
				var tlOperator = new SetTextLeadingOperator();
				RegisterContentOperator("TL", tlOperator);
				RegisterContentOperator("Tf", new SetTextFontOperator());
				RegisterContentOperator("Tr", new SetTextRenderModeOperator());
				RegisterContentOperator("Ts", new SetTextRiseOperator());
				var tdOperator = new TextMoveStartNextLineOperator
					();
				RegisterContentOperator("Td", tdOperator);
				RegisterContentOperator("TD", new TextMoveStartNextLineWithLeadingOperator(tdOperator,
					tlOperator));
				RegisterContentOperator("Tm", new TextSetTextMatrixOperator());
				var tstarOperator = new TextMoveNextLineOperator
					(tdOperator);
				RegisterContentOperator("T*", tstarOperator);
				var tjOperator = new ShowTextOperator();
				RegisterContentOperator("Tj", tjOperator);
				var tickOperator = new MoveNextLineAndShowTextOperator
					(tstarOperator, tjOperator);
				RegisterContentOperator("'", tickOperator);
				RegisterContentOperator("\"", new MoveNextLineAndShowTextWithSpacingOperator(twOperator
					, tcOperator, tickOperator));
				RegisterContentOperator("TJ", new ShowTextArrayOperator());
			}
			if (supportedEvents == null || supportedEvents.Contains(EventType.CLIP_PATH_CHANGED) || supportedEvents.Contains
				(EventType.RENDER_PATH))
			{
				RegisterContentOperator("w", new SetLineWidthOperator());
				RegisterContentOperator("J", new SetLineCapOperator());
				RegisterContentOperator("j", new SetLineJoinOperator());
				RegisterContentOperator("M", new SetMiterLimitOperator());
				RegisterContentOperator("d", new SetLineDashPatternOperator());
				var fillStroke = PathRenderInfo.FILL | PathRenderInfo.STROKE;
				RegisterContentOperator("m", new MoveToOperator());
				RegisterContentOperator("l", new LineToOperator());
				RegisterContentOperator("c", new CurveOperator());
				RegisterContentOperator("v", new CurveFirstPointDuplicatedOperator());
				RegisterContentOperator("y", new CurveFourhPointDuplicatedOperator());
				RegisterContentOperator("h", new CloseSubpathOperator());
				RegisterContentOperator("re", new RectangleOperator());
				RegisterContentOperator("S", new PaintPathOperator(PathRenderInfo.STROKE, -1, false));
				RegisterContentOperator("s", new PaintPathOperator(PathRenderInfo.STROKE, -1, true));
				RegisterContentOperator("f", new PaintPathOperator(PathRenderInfo.FILL, PdfCanvasConstants.FillingRule
					.NONZERO_WINDING, false));
				RegisterContentOperator("F", new PaintPathOperator(PathRenderInfo.FILL, PdfCanvasConstants.FillingRule
					.NONZERO_WINDING, false));
				RegisterContentOperator("f*", new PaintPathOperator(PathRenderInfo.FILL, PdfCanvasConstants.FillingRule
					.EVEN_ODD, false));
				RegisterContentOperator("B", new PaintPathOperator(fillStroke, PdfCanvasConstants.FillingRule
					.NONZERO_WINDING, false));
				RegisterContentOperator("B*", new PaintPathOperator(fillStroke, PdfCanvasConstants.FillingRule
					.EVEN_ODD, false));
				RegisterContentOperator("b", new PaintPathOperator(fillStroke, PdfCanvasConstants.FillingRule
					.NONZERO_WINDING, true));
				RegisterContentOperator("b*", new PaintPathOperator(fillStroke, PdfCanvasConstants.FillingRule
					.EVEN_ODD, true));
				RegisterContentOperator("n", new PaintPathOperator(PathRenderInfo.NO_OP, -1, false));
				RegisterContentOperator("W", new ClipPathOperator(PdfCanvasConstants.FillingRule.NONZERO_WINDING
					));
				RegisterContentOperator("W*", new ClipPathOperator(PdfCanvasConstants.FillingRule.EVEN_ODD
					));
			}
		}

		/// <summary>Displays the current path.</summary>
		/// <param name="operation">
		/// One of the possible combinations of
		/// <see cref="PathRenderInfo.STROKE"/>
		/// and
		/// <see cref="PathRenderInfo.FILL"/>
		/// values or
		/// <see cref="PathRenderInfo.NO_OP"/>
		/// </param>
		/// <param name="rule">
		/// Either
		/// <see cref="PdfCanvasConstants.FillingRule.NONZERO_WINDING"/>
		/// or
		/// <see cref="PdfCanvasConstants.FillingRule.EVEN_ODD"/>
		/// In case it isn't applicable pass any <c>byte</c> value.
		/// </param>
		protected internal virtual void PaintPath(int operation, int rule)
		{
			var gs = GetGraphicsState();
			var renderInfo = new PathRenderInfo(markedContentStack, gs, currentPath, operation, rule,
				isClip, clippingRule);
			EventOccurred(renderInfo, EventType.RENDER_PATH);
			if (isClip)
			{
				isClip = false;
				gs.Clip(currentPath, clippingRule);
				EventOccurred(new ClippingPathInfo(gs, gs.GetClippingPath(), gs.GetCtm()), EventType.CLIP_PATH_CHANGED);
			}
			currentPath = new Path();
		}

		/// <summary>Invokes an operator.</summary>
		/// <param name="operator">the PDF Syntax of the operator</param>
		/// <param name="operands">a list with operands</param>
		protected internal virtual void InvokeOperator(PdfLiteral @operator, IList<PdfObject> operands)
		{
			var op = operators.Get(@operator.ToString());
			if (op == null)
			{
				op = operators.Get(DEFAULT_OPERATOR);
			}
			op.Invoke(this, @operator, operands);
		}

		protected internal virtual PdfStream GetXObjectStream(PdfName xobjectName)
		{
			var xobjects = GetResources().GetResource(PdfName.XObject);
			return xobjects.GetAsStream(xobjectName);
		}

		protected internal virtual PdfResources GetResources()
		{
			return resourcesStack.Peek();
		}

		protected internal virtual void PopulateXObjectDoHandlers()
		{
			RegisterXObjectDoHandler(PdfName.Default, new IgnoreXObjectDoHandler());
			RegisterXObjectDoHandler(PdfName.Form, new FormXObjectDoHandler());
			if (supportedEvents == null || supportedEvents.Contains(EventType.RENDER_IMAGE))
			{
				RegisterXObjectDoHandler(PdfName.Image, new ImageXObjectDoHandler());
			}
		}

		/// <summary>
		/// Creates a
		/// <see cref="PdfFont"/>
		/// object by a font dictionary.
		/// </summary>
		/// <remarks>
		/// Creates a
		/// <see cref="PdfFont"/>
		/// object by a font dictionary. The font may have been cached in case
		/// it is an indirect object.
		/// </remarks>
		/// <param name="fontDict">
		/// the
		/// <see cref="PdfDictionary">font dictionary</see>
		/// to create the font from
		/// </param>
		/// <returns>the created font</returns>
		protected internal virtual PdfFont GetFont(PdfDictionary fontDict)
		{
			if (fontDict.GetIndirectReference() == null)
			{
				return PdfFontFactory.CreateFont(fontDict);
			}

			var n = fontDict.GetIndirectReference().GetObjNumber();
			var fontRef = cachedFonts.Get(n);
			var font = (PdfFont)fontRef?.Target;
			if (font == null)
			{
				font = PdfFontFactory.CreateFont(fontDict);
				cachedFonts.Put(n, new WeakReference(font));
			}
			return font;
		}

		/// <summary>Add to the marked content stack</summary>
		/// <param name="tag">the tag of the marked content</param>
		/// <param name="dict">the PdfDictionary associated with the marked content</param>
		protected internal virtual void BeginMarkedContent(PdfName tag, PdfDictionary dict)
		{
			markedContentStack.Push(new CanvasTag(tag).SetProperties(dict));
		}

		/// <summary>Remove the latest marked content from the stack.</summary>
		/// <remarks>Remove the latest marked content from the stack.  Keeps track of the BMC, BDC and EMC operators.</remarks>
		protected internal virtual void EndMarkedContent()
		{
			markedContentStack.Pop();
		}

		/// <summary>Used to trigger beginTextBlock on the renderListener</summary>
		private void BeginText()
		{
			EventOccurred(null, EventType.BEGIN_TEXT);
		}

		/// <summary>Used to trigger endTextBlock on the renderListener</summary>
		private void EndText()
		{
			EventOccurred(null, EventType.END_TEXT);
		}

		/// <summary>This is a proxy to pass only those events to the event listener which are supported by it.</summary>
		/// <param name="data">event data</param>
		/// <param name="type">event type</param>
		protected internal virtual void EventOccurred(IEventData data, EventType type)
		{
			if (supportedEvents == null || supportedEvents.Contains(type))
			{
				eventListener.EventOccurred(data, type);
			}
			if (data is AbstractRenderInfo)
			{
				((AbstractRenderInfo)data).ReleaseGraphicsState();
			}
		}

		/// <summary>Displays text.</summary>
		/// <param name="string">the text to display</param>
		private void DisplayPdfString(PdfString @string)
		{
			var renderInfo = new TextRenderInfo(@string, GetGraphicsState(), textMatrix, markedContentStack
				);
			textMatrix = new Matrix(renderInfo.GetUnscaledWidth(), 0).Multiply(textMatrix);
			EventOccurred(renderInfo, EventType.RENDER_TEXT);
		}

		/// <summary>Displays an XObject using the registered handler for this XObject's subtype</summary>
		/// <param name="resourceName">the name of the XObject to retrieve from the resource dictionary</param>
		private void DisplayXObject(PdfName resourceName)
		{
			var xobjectStream = GetXObjectStream(resourceName);
			var subType = xobjectStream.GetAsName(PdfName.Subtype);
			var handler = xobjectDoHandlers.Get(subType);
			if (handler == null)
			{
				handler = xobjectDoHandlers.Get(PdfName.Default);
			}
			handler.HandleXObject(this, markedContentStack, xobjectStream, resourceName);
		}

		private void DisplayImage(Stack<CanvasTag> canvasTagHierarchy, PdfStream imageStream, PdfName resourceName
			, bool isInline)
		{
			var colorSpaceDic = GetResources().GetResource(PdfName.ColorSpace);
			var renderInfo = new ImageRenderInfo(canvasTagHierarchy, GetGraphicsState(), GetGraphicsState(
				).GetCtm(), imageStream, resourceName, colorSpaceDic, isInline);
			EventOccurred(renderInfo, EventType.RENDER_IMAGE);
		}

		/// <summary>Adjusts the text matrix for the specified adjustment value (see TJ operator in the PDF spec for information)
		///     </summary>
		/// <param name="tj">the text adjustment</param>
		private void ApplyTextAdjust(float tj)
		{
			var adjustBy = -tj / 1000f * GetGraphicsState().GetFontSize() * (GetGraphicsState().GetHorizontalScaling
				() / 100f);
			textMatrix = new Matrix(adjustBy, 0).Multiply(textMatrix);
		}

		private void InitClippingPath(PdfPage page)
		{
			var clippingPath = new Path();
			clippingPath.Rectangle(page.GetCropBox());
			GetGraphicsState().SetClippingPath(clippingPath);
		}

		/// <summary>A handler that implements operator (unregistered).</summary>
		private class IgnoreOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
			}
			// ignore the operator
		}

		/// <summary>A handler that implements operator (TJ).</summary>
		/// <remarks>A handler that implements operator (TJ). For more information see Table 51 ISO-32000-1</remarks>
		private class ShowTextArrayOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var array = (PdfArray)operands[0];
				float tj = 0;
				foreach (var entryObj in array)
				{
					if (entryObj is PdfString)
					{
						processor.DisplayPdfString((PdfString)entryObj);
						tj = 0;
					}
					else
					{
						tj = ((PdfNumber)entryObj).FloatValue();
						processor.ApplyTextAdjust(tj);
					}
				}
			}
		}

		/// <summary>A handler that implements operator (").</summary>
		/// <remarks>A handler that implements operator ("). For more information see Table 51 ISO-32000-1</remarks>
		private class MoveNextLineAndShowTextWithSpacingOperator : IContentOperator
		{
			private readonly SetTextWordSpacingOperator setTextWordSpacing;

			private readonly SetTextCharacterSpacingOperator setTextCharacterSpacing;

			private readonly MoveNextLineAndShowTextOperator moveNextLineAndShowText;

			/// <summary>Create new instance of this handler.</summary>
			/// <param name="setTextWordSpacing">the handler for Tw operator</param>
			/// <param name="setTextCharacterSpacing">the handler for Tc operator</param>
			/// <param name="moveNextLineAndShowText">the handler for ' operator</param>
			public MoveNextLineAndShowTextWithSpacingOperator(SetTextWordSpacingOperator setTextWordSpacing
				, SetTextCharacterSpacingOperator setTextCharacterSpacing, MoveNextLineAndShowTextOperator
				 moveNextLineAndShowText)
			{
				this.setTextWordSpacing = setTextWordSpacing;
				this.setTextCharacterSpacing = setTextCharacterSpacing;
				this.moveNextLineAndShowText = moveNextLineAndShowText;
			}

			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var aw = (PdfNumber)operands[0];
				var ac = (PdfNumber)operands[1];
				var @string = (PdfString)operands[2];
				IList<PdfObject> twOperands = new List<PdfObject>(1);
				twOperands.Add(0, aw);
				setTextWordSpacing.Invoke(processor, null, twOperands);
				IList<PdfObject> tcOperands = new List<PdfObject>(1);
				tcOperands.Add(0, ac);
				setTextCharacterSpacing.Invoke(processor, null, tcOperands);
				IList<PdfObject> tickOperands = new List<PdfObject>(1);
				tickOperands.Add(0, @string);
				moveNextLineAndShowText.Invoke(processor, null, tickOperands);
			}
		}

		/// <summary>A handler that implements operator (').</summary>
		/// <remarks>A handler that implements operator ('). For more information see Table 51 ISO-32000-1</remarks>
		private class MoveNextLineAndShowTextOperator : IContentOperator
		{
			private readonly TextMoveNextLineOperator textMoveNextLine;

			private readonly ShowTextOperator showText;

			/// <summary>Creates the new instance of this handler</summary>
			/// <param name="textMoveNextLine">the handler for T* operator</param>
			/// <param name="showText">the handler for Tj operator</param>
			public MoveNextLineAndShowTextOperator(TextMoveNextLineOperator textMoveNextLine, ShowTextOperator
				 showText)
			{
				this.textMoveNextLine = textMoveNextLine;
				this.showText = showText;
			}

			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				textMoveNextLine.Invoke(processor, null, new List<PdfObject>(0));
				showText.Invoke(processor, null, operands);
			}
		}

		/// <summary>A handler that implements operator (Tj).</summary>
		/// <remarks>A handler that implements operator (Tj). For more information see Table 51 ISO-32000-1</remarks>
		private class ShowTextOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var @string = (PdfString)operands[0];
				processor.DisplayPdfString(@string);
			}
		}

		/// <summary>A handler that implements operator (T*).</summary>
		/// <remarks>A handler that implements operator (T*). For more information see Table 51 ISO-32000-1</remarks>
		private class TextMoveNextLineOperator : IContentOperator
		{
			private readonly TextMoveStartNextLineOperator moveStartNextLine;

			public TextMoveNextLineOperator(TextMoveStartNextLineOperator moveStartNextLine)
			{
				this.moveStartNextLine = moveStartNextLine;
			}

			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				IList<PdfObject> tdoperands = new List<PdfObject>(2);
				tdoperands.Add(0, new PdfNumber(0));
				tdoperands.Add(1, new PdfNumber(-processor.GetGraphicsState().GetLeading()));
				moveStartNextLine.Invoke(processor, null, tdoperands);
			}
		}

		/// <summary>A handler that implements operator (Tm).</summary>
		/// <remarks>A handler that implements operator (Tm). For more information see Table 51 ISO-32000-1</remarks>
		private class TextSetTextMatrixOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var a = ((PdfNumber)operands[0]).FloatValue();
				var b = ((PdfNumber)operands[1]).FloatValue();
				var c = ((PdfNumber)operands[2]).FloatValue();
				var d = ((PdfNumber)operands[3]).FloatValue();
				var e = ((PdfNumber)operands[4]).FloatValue();
				var f = ((PdfNumber)operands[5]).FloatValue();
				processor.textLineMatrix = new Matrix(a, b, c, d, e, f);
				processor.textMatrix = processor.textLineMatrix;
			}
		}

		/// <summary>A handler that implements operator (TD).</summary>
		/// <remarks>A handler that implements operator (TD). For more information see Table 51 ISO-32000-1</remarks>
		private class TextMoveStartNextLineWithLeadingOperator : IContentOperator
		{
			private readonly TextMoveStartNextLineOperator moveStartNextLine;

			private readonly SetTextLeadingOperator setTextLeading;

			public TextMoveStartNextLineWithLeadingOperator(TextMoveStartNextLineOperator moveStartNextLine
				, SetTextLeadingOperator setTextLeading)
			{
				this.moveStartNextLine = moveStartNextLine;
				this.setTextLeading = setTextLeading;
			}

			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var ty = ((PdfNumber)operands[1]).FloatValue();
				IList<PdfObject> tlOperands = new List<PdfObject>(1);
				tlOperands.Add(0, new PdfNumber(-ty));
				setTextLeading.Invoke(processor, null, tlOperands);
				moveStartNextLine.Invoke(processor, null, operands);
			}
		}

		/// <summary>A handler that implements operator (Td).</summary>
		/// <remarks>A handler that implements operator (Td). For more information see Table 51 ISO-32000-1</remarks>
		private class TextMoveStartNextLineOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var tx = ((PdfNumber)operands[0]).FloatValue();
				var ty = ((PdfNumber)operands[1]).FloatValue();
				var translationMatrix = new Matrix(tx, ty);
				processor.textMatrix = translationMatrix.Multiply(processor.textLineMatrix);
				processor.textLineMatrix = processor.textMatrix;
			}
		}

		/// <summary>A handler that implements operator (Tf).</summary>
		/// <remarks>A handler that implements operator (Tf). For more information see Table 51 ISO-32000-1</remarks>
		private class SetTextFontOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var fontResourceName = (PdfName)operands[0];
				var size = ((PdfNumber)operands[1]).FloatValue();
				var fontsDictionary = processor.GetResources().GetResource(PdfName.Font);
				var fontDict = fontsDictionary.GetAsDictionary(fontResourceName);
				var font = processor.GetFont(fontDict);
				processor.GetGraphicsState().SetFont(font);
				processor.GetGraphicsState().SetFontSize(size);
			}
		}

		/// <summary>A handler that implements operator (Tr).</summary>
		/// <remarks>A handler that implements operator (Tr). For more information see Table 51 ISO-32000-1</remarks>
		private class SetTextRenderModeOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var render = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetTextRenderingMode(render.IntValue());
			}
		}

		/// <summary>A handler that implements operator (Ts).</summary>
		/// <remarks>A handler that implements operator (Ts). For more information see Table 51 ISO-32000-1</remarks>
		private class SetTextRiseOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var rise = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetTextRise(rise.FloatValue());
			}
		}

		/// <summary>A handler that implements operator (TL).</summary>
		/// <remarks>A handler that implements operator (TL). For more information see Table 51 ISO-32000-1</remarks>
		private class SetTextLeadingOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var leading = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetLeading(leading.FloatValue());
			}
		}

		/// <summary>A handler that implements operator (Tz).</summary>
		/// <remarks>A handler that implements operator (Tz). For more information see Table 51 ISO-32000-1</remarks>
		private class SetTextHorizontalScalingOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var scale = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetHorizontalScaling(scale.FloatValue());
			}
		}

		/// <summary>A handler that implements operator (Tc).</summary>
		/// <remarks>A handler that implements operator (Tc). For more information see Table 51 ISO-32000-1</remarks>
		private class SetTextCharacterSpacingOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var charSpace = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetCharSpacing(charSpace.FloatValue());
			}
		}

		/// <summary>A handler that implements operator (Tw).</summary>
		/// <remarks>A handler that implements operator (Tw). For more information see Table 51 ISO-32000-1</remarks>
		private class SetTextWordSpacingOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var wordSpace = (PdfNumber)operands[0];
				processor.GetGraphicsState().SetWordSpacing(wordSpace.FloatValue());
			}
		}

		/// <summary>A handler that implements operator (gs).</summary>
		/// <remarks>A handler that implements operator (gs). For more information see Table 51 ISO-32000-1</remarks>
		private class ProcessGraphicsStateResourceOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var dictionaryName = (PdfName)operands[0];
				var extGState = processor.GetResources().GetResource(PdfName.ExtGState);
				if (extGState == null)
				{
					throw new PdfException(PdfException.ResourcesDoNotContainExtgstateEntryUnableToProcessOperator1).SetMessageParams
						(@operator);
				}
				var gsDic = extGState.GetAsDictionary(dictionaryName);
				if (gsDic == null)
				{
					gsDic = extGState.GetAsStream(dictionaryName);
					if (gsDic == null)
					{
						throw new PdfException(PdfException._1IsAnUnknownGraphicsStateDictionary).SetMessageParams(dictionaryName);
					}
				}
				var fontParameter = gsDic.GetAsArray(PdfName.Font);
				if (fontParameter != null)
				{
					var font = processor.GetFont(fontParameter.GetAsDictionary(0));
					var size = fontParameter.GetAsNumber(1).FloatValue();
					processor.GetGraphicsState().SetFont(font);
					processor.GetGraphicsState().SetFontSize(size);
				}
				var pdfExtGState = new PdfExtGState(gsDic.Clone(JavaCollectionsUtil.SingletonList(PdfName.Font)));
				processor.GetGraphicsState().UpdateFromExtGState(pdfExtGState);
			}
		}

		/// <summary>A handler that implements operator (q).</summary>
		/// <remarks>A handler that implements operator (q). For more information see Table 51 ISO-32000-1</remarks>
		private class PushGraphicsStateOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var gs = processor.gsStack.Peek();
				var copy = new ParserGraphicsState(gs);
				processor.gsStack.Push(copy);
			}
		}

		/// <summary>A handler that implements operator (cm).</summary>
		/// <remarks>A handler that implements operator (cm). For more information see Table 51 ISO-32000-1</remarks>
		private class ModifyCurrentTransformationMatrixOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var a = ((PdfNumber)operands[0]).FloatValue();
				var b = ((PdfNumber)operands[1]).FloatValue();
				var c = ((PdfNumber)operands[2]).FloatValue();
				var d = ((PdfNumber)operands[3]).FloatValue();
				var e = ((PdfNumber)operands[4]).FloatValue();
				var f = ((PdfNumber)operands[5]).FloatValue();
				var matrix = new Matrix(a, b, c, d, e, f);
				try
				{
					processor.GetGraphicsState().UpdateCtm(matrix);
				}
				catch (PdfException exception)
				{
					if (!(exception.InnerException is NoninvertibleTransformException))
					{
						throw;
					}

					var logger = LogManager.GetLogger(typeof(PdfCanvasProcessor));
					logger.Error(MessageFormatUtil.Format(LogMessageConstant.FAILED_TO_PROCESS_A_TRANSFORMATION_MATRIX
					));
				}
			}
		}

		/// <summary>Gets a color based on a list of operands and Color space.</summary>
		private static Color GetColor(PdfColorSpace pdfColorSpace, IList<PdfObject> operands, PdfResources resources
			)
		{
			PdfObject pdfObject;
			if (pdfColorSpace.GetPdfObject().IsIndirectReference())
			{
				pdfObject = ((PdfIndirectReference)pdfColorSpace.GetPdfObject()).GetRefersTo();
			}
			else
			{
				pdfObject = pdfColorSpace.GetPdfObject();
			}
			if (pdfObject.IsName())
			{
				if (PdfName.DeviceGray.Equals(pdfObject))
				{
					return new DeviceGray(GetColorants(operands)[0]);
				}

				if (PdfName.Pattern.Equals(pdfObject))
				{
					if (operands[0] is PdfName)
					{
						var pattern = resources.GetPattern((PdfName)operands[0]);
						if (pattern != null)
						{
							return new PatternColor(pattern);
						}
					}
				}
				if (PdfName.DeviceRGB.Equals(pdfObject))
				{
					var c = GetColorants(operands);
					return new DeviceRgb(c[0], c[1], c[2]);
				}

				if (PdfName.DeviceCMYK.Equals(pdfObject))
				{
					var c = GetColorants(operands);
					return new DeviceCmyk(c[0], c[1], c[2], c[3]);
				}
			}
			else
			{
				if (pdfObject.IsArray())
				{
					var array = (PdfArray)pdfObject;
					var csType = array.GetAsName(0);
					if (PdfName.CalGray.Equals(csType))
					{
						return new CalGray((PdfCieBasedCs.CalGray)pdfColorSpace, GetColorants(operands)[0]);
					}

					if (PdfName.CalRGB.Equals(csType))
					{
						return new CalRgb((PdfCieBasedCs.CalRgb)pdfColorSpace, GetColorants(operands));
					}

					if (PdfName.Lab.Equals(csType))
					{
						return new Lab((PdfCieBasedCs.Lab)pdfColorSpace, GetColorants(operands));
					}

					if (PdfName.ICCBased.Equals(csType))
					{
						return new IccBased((PdfCieBasedCs.IccBased)pdfColorSpace, GetColorants(operands));
					}

					if (PdfName.Indexed.Equals(csType))
					{
						return new Indexed(pdfColorSpace, (int)GetColorants(operands)[0]);
					}

					if (PdfName.Separation.Equals(csType))
					{
						return new Separation((PdfSpecialCs.Separation)pdfColorSpace, GetColorants(operands)[0]);
					}

					if (PdfName.DeviceN.Equals(csType))
					{
						return new DeviceN((PdfSpecialCs.DeviceN)pdfColorSpace, GetColorants(operands));
					}

					if (PdfName.Pattern.Equals(csType))
					{
						IList<PdfObject> underlyingOperands = new List<PdfObject>(operands);
						var patternName = underlyingOperands.JRemoveAt(operands.Count - 2);
						var underlyingCs = ((PdfSpecialCs.UncoloredTilingPattern)pdfColorSpace).GetUnderlyingColorSpace(
						);
						if (patternName is PdfName)
						{
							var pattern = resources.GetPattern((PdfName)patternName);
							if (pattern is PdfPattern.Tiling && !((PdfPattern.Tiling)pattern).IsColored())
							{
								return new PatternColor((PdfPattern.Tiling)pattern, underlyingCs, GetColorants(underlyingOperands));
							}
						}
					}
				}
			}
			var logger = LogManager.GetLogger(typeof(PdfCanvasProcessor));
			logger.Warn(MessageFormatUtil.Format(KernelLogMessageConstant.UNABLE_TO_PARSE_COLOR_WITHIN_COLORSPACE, JavaUtil.ArraysToString
				((object[])operands.ToArray()), pdfColorSpace.GetPdfObject()));
			return null;
		}

		/// <summary>Gets a color based on a list of operands.</summary>
		private static Color GetColor(int nOperands, IList<PdfObject> operands)
		{
			var c = new float[nOperands];
			for (var i = 0; i < nOperands; i++)
			{
				c[i] = ((PdfNumber)operands[i]).FloatValue();
			}
			switch (nOperands)
			{
				case 1:
					{
						return new DeviceGray(c[0]);
					}

				case 3:
					{
						return new DeviceRgb(c[0], c[1], c[2]);
					}

				case 4:
					{
						return new DeviceCmyk(c[0], c[1], c[2], c[3]);
					}
			}
			return null;
		}

		private static float[] GetColorants(IList<PdfObject> operands)
		{
			var c = new float[operands.Count - 1];
			for (var i = 0; i < operands.Count - 1; i++)
			{
				c[i] = ((PdfNumber)operands[i]).FloatValue();
			}
			return c;
		}

		/// <summary>A handler that implements operator (Q).</summary>
		/// <remarks>A handler that implements operator (Q). For more information see Table 51 ISO-32000-1</remarks>
		protected internal class PopGraphicsStateOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.gsStack.Pop();
				var gs = processor.GetGraphicsState();
				processor.EventOccurred(new ClippingPathInfo(gs, gs.GetClippingPath(), gs.GetCtm()), EventType.CLIP_PATH_CHANGED
					);
			}
		}

		/// <summary>A handler that implements operator (g).</summary>
		/// <remarks>A handler that implements operator (g). For more information see Table 51 ISO-32000-1</remarks>
		private class SetGrayFillOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.GetGraphicsState().SetFillColor(GetColor(1, operands));
			}
		}

		/// <summary>A handler that implements operator (G).</summary>
		/// <remarks>A handler that implements operator (G). For more information see Table 51 ISO-32000-1</remarks>
		private class SetGrayStrokeOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.GetGraphicsState().SetStrokeColor(GetColor(1, operands));
			}
		}

		/// <summary>A handler that implements operator (rg).</summary>
		/// <remarks>A handler that implements operator (rg). For more information see Table 51 ISO-32000-1</remarks>
		private class SetRGBFillOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.GetGraphicsState().SetFillColor(GetColor(3, operands));
			}
		}

		/// <summary>A handler that implements operator (RG).</summary>
		/// <remarks>A handler that implements operator (RG). For more information see Table 51 ISO-32000-1</remarks>
		private class SetRGBStrokeOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.GetGraphicsState().SetStrokeColor(GetColor(3, operands));
			}
		}

		/// <summary>A handler that implements operator (k).</summary>
		/// <remarks>A handler that implements operator (k). For more information see Table 51 ISO-32000-1</remarks>
		private class SetCMYKFillOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.GetGraphicsState().SetFillColor(GetColor(4, operands));
			}
		}

		/// <summary>A handler that implements operator (K).</summary>
		/// <remarks>A handler that implements operator (K). For more information see Table 51 ISO-32000-1</remarks>
		private class SetCMYKStrokeOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.GetGraphicsState().SetStrokeColor(GetColor(4, operands));
			}
		}

		/// <summary>A handler that implements operator (CS).</summary>
		/// <remarks>A handler that implements operator (CS). For more information see Table 51 ISO-32000-1</remarks>
		private class SetColorSpaceFillOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var pdfColorSpace = DetermineColorSpace((PdfName)operands[0], processor);
				processor.GetGraphicsState().SetFillColor(Color.MakeColor(pdfColorSpace));
			}

			internal static PdfColorSpace DetermineColorSpace(PdfName colorSpace, PdfCanvasProcessor processor)
			{
				PdfColorSpace pdfColorSpace = null;
				if (PdfColorSpace.directColorSpaces.Contains(colorSpace))
				{
					pdfColorSpace = PdfColorSpace.MakeColorSpace(colorSpace);
				}
				else
				{
					var pdfResources = processor.GetResources();
					var resourceColorSpace = pdfResources.GetPdfObject().GetAsDictionary(PdfName.ColorSpace);
					pdfColorSpace = PdfColorSpace.MakeColorSpace(resourceColorSpace.Get(colorSpace));
				}
				return pdfColorSpace;
			}
		}

		/// <summary>A handler that implements operator (cs).</summary>
		/// <remarks>A handler that implements operator (cs). For more information see Table 51 ISO-32000-1</remarks>
		private class SetColorSpaceStrokeOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var pdfColorSpace = SetColorSpaceFillOperator.DetermineColorSpace((PdfName)operands
					[0], processor);
				processor.GetGraphicsState().SetStrokeColor(Color.MakeColor(pdfColorSpace));
			}
		}

		/// <summary>A handler that implements operator (sc / scn).</summary>
		/// <remarks>A handler that implements operator (sc / scn). For more information see Table 51 ISO-32000-1</remarks>
		private class SetColorFillOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.GetGraphicsState().SetFillColor(GetColor(processor.GetGraphicsState().GetFillColor().GetColorSpace
					(), operands, processor.GetResources()));
			}
		}

		/// <summary>A handler that implements operator (SC / SCN).</summary>
		/// <remarks>A handler that implements operator (SC / SCN). For more information see Table 51 ISO-32000-1</remarks>
		private class SetColorStrokeOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.GetGraphicsState().SetStrokeColor(GetColor(processor.GetGraphicsState().GetStrokeColor().GetColorSpace
					(), operands, processor.GetResources()));
			}
		}

		/// <summary>A handler that implements operator (BT).</summary>
		/// <remarks>A handler that implements operator (BT). For more information see Table 51 ISO-32000-1</remarks>
		private class BeginTextOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.textMatrix = new Matrix();
				processor.textLineMatrix = processor.textMatrix;
				processor.BeginText();
			}
		}

		/// <summary>A handler that implements operator (ET).</summary>
		/// <remarks>A handler that implements operator (ET). For more information see Table 51 ISO-32000-1</remarks>
		private class EndTextOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.textMatrix = null;
				processor.textLineMatrix = null;
				processor.EndText();
			}
		}

		/// <summary>A handler that implements operator (BMC).</summary>
		/// <remarks>A handler that implements operator (BMC). For more information see Table 51 ISO-32000-1</remarks>
		private class BeginMarkedContentOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.BeginMarkedContent((PdfName)operands[0], null);
			}
		}

		/// <summary>A handler that implements operator (BDC).</summary>
		/// <remarks>A handler that implements operator (BDC). For more information see Table 51 ISO-32000-1</remarks>
		private class BeginMarkedContentDictionaryOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var properties = operands[1];
				processor.BeginMarkedContent((PdfName)operands[0], GetPropertiesDictionary(properties, processor.GetResources
					()));
			}

			internal virtual PdfDictionary GetPropertiesDictionary(PdfObject operand1, PdfResources resources)
			{
				if (operand1.IsDictionary())
				{
					return (PdfDictionary)operand1;
				}
				var dictionaryName = ((PdfName)operand1);
				var properties = resources.GetResource(PdfName.Properties);
				if (null == properties)
				{
					var logger = LogManager.GetLogger(typeof(PdfCanvasProcessor));
					logger.Warn(MessageFormatUtil.Format(LogMessageConstant.PDF_REFERS_TO_NOT_EXISTING_PROPERTY_DICTIONARY
						, PdfName.Properties));
					return null;
				}
				var propertiesDictionary = properties.GetAsDictionary(dictionaryName);
				if (null == propertiesDictionary)
				{
					var logger = LogManager.GetLogger(typeof(PdfCanvasProcessor));
					logger.Warn(MessageFormatUtil.Format(LogMessageConstant.PDF_REFERS_TO_NOT_EXISTING_PROPERTY_DICTIONARY
						, dictionaryName));
					return null;
				}
				return properties.GetAsDictionary(dictionaryName);
			}
		}

		/// <summary>A handler that implements operator (EMC).</summary>
		/// <remarks>A handler that implements operator (EMC). For more information see Table 51 ISO-32000-1</remarks>
		private class EndMarkedContentOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.EndMarkedContent();
			}
		}

		/// <summary>A handler that implements operator (Do).</summary>
		/// <remarks>A handler that implements operator (Do). For more information see Table 51 ISO-32000-1</remarks>
		private class DoOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var resourceName = (PdfName)operands[0];
				processor.DisplayXObject(resourceName);
			}
		}

		/// <summary>A handler that implements operator (EI).</summary>
		/// <remarks>
		/// A handler that implements operator (EI). For more information see Table 51 ISO-32000-1
		/// BI and ID operators are parsed along with this operator.
		/// This not a usual operator, it will have a single operand, which will be a PdfStream object which
		/// encapsulates inline image dictionary and bytes
		/// </remarks>
		private class EndImageOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var imageStream = (PdfStream)operands[0];
				processor.DisplayImage(processor.markedContentStack, imageStream, null, true);
			}
		}

		/// <summary>A handler that implements operator (w).</summary>
		/// <remarks>A handler that implements operator (w). For more information see Table 51 ISO-32000-1</remarks>
		private class SetLineWidthOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject> operands)
			{
				var lineWidth = ((PdfNumber)operands[0]).FloatValue();
				processor.GetGraphicsState().SetLineWidth(lineWidth);
			}
		}

		/// <summary>A handler that implements operator (J).</summary>
		/// <remarks>A handler that implements operator (J). For more information see Table 51 ISO-32000-1</remarks>
		private class SetLineCapOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject> operands)
			{
				var lineCap = ((PdfNumber)operands[0]).IntValue();
				processor.GetGraphicsState().SetLineCapStyle(lineCap);
			}
		}

		/// <summary>A handler that implements operator (j).</summary>
		/// <remarks>A handler that implements operator (j). For more information see Table 51 ISO-32000-1</remarks>
		private class SetLineJoinOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject> operands)
			{
				var lineJoin = ((PdfNumber)operands[0]).IntValue();
				processor.GetGraphicsState().SetLineJoinStyle(lineJoin);
			}
		}

		/// <summary>A handler that implements operator (M).</summary>
		/// <remarks>A handler that implements operator (M). For more information see Table 51 ISO-32000-1</remarks>
		private class SetMiterLimitOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject> operands)
			{
				var miterLimit = ((PdfNumber)operands[0]).FloatValue();
				processor.GetGraphicsState().SetMiterLimit(miterLimit);
			}
		}

		/// <summary>A handler that implements operator (d).</summary>
		/// <remarks>A handler that implements operator (d). For more information see Table 51 ISO-32000-1</remarks>
		private class SetLineDashPatternOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral oper, IList<PdfObject> operands)
			{
				processor.GetGraphicsState().SetDashPattern(new PdfArray(JavaUtil.ArraysAsList(operands[0], operands[1])));
			}
		}

		/// <summary>An XObject subtype handler for FORM</summary>
		private class FormXObjectDoHandler : IXObjectDoHandler
		{
			public virtual void HandleXObject(PdfCanvasProcessor processor, Stack<CanvasTag> canvasTagHierarchy, PdfStream
				 xObjectStream, PdfName xObjectName)
			{
				var resourcesDic = xObjectStream.GetAsDictionary(PdfName.Resources);
				PdfResources resources;
				if (resourcesDic == null)
				{
					resources = processor.GetResources();
				}
				else
				{
					resources = new PdfResources(resourcesDic);
				}
				// we read the content bytes up here so if it fails we don't leave the graphics state stack corrupted
				// this is probably not necessary (if we fail on this, probably the entire content stream processing
				// operation should be rejected
				byte[] contentBytes;
				contentBytes = xObjectStream.GetBytes();
				var matrix = xObjectStream.GetAsArray(PdfName.Matrix);
				new PushGraphicsStateOperator().Invoke(processor, null, null);
				if (matrix != null)
				{
					var a = matrix.GetAsNumber(0).FloatValue();
					var b = matrix.GetAsNumber(1).FloatValue();
					var c = matrix.GetAsNumber(2).FloatValue();
					var d = matrix.GetAsNumber(3).FloatValue();
					var e = matrix.GetAsNumber(4).FloatValue();
					var f = matrix.GetAsNumber(5).FloatValue();
					var formMatrix = new Matrix(a, b, c, d, e, f);
					processor.GetGraphicsState().UpdateCtm(formMatrix);
				}
				processor.ProcessContent(contentBytes, resources);
				new PopGraphicsStateOperator().Invoke(processor, null, null);
			}
		}

		/// <summary>An XObject subtype handler for IMAGE</summary>
		private class ImageXObjectDoHandler : IXObjectDoHandler
		{
			public virtual void HandleXObject(PdfCanvasProcessor processor, Stack<CanvasTag> canvasTagHierarchy, PdfStream
				 xObjectStream, PdfName resourceName)
			{
				processor.DisplayImage(canvasTagHierarchy, xObjectStream, resourceName, false);
			}
		}

		/// <summary>An XObject subtype handler that does nothing</summary>
		private class IgnoreXObjectDoHandler : IXObjectDoHandler
		{
			public virtual void HandleXObject(PdfCanvasProcessor processor, Stack<CanvasTag> canvasTagHierarchy, PdfStream
				 xObjectStream, PdfName xObjectName)
			{
			}
			// ignore XObject subtype
		}

		/// <summary>A handler that implements operator (m).</summary>
		/// <remarks>A handler that implements operator (m). For more information see Table 51 ISO-32000-1</remarks>
		private class MoveToOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var x = ((PdfNumber)operands[0]).FloatValue();
				var y = ((PdfNumber)operands[1]).FloatValue();
				processor.currentPath.MoveTo(x, y);
			}
		}

		/// <summary>A handler that implements operator (l).</summary>
		/// <remarks>A handler that implements operator (l). For more information see Table 51 ISO-32000-1</remarks>
		private class LineToOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var x = ((PdfNumber)operands[0]).FloatValue();
				var y = ((PdfNumber)operands[1]).FloatValue();
				processor.currentPath.LineTo(x, y);
			}
		}

		/// <summary>A handler that implements operator (c).</summary>
		/// <remarks>A handler that implements operator (c). For more information see Table 51 ISO-32000-1</remarks>
		private class CurveOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var x1 = ((PdfNumber)operands[0]).FloatValue();
				var y1 = ((PdfNumber)operands[1]).FloatValue();
				var x2 = ((PdfNumber)operands[2]).FloatValue();
				var y2 = ((PdfNumber)operands[3]).FloatValue();
				var x3 = ((PdfNumber)operands[4]).FloatValue();
				var y3 = ((PdfNumber)operands[5]).FloatValue();
				processor.currentPath.CurveTo(x1, y1, x2, y2, x3, y3);
			}
		}

		/// <summary>A handler that implements operator (v).</summary>
		/// <remarks>A handler that implements operator (v). For more information see Table 51 ISO-32000-1</remarks>
		private class CurveFirstPointDuplicatedOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var x2 = ((PdfNumber)operands[0]).FloatValue();
				var y2 = ((PdfNumber)operands[1]).FloatValue();
				var x3 = ((PdfNumber)operands[2]).FloatValue();
				var y3 = ((PdfNumber)operands[3]).FloatValue();
				processor.currentPath.CurveTo(x2, y2, x3, y3);
			}
		}

		/// <summary>A handler that implements operator (y).</summary>
		/// <remarks>A handler that implements operator (y). For more information see Table 51 ISO-32000-1</remarks>
		private class CurveFourhPointDuplicatedOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var x1 = ((PdfNumber)operands[0]).FloatValue();
				var y1 = ((PdfNumber)operands[1]).FloatValue();
				var x3 = ((PdfNumber)operands[2]).FloatValue();
				var y3 = ((PdfNumber)operands[3]).FloatValue();
				processor.currentPath.CurveFromTo(x1, y1, x3, y3);
			}
		}

		/// <summary>A handler that implements operator (h).</summary>
		/// <remarks>A handler that implements operator (h). For more information see Table 51 ISO-32000-1</remarks>
		private class CloseSubpathOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.currentPath.CloseSubpath();
			}
		}

		/// <summary>A handler that implements operator (re).</summary>
		/// <remarks>A handler that implements operator (re). For more information see Table 51 ISO-32000-1</remarks>
		private class RectangleOperator : IContentOperator
		{
			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				var x = ((PdfNumber)operands[0]).FloatValue();
				var y = ((PdfNumber)operands[1]).FloatValue();
				var w = ((PdfNumber)operands[2]).FloatValue();
				var h = ((PdfNumber)operands[3]).FloatValue();
				processor.currentPath.Rectangle(x, y, w, h);
			}
		}

		/// <summary>A handler that implements operator (S, s, f, F, f*, B, B*, b, b*).</summary>
		/// <remarks>A handler that implements operator (S, s, f, F, f*, B, B*, b, b*). For more information see Table 51 ISO-32000-1
		///     </remarks>
		private class PaintPathOperator : IContentOperator
		{
			private int operation;

			private int rule;

			private bool close;

			/// <summary>Constructs PainPath object.</summary>
			/// <param name="operation">
			/// One of the possible combinations of
			/// <see cref="PathRenderInfo.STROKE"/>
			/// and
			/// <see cref="PathRenderInfo.FILL"/>
			/// values or
			/// <see cref="PathRenderInfo.NO_OP"/>
			/// </param>
			/// <param name="rule">
			/// Either
			/// <see cref="PdfCanvasConstants.FillingRule.NONZERO_WINDING"/>
			/// or
			/// <see cref="PdfCanvasConstants.FillingRule.EVEN_ODD"/>
			/// In case it isn't applicable pass any value.
			/// </param>
			/// <param name="close">Indicates whether the path should be closed or not.</param>
			public PaintPathOperator(int operation, int rule, bool close)
			{
				this.operation = operation;
				this.rule = rule;
				this.close = close;
			}

			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				if (close)
				{
					processor.currentPath.CloseSubpath();
				}
				processor.PaintPath(operation, rule);
			}
		}

		/// <summary>A handler that implements operator (W, W*).</summary>
		/// <remarks>A handler that implements operator (W, W*). For more information see Table 51 ISO-32000-1</remarks>
		private class ClipPathOperator : IContentOperator
		{
			private int rule;

			public ClipPathOperator(int rule)
			{
				this.rule = rule;
			}

			/// <summary><inheritDoc/></summary>
			public virtual void Invoke(PdfCanvasProcessor processor, PdfLiteral @operator, IList<PdfObject> operands)
			{
				processor.isClip = true;
				processor.clippingRule = rule;
			}
		}
	}
}
