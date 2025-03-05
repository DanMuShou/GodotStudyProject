shader_type spatial;

uniform vec4 Color : source_color;
uniform sampler2D Noise : source_color, filter_linear, repeat_enable;
uniform float Glossiness : hint_range(0.0, 1.0) = 0.5;
uniform float Specular : hint_range(0.0, 1.0) = 0.0;
uniform float DepthOffsetY : hint_range(-1.0, 1.0) = 0.01;

#include "ShaderExpand/HexMetrics.gdshaderinc"

void vertex() {
	VERTEX.y += DepthOffsetY;
}

void fragment() {
	vec3 worldPos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec4 noise = texture(Noise, worldPos.xz * 0.25);
	vec4 color = Color * (noise.y * 0.75 + 0.25);

	float blend = UV.x;
	blend *= noise.x + 0.7;
	blend = smoothstep(0.4, 0.7, blend);

	ALBEDO = color.rgb;
	ALPHA = blend;

	SPECULAR = Specular;
	ROUGHNESS = 1.0 - Glossiness;
}
