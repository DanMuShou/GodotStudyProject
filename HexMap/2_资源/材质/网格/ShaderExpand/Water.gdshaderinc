#include "HexMetrics.gdshaderinc"

float River(vec2 riverUV, sampler2D noiseTex){
	vec2 uv = riverUV;
	uv.x = uv.x * 0.0625 + TIME * 0.005 / BASE_SIZE;
	uv.y -= TIME * 0.25;
	vec4 noise1 = texture(noiseTex, uv);

	vec2 uv2 = riverUV;
	uv2.x = uv2.x * 0.0625 - TIME * 0.0052 / BASE_SIZE;
	uv2.y -= TIME * 0.23;
	vec4 noise2 = texture(noiseTex, uv2);

	return  noise1.x * noise2.w;
}

float Foam(float shore, vec2 worldXZ, sampler2D noiseTex){
	shore = sqrt(shore) * 0.9;

	vec2 noiseUV = worldXZ + TIME * 0.25 / BASE_SIZE;

	vec4 n = texture(noiseTex, noiseUV * 0.015 * BASE_SIZE);
	float distortion1 = n.x * (1.0 - shore);
	float foam1 = sin((shore + distortion1) * 10.0  - TIME);
	foam1 *= foam1;

	float distortion2 = n.y * (1.0 - shore);
	float foam2 = sin((shore + distortion2) * 10.0  + TIME);
	foam2 *= foam2 * 0.7;

	return max(foam1, foam2) * shore;
}

float Waves(vec2 wordXZ, sampler2D noiseTex){
	vec2 uv1 = wordXZ;
	uv1.y += TIME / BASE_SIZE;
	vec4 noise1 = texture(noiseTex, uv1 * 0.025 * BASE_SIZE);

	vec2 uv2 = wordXZ;
	uv2.x += TIME / BASE_SIZE;
	vec4 noise2 = texture(noiseTex, uv2 * 0.025 * BASE_SIZE);

	float blendWave = sin(
		(wordXZ.x + wordXZ.y) * 0.1 +
		noise1.y + noise2.z + TIME / BASE_SIZE);
	blendWave *= blendWave;

	float waves =
		mix(noise1.z, noise1.w, blendWave) +
		mix(noise2.x, noise2.y, blendWave);
	return smoothstep(0.75, 2.0, waves);
}