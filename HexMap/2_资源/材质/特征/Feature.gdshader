shader_type spatial;

uniform vec3 Color : source_color;

void fragment() {
	ALBEDO = Color;
}