#version 330
uniform mat4 MVPMatrix;
uniform int FastFade;
uniform float PixelPerWorld;
in vec3 Colour;
in vec2 Position;
in float Rotation;
in float Age;
in float Scale;
in float Opacity;
out vec4 outColor;
out float rotation;

vec3 HueToRgb(in float hue) {
	float r = abs(hue * 6 - 3) - 1;
	float g = 2 - abs(hue * 6 - 2);
	float b = 2 - abs(hue * 6 - 4);

	return clamp(vec3(r, g, b), 0.0, 1.0);
}

vec3 HslToRgb(in vec3 hsl) {
	vec3 rgb = HueToRgb(hsl.x / 360.0f);
	float c = (1 - abs(2 * hsl.z - 1)) * hsl.y;

	return (rgb - 0.5) * c + hsl.z;
}

void main()
{
	outColor.xyz = HslToRgb(Colour.xyz);

	if (FastFade != 0) {
		outColor.a = 1.0f - Age;
	}
	else
	{
		outColor.a = Opacity;
	}

	rotation = Rotation;

	gl_PointSize = Scale * PixelPerWorld;
	gl_Position = MVPMatrix * vec4(Position, 0, 1);
}
