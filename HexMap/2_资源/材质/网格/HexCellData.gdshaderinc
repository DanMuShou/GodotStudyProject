global uniform sampler2D _hexCellDate: source_color, filter_nearest;
global uniform vec4 _hexCellDataTexelSize;

#include "ShaderExpand/HexMetrics.gdshaderinc"

vec4 GetCellData(vec4 cellCoord, int index){
	vec2 uv;
	uv.x = (cellCoord[index] + 0.5) * _hexCellDataTexelSize.x;
	float row = floor(uv.x);
	uv.x -= row;
	uv.y = (row + 0.5) * _hexCellDataTexelSize.y;
	vec4 data = textureLod(_hexCellDate, uv, 0);
	data.w *= 255.0;
	return data;
}