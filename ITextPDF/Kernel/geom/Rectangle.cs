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
using System.Linq;
using IText.IO.Util;
using IText.Kernel.Pdf;

namespace IText.Kernel.Geom
{
	/// <summary>Class that represent rectangle object.</summary>
	public class Rectangle
	{
		internal static float EPS = 1e-4f;

		protected internal float x;

		protected internal float y;

		protected internal float width;

		protected internal float height;

		/// <summary>Creates new instance.</summary>
		/// <param name="x">the x coordinate of lower left point</param>
		/// <param name="y">the y coordinate of lower left point</param>
		/// <param name="width">the width value</param>
		/// <param name="height">the height value</param>
		public Rectangle(float x, float y, float width, float height)
		{
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}

		/// <summary>Creates new instance of rectangle with (0, 0) as the lower left point.</summary>
		/// <param name="width">the width value</param>
		/// <param name="height">the height value</param>
		public Rectangle(float width, float height)
			: this(0, 0, width, height)
		{
		}

		/// <summary>
		/// Creates the copy of given
		/// <see cref="Rectangle"/>
		/// </summary>
		/// <param name="rect">
		/// the copied
		/// <see cref="Rectangle"/>
		/// </param>
		public Rectangle(Rectangle rect)
			: this(rect.GetX(), rect.GetY(), rect.GetWidth(), rect.GetHeight())
		{
		}

		/// <summary>Calculates the common rectangle which includes all the input rectangles.</summary>
		/// <param name="rectangles">list of input rectangles.</param>
		/// <returns>common rectangle.</returns>
		public static Rectangle GetCommonRectangle(params Rectangle[] rectangles
			)
		{
			var ury = -float.MaxValue;
			var llx = float.MaxValue;
			var lly = float.MaxValue;
			var urx = -float.MaxValue;
			foreach (var rectangle in rectangles)
			{
				if (rectangle == null)
				{
					continue;
				}
				var rec = rectangle.Clone();
				if (rec.GetY() < lly)
				{
					lly = rec.GetY();
				}
				if (rec.GetX() < llx)
				{
					llx = rec.GetX();
				}
				if (rec.GetY() + rec.GetHeight() > ury)
				{
					ury = rec.GetY() + rec.GetHeight();
				}
				if (rec.GetX() + rec.GetWidth() > urx)
				{
					urx = rec.GetX() + rec.GetWidth();
				}
			}
			return new Rectangle(llx, lly, urx - llx, ury - lly);
		}

		/// <summary>
		/// Gets the rectangle as it looks on the rotated page
		/// and returns the rectangle in coordinates relevant to the true page origin.
		/// </summary>
		/// <remarks>
		/// Gets the rectangle as it looks on the rotated page
		/// and returns the rectangle in coordinates relevant to the true page origin.
		/// This rectangle can be used to add annotations, fields, and other objects
		/// to the rotated page.
		/// </remarks>
		/// <param name="rect">the rectangle as it looks on the rotated page.</param>
		/// <param name="page">the page on which one want to process the rectangle.</param>
		/// <returns>the newly created rectangle with translated coordinates.</returns>
		public static Rectangle GetRectangleOnRotatedPage(Rectangle rect, PdfPage
			 page)
		{
			var resultRect = rect;
			var rotation = page.GetRotation();
			if (0 != rotation)
			{
				var pageSize = page.PageSize;
				switch ((rotation / 90) % 4)
				{
					case 1:
						{
							// 90 degrees
							resultRect = new Rectangle(pageSize.GetWidth() - resultRect.GetTop(), resultRect.GetLeft
								(), resultRect.GetHeight(), resultRect.GetWidth());
							break;
						}

					case 2:
						{
							// 180 degrees
							resultRect = new Rectangle(pageSize.GetWidth() - resultRect.GetRight(), pageSize.GetHeight
								() - resultRect.GetTop(), resultRect.GetWidth(), resultRect.GetHeight());
							break;
						}

					case 3:
						{
							// 270 degrees
							resultRect = new Rectangle(resultRect.GetLeft(), pageSize.GetHeight() - resultRect.GetRight
								(), resultRect.GetHeight(), resultRect.GetWidth());
							break;
						}

					case 4:
					default:
						{
							// 0 degrees
							break;
						}
				}
			}
			return resultRect;
		}

