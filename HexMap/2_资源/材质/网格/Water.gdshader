shader_type spatial;

uniform vec4 Color : source_color;
uniform sampler2D Noise : source_color, filter_linear, repeat_enable;
uniform float Glossiness : hint_range(0.0, 1.0) = 0.5;
uniform float Specular : hint_range(0.0, 1.0) = 0.0;

#include "ShaderExpand/Water.gdshaderinc"

void fragment() {
	vec2 wordXZ =  (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xz;
	float shore = UV.y;
	float foam = Foam(shore, wordXZ, Noise);
	float waves = Waves(wordXZ, Noise);
	waves *= 1.0 - shore;

	vec4 c = clamp(Color + max(foam, waves), 0.0, 1.0);

	ALBEDO = c.rgb;
	ALPHA = c.a;
	SPECULAR = Specular;
	ROUGHNESS = 1.0 - Glossiness;
}