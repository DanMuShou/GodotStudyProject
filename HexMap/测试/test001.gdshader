shader_type spatial;

uniform sampler2DArray Texture : source_color, filter_linear_mipmap, repeat_enable;

void vertex() {
}

void fragment() {
	vec3 uvw = vec3(1.0, 1.0, 0);
	vec4 color = texture(Texture, uvw);
	
	ALBEDO = color.rgb;
}