		/// <summary>Calculates the bounding box of passed points.</summary>
		/// <param name="points">the points which appear inside the area</param>
		/// <returns>the bounding box of passed points.</returns>
		public static Rectangle CalculateBBox(IList<Point> points)
		{
			IList<double> xs = new List<double>();
			IList<double> ys = new List<double>();
			foreach (var point in points)
			{
				xs.Add(point.GetX());
				ys.Add(point.GetY());
			}
			var left = xs.Min();
			var bottom = ys.Min();
			var right = xs.Max();
			var top = ys.Max();
			return new Rectangle((float)left, (float)bottom, (float)(right - left), (float)(top - bottom
				));
		}

		/// <summary>Convert rectangle to an array of points</summary>
		/// <returns>array of four extreme points of rectangle</returns>
		public virtual Point[] ToPointsArray()
		{
			return new[] { new Point(x, y), new Point(x + width, y), new Point(x + width, y + height), new Point
				(x, y + height) };
		}

		/// <summary>Get the rectangle representation of the intersection between this rectangle and the passed rectangle
		///     </summary>
		/// <param name="rect">the rectangle to find the intersection with</param>
		/// <returns>
		/// the intersection rectangle if the passed rectangles intersects with this rectangle,
		/// a rectangle representing a line if the intersection is along an edge or
		/// a rectangle representing a point if the intersection is a single point,
		/// null otherwise
		/// </returns>
		public virtual Rectangle GetIntersection(Rectangle rect)
		{
			Rectangle result = null;
			//Calculate possible lower-left corner and upper-right corner
			var llx = Math.Max(x, rect.x);
			var lly = Math.Max(y, rect.y);
			var urx = Math.Min(GetRight(), rect.GetRight());
			var ury = Math.Min(GetTop(), rect.GetTop());
			//If width or height is non-negative, there is overlap and we can construct the intersection rectangle
			var width = urx - llx;
			if (Math.Abs(width) < EPS)
			{
				width = 0;
			}
			var height = ury - lly;
			if (Math.Abs(height) < EPS)
			{
				height = 0;
			}
			if (JavaUtil.FloatCompare(width, 0) >= 0 && JavaUtil.FloatCompare(height, 0) >= 0)
			{
				if (JavaUtil.FloatCompare(width, 0) < 0)
				{
					width = 0;
				}
				if (JavaUtil.FloatCompare(height, 0) < 0)
				{
					height = 0;
				}
				result = new Rectangle(llx, lly, width, height);
			}
			return result;
		}

		/// <summary>Check if this rectangle contains the passed rectangle.</summary>
		/// <remarks>
		/// Check if this rectangle contains the passed rectangle.
		/// A rectangle will envelop itself, meaning that for any rectangle
		/// <paramref name="rect"/>
		/// the expression
		/// <c>rect.contains(rect)</c>
		/// always returns true.
		/// </remarks>
		/// <param name="rect">a rectangle which is to be checked if it is fully contained inside this rectangle.</param>
		/// <returns>true if this rectangle contains the passed rectangle, false otherwise.</returns>
		public virtual bool Contains(Rectangle rect)
		{
			var llx = GetX();
			var lly = GetY();
			var urx = llx + GetWidth();
			var ury = lly + GetHeight();
			var rllx = rect.GetX();
			var rlly = rect.GetY();
			var rurx = rllx + rect.GetWidth();
			var rury = rlly + rect.GetHeight();
			return llx - EPS <= rllx && lly - EPS <= rlly && rurx <= urx + EPS && rury <= ury + EPS;
		}

		/// <summary>Check if this rectangle and the passed rectangle overlap</summary>
		/// <param name="rect">a rectangle which is to be checked if it overlaps the passed rectangle</param>
		/// <returns>true if there is overlap of some kind</returns>
		public virtual bool Overlaps(Rectangle rect)
		{
			return Overlaps(rect, -EPS);
		}

