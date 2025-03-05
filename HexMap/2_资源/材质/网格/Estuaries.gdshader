shader_type spatial;

uniform vec4 Color : source_color;
uniform sampler2D Noise : source_color, filter_linear_mipmap, repeat_enable;

#include "ShaderExpand/Water.gdshaderinc"

void vertex() {
}

void fragment() {
	vec2 wordXZ = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xz;
	float shore = UV.y;
	float foam = Foam(shore, wordXZ, Noise);
	float waves = Waves(wordXZ, Noise);
	float shoreWater = max(foam, waves);
	float river = River(UV2, Noise);
	float water = mix(shoreWater, river, UV.x);

	vec4 c = clamp((Color + water * UV.y), 0.0, 1.0);

	ALBEDO = c.rgb;
	ALPHA = c.a;
}
