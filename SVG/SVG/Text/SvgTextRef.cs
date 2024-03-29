﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Svg
{
    [SvgElement("tref")]
    public class SvgTextRef : SvgTextBase
    {
        private Uri _referencedElement;

        [SvgAttribute("href", SvgAttributeAttribute.XLinkNamespace)]
        public virtual Uri ReferencedElement
        {
            get { return _referencedElement; }
            set { _referencedElement = value; }
        }

        internal override IEnumerable<ISvgNode> GetContentNodes()
        {
            var refText = OwnerDocument.IdManager.GetElementById(ReferencedElement) as SvgTextBase;
            IEnumerable<ISvgNode> contentNodes = null;

            if (refText == null)
            {
                contentNodes = base.GetContentNodes();
            }
            else
            {
                contentNodes = refText.GetContentNodes();
            }

            contentNodes = contentNodes.Where(o => !(o is ISvgDescriptiveElement));

            return contentNodes;
        }

        public override SvgElement DeepCopy()
        {
            return DeepCopy<SvgTextRef>();
        }

        public override SvgElement DeepCopy<T>()
        {
            var newObj = base.DeepCopy<T>() as SvgTextRef;
            newObj.X = X;
            newObj.Y = Y;
            newObj.Dx = Dx;
            newObj.Dy = Dy;
            newObj.Text = Text;
            newObj.ReferencedElement = ReferencedElement;

            return newObj;
        }


    }
}
