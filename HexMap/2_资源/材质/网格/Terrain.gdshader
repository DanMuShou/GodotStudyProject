shader_type spatial;

uniform bool _gridOn = true;
uniform float text1 = 1.0;
uniform sampler2DArray _mainTex : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D _gridTex : source_color;

#include "HexCellData.gdshaderinc"

varying vec3 terrainType;
varying vec4 cellVis;
varying vec4 test;

void vertex() {
	vec4 cell0 = GetCellData(CUSTOM0, 0);
	vec4 cell1 = GetCellData(CUSTOM0, 1);
	vec4 cell2 = GetCellData(CUSTOM0, 2);
	
	test = cell0;
	
	terrainType.x = cell0.a;
	terrainType.y = cell1.a;
	terrainType.z = cell2.a;
	
	cellVis.x = cell0.r;
	cellVis.y = cell1.r;
	cellVis.z = cell2.r;
	cellVis.xyz = mix(vec3(0.25), vec3(1.0), cellVis.xyz);
}

vec4 GetTerrainColor(vec3 worldPos, int index, vec3 colorMin, vec4 visibility){
	vec3 uvw = vec3(worldPos.xz * 0.2, terrainType[index]);
	vec4 c = texture(_mainTex, uvw);
	return c * colorMin[index] * cellVis[index];
}

void fragment() {
	vec3 worldPos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	
	//vec3 colorMix = COLOR.rgb;
	vec4 c = 
		GetTerrainColor(worldPos, 0, COLOR.rgb, cellVis) +
		GetTerrainColor(worldPos, 1, COLOR.rgb, cellVis) +
		GetTerrainColor(worldPos, 2, COLOR.rgb, cellVis);
	
	vec4 grid = vec4(1.0);
	if(_gridOn){
		vec2 gridUV = worldPos.xz;
		gridUV.x *= 10.0 / (4.0 * 8.66025404);
		gridUV.y *= 10.0 / (2.0 * 15.0);
		grid = texture(_gridTex, gridUV);
	}
	
	ALBEDO = c.rgb * grid.rgb;
}	