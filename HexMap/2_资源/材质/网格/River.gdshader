shader_type spatial;

uniform vec4 Color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform sampler2D Noise : source_color, filter_linear_mipmap, repeat_enable;
uniform float Glossiness : hint_range(0.0, 1.0) = 0.5;
uniform float Specular : hint_range(0.0, 1.0) = 0.0;

#include "ShaderExpand/Water.gdshaderinc"

void fragment() {
	float river = River(UV, Noise);
	vec4 c = clamp(Color + river, 0.0, 1.0);
	
	ALBEDO = c.rgb;
	ALPHA = c.a;
	SPECULAR = Specular;
	ROUGHNESS = 1.0 - Glossiness;
}
