namespace Svg
{
    [SvgElement("tspan")]
    public class SvgTextSpan : SvgTextBase
    {
        public override SvgElement DeepCopy()
        {
            return DeepCopy<SvgTextSpan>();
        }

        public override SvgElement DeepCopy<T>()
        {
            var newObj = base.DeepCopy<T>() as SvgTextSpan;
            newObj.X = X;
            newObj.Y = Y;
            newObj.Dx = Dx;
            newObj.Dy = Dy;
            newObj.Text = Text;

            return newObj;
        }


    }
}