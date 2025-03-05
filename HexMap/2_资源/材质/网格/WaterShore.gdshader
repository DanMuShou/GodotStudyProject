shader_type spatial;

uniform vec4 Color : source_color;
uniform sampler2D Noise : source_color, filter_linear, repeat_enable;

#include "ShaderExpand/Water.gdshaderinc"

void fragment() {
	vec2 wordXZ = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xz;
	float shore = UV.y;
	float foam = Foam(shore, wordXZ, Noise);
	vec4 c = clamp(Color + foam * UV.y, 0, 1);

	ALBEDO = c.rgb;
	ALPHA = c.a;
}