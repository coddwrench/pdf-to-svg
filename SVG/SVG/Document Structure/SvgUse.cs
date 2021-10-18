using System;

namespace Svg
{
    [SvgElement("use")]
    public class SvgUse : SvgVisualElement
    {
        private Uri _referencedElement;

        [SvgAttribute("href", SvgAttributeAttribute.XLinkNamespace)]
        public virtual Uri ReferencedElement
        {
            get { return _referencedElement; }
            set { _referencedElement = value; }
        }

        [SvgAttribute("x")]
        public virtual SvgUnit X
        {
            get { return Attributes.GetAttribute<SvgUnit>("x"); }
            set { Attributes["x"] = value; }
        }

        [SvgAttribute("y")]
        public virtual SvgUnit Y
        {
            get { return Attributes.GetAttribute<SvgUnit>("y"); }
            set { Attributes["y"] = value; }
        }

        /// <summary>
        /// Applies the required transforms to <see cref="ISvgRenderer"/>.
        /// </summary>
        /// <param name="renderer">The <see cref="ISvgRenderer"/> to be transformed.</param>
        protected internal override bool PushTransforms(ISvgRenderer renderer)
        {
            if (!base.PushTransforms(renderer)) return false;
            renderer.TranslateTransform(X.ToDeviceValue(renderer, UnitRenderingType.Horizontal, this),
                                        Y.ToDeviceValue(renderer, UnitRenderingType.Vertical, this));
            return true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SvgUse"/> class.
        /// </summary>
        public SvgUse()
        {
            X = 0;
            Y = 0;
        }

        public override System.Drawing.Drawing2D.GraphicsPath Path(ISvgRenderer renderer)
        {
            SvgVisualElement element = (SvgVisualElement)OwnerDocument.IdManager.GetElementById(ReferencedElement);
            return (element != null) ? element.Path(renderer) : null;
        }

        public override System.Drawing.RectangleF Bounds
        {
            get { return new System.Drawing.RectangleF(); }
        }

        protected override bool Renderable { get { return false; } }

        protected override void Render(ISvgRenderer renderer)
        {
            if (Visible && Displayable && PushTransforms(renderer))
            {
                SetClip(renderer);

                var element = OwnerDocument.IdManager.GetElementById(ReferencedElement) as SvgVisualElement;
                if (element != null)
                {
                    var origParent = element.Parent;
                    element._parent = this;
                    element.RenderElement(renderer);
                    element._parent = origParent;
                }

                ResetClip(renderer);
                PopTransforms(renderer);
            }
        }


        public override SvgElement DeepCopy()
        {
            return DeepCopy<SvgUse>();
        }

        public override SvgElement DeepCopy<T>()
        {
            var newObj = base.DeepCopy<T>() as SvgUse;
            newObj.ReferencedElement = ReferencedElement;
            newObj.X = X;
            newObj.Y = Y;

            return newObj;
        }

    }
}