shader_type spatial;
render_mode unshaded, blend_mix, depth_test_disabled;

uniform sampler2D _tex : source_color;

void fragment() {
	vec4 c = texture(_tex, UV);
	c *= COLOR;
	ALBEDO = c.rgb;
	ALPHA = c.a;
}