		/// <summary>Check if this rectangle and the passed rectangle overlap</summary>
		/// <param name="rect">a rectangle which is to be checked if it overlaps the passed rectangle</param>
		/// <param name="epsilon">
		/// if greater than zero, then this is the maximum distance that one rectangle can go to another, but
		/// they will not overlap, if less than zero, then this is the minimum required distance between the
		/// rectangles so that they do not overlap
		/// </param>
		/// <returns>true if there is overlap of some kind</returns>
		public virtual bool Overlaps(Rectangle rect, float epsilon)
		{
			// Two rectangles do not overlap if any of the following holds
			// The first rectangle lies to the left of the second rectangle or touches very slightly
			if ((GetX() + GetWidth()) < (rect.GetX() + epsilon))
			{
				return false;
			}
			// The first rectangle lies to the right of the second rectangle or touches very slightly
			if ((GetX() + epsilon) > (rect.GetX() + rect.GetWidth()))
			{
				return false;
			}
			// The first rectangle lies to the bottom of the second rectangle or touches very slightly
			if ((GetY() + GetHeight()) < (rect.GetY() + epsilon))
			{
				return false;
			}
			// The first rectangle lies to the top of the second rectangle or touches very slightly
			if ((GetY() + epsilon) > (rect.GetY() + rect.GetHeight()))
			{
				return false;
			}
			return true;
		}

		/// <summary>Sets the rectangle by the coordinates, specifying its lower left and upper right points.</summary>
		/// <remarks>
		/// Sets the rectangle by the coordinates, specifying its lower left and upper right points. May be used in chain.
		/// <br />
		/// <br />
		/// Note: this method will normalize coordinates, so the rectangle will have non negative width and height,
		/// and its x and y coordinates specified lower left point.
		/// </remarks>
		/// <param name="llx">the X coordinate of lower left point</param>
		/// <param name="lly">the Y coordinate of lower left point</param>
		/// <param name="urx">the X coordinate of upper right point</param>
		/// <param name="ury">the Y coordinate of upper right point</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle SetBbox(float llx, float lly, float urx, float ury)
		{
			// If llx is greater than urx, swap them (normalize)
			if (llx > urx)
			{
				var temp = llx;
				llx = urx;
				urx = temp;
			}
			// If lly is greater than ury, swap them (normalize)
			if (lly > ury)
			{
				var temp = lly;
				lly = ury;
				ury = temp;
			}
			x = llx;
			y = lly;
			width = urx - llx;
			height = ury - lly;
			return this;
		}

		/// <summary>Gets the X coordinate of lower left point.</summary>
		/// <returns>the X coordinate of lower left point.</returns>
		public virtual float GetX()
		{
			return x;
		}

		/// <summary>Sets the X coordinate of lower left point.</summary>
		/// <remarks>Sets the X coordinate of lower left point. May be used in chain.</remarks>
		/// <param name="x">the X coordinate of lower left point to be set.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle SetX(float x)
		{
			this.x = x;
			return this;
		}

		/// <summary>Gets the Y coordinate of lower left point.</summary>
		/// <returns>the Y coordinate of lower left point.</returns>
		public virtual float GetY()
		{
			return y;
		}

		/// <summary>Sets the Y coordinate of lower left point.</summary>
		/// <remarks>Sets the Y coordinate of lower left point. May be used in chain.</remarks>
		/// <param name="y">the Y coordinate of lower left point to be set.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle SetY(float y)
		{
			this.y = y;
			return this;
		}

		/// <summary>Gets the width of rectangle.</summary>
		/// <returns>the width of rectangle.</returns>
		public virtual float GetWidth()
		{
			return width;
		}

		/// <summary>Sets the width of rectangle.</summary>
		/// <remarks>Sets the width of rectangle. May be used in chain.</remarks>
		/// <param name="width">the the width of rectangle to be set.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle SetWidth(float width)
		{
			this.width = width;
			return this;
		}

