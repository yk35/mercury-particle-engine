#version 330
uniform sampler2D tex;
in float rotation;
in vec4 outColor;
void main()
{
	float c = cos(rotation);
	float s = sin(rotation);
	
	mat2 rotationMatrix = mat2(c, -s, s, c);
	
	vec2 texCoord = rotationMatrix * (gl_PointCoord.xy - vec2(0.5f, 0.5f)) + vec2(0.5f, 0.5f);
	
    gl_FragColor = texture(tex, texCoord) * outColor;
}

