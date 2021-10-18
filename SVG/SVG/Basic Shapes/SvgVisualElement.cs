using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Linq;

namespace Svg
{
    /// <summary>
    /// The class that all SVG elements should derive from when they are to be rendered.
    /// </summary>
    public abstract partial class SvgVisualElement : SvgElement, ISvgBoundable, ISvgStylable, ISvgClipable
    {
        private bool? _requiresSmoothRendering;
        private Region _previousClip;

        /// <summary>
        /// Gets the <see cref="GraphicsPath"/> for this element.
        /// </summary>
        public abstract GraphicsPath Path(ISvgRenderer renderer);

        PointF ISvgBoundable.Location
        {
            get
            {
                return Bounds.Location;
            }
        }

        SizeF ISvgBoundable.Size
        {
            get
            {
                return Bounds.Size;
            }
        }

        /// <summary>
        /// Gets the bounds of the element.
        /// </summary>
        /// <value>The bounds.</value>
        public abstract RectangleF Bounds { get; }

        /// <summary>
        /// Gets the associated <see cref="SvgClipPath"/> if one has been specified.
        /// </summary>
        [SvgAttribute("clip")]
        public virtual string Clip
        {
            get { return Attributes.GetInheritedAttribute<string>("clip"); }
            set { Attributes["clip"] = value; }
        }

        /// <summary>
        /// Gets the associated <see cref="SvgClipPath"/> if one has been specified.
        /// </summary>
        [SvgAttribute("clip-path")]
        public virtual Uri ClipPath
        {
            get { return Attributes.GetAttribute<Uri>("clip-path"); }
            set { Attributes["clip-path"] = value; }
        }

        /// <summary>
        /// Gets or sets the algorithm which is to be used to determine the clipping region.
        /// </summary>
        [SvgAttribute("clip-rule")]
        public SvgClipRule ClipRule
        {
            get { return Attributes.GetAttribute<SvgClipRule>("clip-rule", SvgClipRule.NonZero); }
            set { Attributes["clip-rule"] = value; }
        }

        /// <summary>
        /// Gets the associated <see cref="SvgClipPath"/> if one has been specified.
        /// </summary>
        [SvgAttribute("filter")]
        public virtual Uri Filter
        {
            get { return Attributes.GetInheritedAttribute<Uri>("filter"); }
            set { Attributes["filter"] = value; }
        }

        /// <summary>
        /// Gets or sets a value to determine if anti-aliasing should occur when the element is being rendered.
        /// </summary>
        protected virtual bool RequiresSmoothRendering
        {
            get
            {
                if (_requiresSmoothRendering == null)
                    _requiresSmoothRendering = ConvertShapeRendering2AntiAlias(ShapeRendering);

                return _requiresSmoothRendering.Value;
            }
        }