		/// <summary>Gets the height of rectangle.</summary>
		/// <returns>the height of rectangle.</returns>
		public virtual float GetHeight()
		{
			return height;
		}

		/// <summary>Sets the height of rectangle.</summary>
		/// <remarks>Sets the height of rectangle. May be used in chain.</remarks>
		/// <param name="height">the the width of rectangle to be set.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle SetHeight(float height)
		{
			this.height = height;
			return this;
		}

		/// <summary>Increases the height of rectangle by the given value.</summary>
		/// <remarks>Increases the height of rectangle by the given value. May be used in chain.</remarks>
		/// <param name="extra">the value of the extra height to be added.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle IncreaseHeight(float extra)
		{
			height += extra;
			return this;
		}

		/// <summary>Decreases the height of rectangle by the given value.</summary>
		/// <remarks>Decreases the height of rectangle by the given value. May be used in chain.</remarks>
		/// <param name="extra">the value of the extra height to be subtracted.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle DecreaseHeight(float extra)
		{
			height -= extra;
			return this;
		}

		/// <summary>Increases the width of rectangle by the given value.</summary>
		/// <remarks>Increases the width of rectangle by the given value. May be used in chain.</remarks>
		/// <param name="extra">the value of the extra wudth to be added.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle IncreaseWidth(float extra)
		{
			width += extra;
			return this;
		}

		/// <summary>Decreases the width of rectangle by the given value.</summary>
		/// <remarks>Decreases the width of rectangle by the given value. May be used in chain.</remarks>
		/// <param name="extra">the value of the extra width to be subtracted.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle DecreaseWidth(float extra)
		{
			width -= extra;
			return this;
		}

		/// <summary>Gets the X coordinate of the left edge of the rectangle.</summary>
		/// <remarks>
		/// Gets the X coordinate of the left edge of the rectangle. Same as:
		/// <c>getX()</c>.
		/// </remarks>
		/// <returns>the X coordinate of the left edge of the rectangle.</returns>
		public virtual float GetLeft()
		{
			return x;
		}

		/// <summary>Gets the X coordinate of the right edge of the rectangle.</summary>
		/// <remarks>
		/// Gets the X coordinate of the right edge of the rectangle. Same as:
		/// <c>getX() + getWidth()</c>.
		/// </remarks>
		/// <returns>the X coordinate of the right edge of the rectangle.</returns>
		public virtual float GetRight()
		{
			return x + width;
		}

		/// <summary>Gets the Y coordinate of the upper edge of the rectangle.</summary>
		/// <remarks>
		/// Gets the Y coordinate of the upper edge of the rectangle. Same as:
		/// <c>getY() + getHeight()</c>.
		/// </remarks>
		/// <returns>the Y coordinate of the upper edge of the rectangle.</returns>
		public virtual float GetTop()
		{
			return y + height;
		}

		/// <summary>Gets the Y coordinate of the lower edge of the rectangle.</summary>
		/// <remarks>
		/// Gets the Y coordinate of the lower edge of the rectangle. Same as:
		/// <c>getY()</c>.
		/// </remarks>
		/// <returns>the Y coordinate of the lower edge of the rectangle.</returns>
		public virtual float GetBottom()
		{
			return y;
		}

		/// <summary>Decreases the y coordinate.</summary>
		/// <param name="move">the value on which the position will be changed.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle MoveDown(float move)
		{
			y -= move;
			return this;
		}

		/// <summary>Increases the y coordinate.</summary>
		/// <param name="move">the value on which the position will be changed.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle MoveUp(float move)
		{
			y += move;
			return this;
		}

		/// <summary>Increases the x coordinate.</summary>
		/// <param name="move">the value on which the position will be changed.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle MoveRight(float move)
		{
			x += move;
			return this;
		}

		/// <summary>Decreases the x coordinate.</summary>
		/// <param name="move">the value on which the position will be changed.</param>
		/// <returns>
		/// this
		/// <see cref="Rectangle"/>
		/// instance.
		/// </returns>
		public virtual Rectangle MoveLeft(float move)
		{
			x -= move;
			return this;
		}

