using System.ComponentModel;

namespace Svg.FilterEffects
{
    [TypeConverter(typeof(EnumBaseConverter<SvgColourMatrixType>))]
	public enum SvgColourMatrixType
	{
		Matrix,
		Saturate,
		HueRotate,
		LuminanceToAlpha
	}
}