        private bool ConvertShapeRendering2AntiAlias(SvgShapeRendering shapeRendering)
        {
            switch (shapeRendering)
            {
                case SvgShapeRendering.OptimizeSpeed:
                case SvgShapeRendering.CrispEdges:
                case SvgShapeRendering.GeometricPrecision:
                    return false;
                default:
                    // SvgShapeRendering.Auto
                    // SvgShapeRendering.Inherit
                    return true;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgGraphicsElement"/> class.
        /// </summary>
        public SvgVisualElement()
        {
            IsPathDirty = true;
        }

        protected virtual bool Renderable { get { return true; } }

        /// <summary>
        /// Renders the <see cref="SvgElement"/> and contents to the specified <see cref="Graphics"/> object.
        /// </summary>
        /// <param name="renderer">The <see cref="ISvgRenderer"/> object to render to.</param>
        protected override void Render(ISvgRenderer renderer)
        {
            Render(renderer, true);
        }

        private void Render(ISvgRenderer renderer, bool renderFilter)
        {
            if (Visible && Displayable && PushTransforms(renderer) &&
                (!Renderable || Path(renderer) != null))
            {
                bool renderNormal = true;

                if (renderFilter && Filter != null)
                {
                    var filterPath = Filter;
                    if (filterPath.ToString().StartsWith("url("))
                    {
                        filterPath = new Uri(filterPath.ToString().Substring(4, filterPath.ToString().Length - 5), UriKind.RelativeOrAbsolute);
                    }
                    var filter = OwnerDocument.IdManager.GetElementById(filterPath) as FilterEffects.SvgFilter;
                    if (filter != null)
                    {
                        PopTransforms(renderer);
                        try
                        {
                            filter.ApplyFilter(this, renderer, (r) => Render(r, false));
                        }
                        catch (Exception ex) { Debug.Print(ex.ToString()); }
                        renderNormal = false;
                    }
                }


                if (renderNormal)
                {
                    SetClip(renderer);

                    if (Renderable)
                    {
                        // If this element needs smoothing enabled turn anti-aliasing on
                        if (RequiresSmoothRendering)
                        {
                            renderer.SmoothingMode = SmoothingMode.AntiAlias;
                        }

                        RenderFill(renderer);
                        RenderStroke(renderer);

                        // Reset the smoothing mode
                        if (RequiresSmoothRendering && renderer.SmoothingMode == SmoothingMode.AntiAlias)
                        {
                            renderer.SmoothingMode = SmoothingMode.Default;
                        }
                    }
                    else
                    {
                        base.RenderChildren(renderer);
                    }

                    ResetClip(renderer);
                    PopTransforms(renderer);
                }

            }
        }

        /// <summary>
        /// Renders the fill of the <see cref="SvgVisualElement"/> to the specified <see cref="ISvgRenderer"/>
        /// </summary>
        /// <param name="renderer">The <see cref="ISvgRenderer"/> object to render to.</param>
        protected internal virtual void RenderFill(ISvgRenderer renderer)
        {
            if (Fill != null)
            {
                using (var brush = Fill.GetBrush(this, renderer, Math.Min(Math.Max(FillOpacity * Opacity, 0), 1)))
                {
                    if (brush != null)
                    {
                        Path(renderer).FillMode = FillRule == SvgFillRule.NonZero ? FillMode.Winding : FillMode.Alternate;
                        renderer.FillPath(brush, Path(renderer));
                    }
                }
            }
        }

        /// <summary>
        /// Renders the stroke of the <see cref="SvgVisualElement"/> to the specified <see cref="ISvgRenderer"/>
        /// </summary>
        /// <param name="renderer">The <see cref="ISvgRenderer"/> object to render to.</param>
        protected internal virtual bool RenderStroke(ISvgRenderer renderer)
        {
            if (Stroke != null && Stroke != SvgPaintServer.None && StrokeWidth > 0)
            {
                float strokeWidth = StrokeWidth.ToDeviceValue(renderer, UnitRenderingType.Other, this);
                using (var brush = Stroke.GetBrush(this, renderer, Math.Min(Math.Max(StrokeOpacity * Opacity, 0), 1), true))
                {
                    if (brush != null)
                    {
                        var path = Path(renderer);
                        var bounds = path.GetBounds();
                        if (path.PointCount < 1) return false;
                        if (bounds.Width <= 0 && bounds.Height <= 0)
                        {
                            switch (StrokeLineCap)
                            {
                                case SvgStrokeLineCap.Round:
                                    using (var capPath = new GraphicsPath())
                                    {
                                        capPath.AddEllipse(path.PathPoints[0].X - strokeWidth / 2, path.PathPoints[0].Y - strokeWidth / 2, strokeWidth, strokeWidth);
                                        renderer.FillPath(brush, capPath);
                                    }
                                    break;
                                case SvgStrokeLineCap.Square:
                                    using (var capPath = new GraphicsPath())
                                    {
                                        capPath.AddRectangle(new RectangleF(path.PathPoints[0].X - strokeWidth / 2, path.PathPoints[0].Y - strokeWidth / 2, strokeWidth, strokeWidth));
                                        renderer.FillPath(brush, capPath);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            using (var pen = new Pen(brush, strokeWidth))
                            {
                                if (StrokeDashArray != null && StrokeDashArray.Count > 0)
                                {
                                    /* divide by stroke width - GDI behaviour that I don't quite understand yet.*/
                                    pen.DashPattern = StrokeDashArray.ConvertAll(u => ((u.ToDeviceValue(renderer, UnitRenderingType.Other, this) <= 0) ? 1 : u.ToDeviceValue(renderer, UnitRenderingType.Other, this)) /
                                        ((strokeWidth <= 0) ? 1 : strokeWidth)).ToArray();
                                }
                                switch (StrokeLineJoin)
                                {
                                    case SvgStrokeLineJoin.Bevel:
                                        pen.LineJoin = LineJoin.Bevel;
                                        break;
                                    case SvgStrokeLineJoin.Round:
                                        pen.LineJoin = LineJoin.Round;
                                        break;
                                    default:
                                        pen.LineJoin = LineJoin.Miter;
                                        break;
                                }
                                pen.MiterLimit = StrokeMiterLimit;
                                switch (StrokeLineCap)
                                {
                                    case SvgStrokeLineCap.Round:
                                        pen.StartCap = LineCap.Round;
                                        pen.EndCap = LineCap.Round;
                                        break;
                                    case SvgStrokeLineCap.Square:
                                        pen.StartCap = LineCap.Square;
                                        pen.EndCap = LineCap.Square;
                                        break;
                                }

                                renderer.DrawPath(pen, path);

                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the clipping region of the specified <see cref="ISvgRenderer"/>.
        /// </summary>
        /// <param name="renderer">The <see cref="ISvgRenderer"/> to have its clipping region set.</param>
        protected internal virtual void SetClip(ISvgRenderer renderer)
        {
            if (ClipPath != null || !string.IsNullOrEmpty(Clip))
            {
                _previousClip = renderer.GetClip();

                if (ClipPath != null)
                {
                    SvgClipPath clipPath = OwnerDocument.GetElementById<SvgClipPath>(ClipPath.ToString());
                    if (clipPath != null) renderer.SetClip(clipPath.GetClipRegion(this), CombineMode.Intersect);
                }

                var clip = Clip;
                if (!string.IsNullOrEmpty(clip) && clip.StartsWith("rect("))
                {
                    clip = clip.Trim();
                    var offsets = (from o in clip.Substring(5, clip.Length - 6).Split(',')
                                   select float.Parse(o.Trim())).ToList();
                    var bounds = Bounds;
                    var clipRect = new RectangleF(bounds.Left + offsets[3], bounds.Top + offsets[0],
                                                  bounds.Width - (offsets[3] + offsets[1]),
                                                  bounds.Height - (offsets[2] + offsets[0]));
                    renderer.SetClip(new Region(clipRect), CombineMode.Intersect);
                }
            }
        }

        /// <summary>
        /// Resets the clipping region of the specified <see cref="ISvgRenderer"/> back to where it was before the <see cref="SetClip"/> method was called.
        /// </summary>
        /// <param name="renderer">The <see cref="ISvgRenderer"/> to have its clipping region reset.</param>
        protected internal virtual void ResetClip(ISvgRenderer renderer)
        {
            if (_previousClip != null)
            {
                renderer.SetClip(_previousClip);
                _previousClip = null;
            }
        }

        /// <summary>
        /// Sets the clipping region of the specified <see cref="ISvgRenderer"/>.
        /// </summary>
        /// <param name="renderer">The <see cref="ISvgRenderer"/> to have its clipping region set.</param>
        void ISvgClipable.SetClip(ISvgRenderer renderer)
        {
            SetClip(renderer);
        }

        /// <summary>
        /// Resets the clipping region of the specified <see cref="ISvgRenderer"/> back to where it was before the <see cref="SetClip"/> method was called.
        /// </summary>
        /// <param name="renderer">The <see cref="ISvgRenderer"/> to have its clipping region reset.</param>
        void ISvgClipable.ResetClip(ISvgRenderer renderer)
        {
            ResetClip(renderer);
        }

        public override SvgElement DeepCopy<T>()
        {
            var newObj = base.DeepCopy<T>() as SvgVisualElement;
            newObj.ClipPath = ClipPath;
            newObj.ClipRule = ClipRule;
            newObj.Filter = Filter;

            newObj.Visible = Visible;
            if (Fill != null)
                newObj.Fill = Fill;
            if (Stroke != null)
                newObj.Stroke = Stroke;
            newObj.FillRule = FillRule;
            newObj.FillOpacity = FillOpacity;
            newObj.StrokeWidth = StrokeWidth;
            newObj.StrokeLineCap = StrokeLineCap;
            newObj.StrokeLineJoin = StrokeLineJoin;
            newObj.StrokeMiterLimit = StrokeMiterLimit;
            newObj.StrokeDashArray = StrokeDashArray;
            newObj.StrokeDashOffset = StrokeDashOffset;
            newObj.StrokeOpacity = StrokeOpacity;
            newObj.Opacity = Opacity;

            return newObj;
        }

    }
}