		/// <summary>Change the rectangle according the specified margins.</summary>
		/// <param name="topIndent">the value on which the top y coordinate will change.</param>
		/// <param name="rightIndent">the value on which the right x coordinate will change.</param>
		/// <param name="bottomIndent">the value on which the bottom y coordinate will change.</param>
		/// <param name="leftIndent">the value on which the left x coordinate will change.</param>
		/// <param name="reverse">
		/// if
		/// <see langword="true"/>
		/// the rectangle will expand, otherwise it will shrink
		/// </param>
		/// <returns>the rectangle with applied margins</returns>
		public virtual Rectangle ApplyMargins(float topIndent, float rightIndent, float bottomIndent
			, float leftIndent, bool reverse)
		{
			x += leftIndent * (reverse ? -1 : 1);
			width -= (leftIndent + rightIndent) * (reverse ? -1 : 1);
			y += bottomIndent * (reverse ? -1 : 1);
			height -= (topIndent + bottomIndent) * (reverse ? -1 : 1);
			return this;
		}

		/// <summary>Checks if rectangle have common points with line, specified by two points.</summary>
		/// <param name="x1">the x coordinate of first line's point.</param>
		/// <param name="y1">the y coordinate of first line's point.</param>
		/// <param name="x2">the x coordinate of second line's point.</param>
		/// <param name="y2">the y coordinate of second line's point.</param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if rectangle have common points with line and
		/// <see langword="false"/>
		/// otherwise.
		/// </returns>
		public virtual bool IntersectsLine(float x1, float y1, float x2, float y2)
		{
			double rx1 = GetX();
			double ry1 = GetY();
			var rx2 = rx1 + GetWidth();
			var ry2 = ry1 + GetHeight();
			return (rx1 <= x1 && x1 <= rx2 && ry1 <= y1 && y1 <= ry2) || (rx1 <= x2 && x2 <= rx2 && ry1 <= y2 && y2 <=
				 ry2) || LinesIntersect(rx1, ry1, rx2, ry2, x1, y1, x2, y2) || LinesIntersect(rx2, ry1, rx1, ry2, x1,
				y1, x2, y2);
		}

		/// <summary>Gets the string representation of rectangle.</summary>
		/// <returns>the string representation of rectangle.</returns>
		public override string ToString()
		{
			return "Rectangle: " + GetWidth() + 'x' + GetHeight();
		}

		/// <summary>
		/// Creates a "deep copy" of this rectangle, meaning the object returned by this method will be independent
		/// of the object being cloned.
		/// </summary>
		/// <returns>the copied rectangle.</returns>
		public virtual Rectangle Clone()
		{
			return (Rectangle)MemberwiseClone();
		}

		/// <summary>Compares instance of this rectangle with given deviation equals to 0.0001</summary>
		/// <param name="that">
		/// the
		/// <see cref="Rectangle"/>
		/// to compare with.
		/// </param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if the difference between corresponding rectangle values is less than deviation and
		/// <see langword="false"/>
		/// otherwise.
		/// </returns>
		public virtual bool EqualsWithEpsilon(Rectangle that)
		{
			return EqualsWithEpsilon(that, EPS);
		}

		/// <summary>Compares instance of this rectangle with given deviation.</summary>
		/// <param name="that">
		/// the
		/// <see cref="Rectangle"/>
		/// to compare with.
		/// </param>
		/// <param name="eps">the deviation value.</param>
		/// <returns>
		/// 
		/// <see langword="true"/>
		/// if the difference between corresponding rectangle values is less than deviation and
		/// <see langword="false"/>
		/// otherwise.
		/// </returns>
		public virtual bool EqualsWithEpsilon(Rectangle that, float eps)
		{
			var dx = Math.Abs(x - that.x);
			var dy = Math.Abs(y - that.y);
			var dw = Math.Abs(width - that.width);
			var dh = Math.Abs(height - that.height);
			return dx < eps && dy < eps && dw < eps && dh < eps;
		}

		private static bool LinesIntersect(double x1, double y1, double x2, double y2, double x3, double y3, double
			 x4, double y4)
		{
			/*
            * A = (x2-x1, y2-y1) B = (x3-x1, y3-y1) C = (x4-x1, y4-y1) D = (x4-x3,
            * y4-y3) = C-B E = (x1-x3, y1-y3) = -B F = (x2-x3, y2-y3) = A-B
            *
            * Result is ((AxB) * (AxC) <=0) and ((DxE) * (DxF) <= 0)
            *
            * DxE = (C-B)x(-B) = BxB-CxB = BxC DxF = (C-B)x(A-B) = CxA-CxB-BxA+BxB =
            * AxB+BxC-AxC
            */
			// A
			x2 -= x1;
			y2 -= y1;
			// B
			x3 -= x1;
			y3 -= y1;
			// C
			x4 -= x1;
			y4 -= y1;
			var AvB = x2 * y3 - x3 * y2;
			var AvC = x2 * y4 - x4 * y2;
			// Online
			if (AvB == 0.0 && AvC == 0.0)
			{
				if (x2 != 0.0)
				{
					return (x4 * x3 <= 0.0) || ((x3 * x2 >= 0.0) && (x2 > 0.0 ? x3 <= x2 || x4 <= x2 : x3 >= x2 || x4 >= x2));
				}
				if (y2 != 0.0)
				{
					return (y4 * y3 <= 0.0) || ((y3 * y2 >= 0.0) && (y2 > 0.0 ? y3 <= y2 || y4 <= y2 : y3 >= y2 || y4 >= y2));
				}
				return false;
			}
			var BvC = x3 * y4 - x4 * y3;
			return (AvB * AvC <= 0.0) && (BvC * (AvB + BvC - AvC) <= 0.0);
		}

		/// <summary>Create a list of bounding rectangles from an 8 x n array of Quadpoints.</summary>
		/// <param name="quadPoints">8xn array of numbers representing 4 points</param>
		/// <returns>a list of bounding rectangles for the passed quadpoints</returns>
		public static IList<Rectangle> CreateBoundingRectanglesFromQuadPoint(PdfArray quadPoints
			)
		{
			IList<Rectangle> boundingRectangles = new List<Rectangle>();
			if (quadPoints.Size() % 8 != 0)
			{
				throw new PdfException(PdfException.QuadPointArrayLengthIsNotAMultipleOfEight);
			}
			for (var i = 0; i < quadPoints.Size(); i += 8)
			{
				var quadPointEntry = JavaUtil.ArraysCopyOfRange(quadPoints.ToFloatArray(), i, i + 8);
				var quadPointEntryFA = new PdfArray(quadPointEntry);
				boundingRectangles.Add(CreateBoundingRectangleFromQuadPoint(quadPointEntryFA));
			}
			return boundingRectangles;
		}

		/// <summary>Create the bounding rectangle for the given array of quadpoints.</summary>
		/// <param name="quadPoints">an array containing 8 numbers that correspond to 4 points.</param>
		/// <returns>The smallest orthogonal rectangle containing the quadpoints.</returns>
		public static Rectangle CreateBoundingRectangleFromQuadPoint(PdfArray quadPoints)
		{
			//Check if array length is a multiple of 8
			if (quadPoints.Size() % 8 != 0)
			{
				throw new PdfException(PdfException.QuadPointArrayLengthIsNotAMultipleOfEight);
			}
			var llx = float.MaxValue;
			var lly = float.MaxValue;
			var urx = -float.MaxValue;
			var ury = -float.MaxValue;
			// QuadPoints in redact annotations have "Z" order, in spec they're specified
			for (var j = 0; j < 8; j += 2)
			{
				var x = quadPoints.GetAsNumber(j).FloatValue();
				var y = quadPoints.GetAsNumber(j + 1).FloatValue();
				if (x < llx)
				{
					llx = x;
				}
				if (x > urx)
				{
					urx = x;
				}
				if (y < lly)
				{
					lly = y;
				}
				if (y > ury)
				{
					ury = y;
				}
			}
			return (new Rectangle(llx, lly, urx - llx, ury - lly));
		}
	}
}
