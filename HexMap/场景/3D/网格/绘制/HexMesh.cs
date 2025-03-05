using System;
using System.Collections.Generic;
using Godot;

public partial class HexMesh : MeshInstance3D
{
    [Export] private bool
        _useCollider, _useCellData, _useUvCoordinates, _useUv2Coordinates;

    [Export] private ShaderMaterial _material;
    [Export] private Color _debugColor;

    [NonSerialized] private List<Vector3> _vertices;
    [NonSerialized] private List<int> _verticesIndex;

    [NonSerialized] private List<Color> _cellWeights, _cellIndices;
    [NonSerialized] private List<Vector2> _uvS, _uv2S;

    private ConcavePolygonShape3D _polygonShapeShape;
    private SurfaceTool _sf;

    public void Init()
    {
        _sf = new SurfaceTool();
        InitCollider();
    }

    private void InitCollider()
    {
        if (!_useCollider) return;

        var staticBody = new StaticBody3D();
        var collisionShape = new CollisionShape3D();

        _polygonShapeShape = new ConcavePolygonShape3D();
        collisionShape.Shape = _polygonShapeShape;

        collisionShape.DebugColor = _debugColor;

        staticBody.AddChild(collisionShape);
        AddChild(staticBody);
    }

    #region 三角形和四边形基本绘制

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var vertexIndex = _vertices.Count;
        _vertices.Add(HexMetrics.Perturb(v1));
        _vertices.Add(HexMetrics.Perturb(v2));
        _vertices.Add(HexMetrics.Perturb(v3));

        _verticesIndex.Add(vertexIndex);
        _verticesIndex.Add(vertexIndex + 1);
        _verticesIndex.Add(vertexIndex + 2);
    }

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        var vertexIndex = _vertices.Count;

        _vertices.Add(HexMetrics.Perturb(v1));
        _vertices.Add(HexMetrics.Perturb(v2));
        _vertices.Add(HexMetrics.Perturb(v3));
        _vertices.Add(HexMetrics.Perturb(v4));

        _verticesIndex.Add(vertexIndex);
        _verticesIndex.Add(vertexIndex + 2);
        _verticesIndex.Add(vertexIndex + 1);
        _verticesIndex.Add(vertexIndex + 1);
        _verticesIndex.Add(vertexIndex + 2);
        _verticesIndex.Add(vertexIndex + 3);
    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var vertexIndex = _vertices.Count;
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);

        _verticesIndex.Add(vertexIndex);
        _verticesIndex.Add(vertexIndex + 1);
        _verticesIndex.Add(vertexIndex + 2);
    }

    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        var vertexIndex = _vertices.Count;

        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _vertices.Add(v4);

        _verticesIndex.Add(vertexIndex);
        _verticesIndex.Add(vertexIndex + 2);
        _verticesIndex.Add(vertexIndex + 1);
        _verticesIndex.Add(vertexIndex + 1);
        _verticesIndex.Add(vertexIndex + 2);
        _verticesIndex.Add(vertexIndex + 3);
    }

    #endregion

    #region Uv

    public void AddTriangleUv(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        _uvS.Add(uv1);
        _uvS.Add(uv2);
        _uvS.Add(uv3);
    }

    public void AddQuadUv(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        _uvS.Add(uv1);
        _uvS.Add(uv2);
        _uvS.Add(uv3);
        _uvS.Add(uv4);
    }

    public void AddQuadUv(float uMin, float uMax, float vMin, float vMax)
    {
        _uvS.Add(new Vector2(uMin, vMin));
        _uvS.Add(new Vector2(uMax, vMin));
        _uvS.Add(new Vector2(uMin, vMax));
        _uvS.Add(new Vector2(uMax, vMax));
    }

    public void AddTriangleUv2(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        _uv2S.Add(uv1);
        _uv2S.Add(uv2);
        _uv2S.Add(uv3);
    }

    public void AddQuadUv2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        _uv2S.Add(uv1);
        _uv2S.Add(uv2);
        _uv2S.Add(uv3);
        _uv2S.Add(uv4);
    }

    public void AddQuadUv2(float uMin, float uMax, float vMin, float vMax)
    {
        _uv2S.Add(new Vector2(uMin, vMin));
        _uv2S.Add(new Vector2(uMax, vMin));
        _uv2S.Add(new Vector2(uMin, vMax));
        _uv2S.Add(new Vector2(uMax, vMax));
    }

    #endregion

    #region 图块编号

    public void AddTriangleCellData(Color indices, Color weights1, Color weights2, Color weights3)
    {
        _cellIndices.Add(indices);
        _cellIndices.Add(indices);
        _cellIndices.Add(indices);
        _cellWeights.Add(weights1);
        _cellWeights.Add(weights2);
        _cellWeights.Add(weights3);
    }

    public void AddTriangleCellData(Color indices, Color weights)
    {
        AddTriangleCellData(indices, weights, weights, weights);
    }

    public void AddQuadCellData(Color indices, Color weights1, Color weights2, Color weights3, Color weights4)
    {
        _cellIndices.Add(indices);
        _cellIndices.Add(indices);
        _cellIndices.Add(indices);
        _cellIndices.Add(indices);
        _cellWeights.Add(weights1);
        _cellWeights.Add(weights2);
        _cellWeights.Add(weights3);
        _cellWeights.Add(weights4);
    }

    public void AddQuadCellData(Color indices, Color weights1, Color weights2)
    {
        AddQuadCellData(indices, weights1, weights1, weights2, weights2);
    }

    public void AddQuadCellData(Color indices, Color weights)
    {
        AddQuadCellData(indices, weights, weights, weights, weights);
    }

    #endregion

    public void Clear()
    {
        _sf.Clear();
        _sf.Begin(Mesh.PrimitiveType.Triangles);
        _sf.SetCustomFormat(0, SurfaceTool.CustomFormat.RgbFloat);

        _vertices = ListPool<Vector3>.Get();
        _verticesIndex = ListPool<int>.Get();


        if (_useUvCoordinates)
            _uvS = ListPool<Vector2>.Get();

        if (_useUv2Coordinates)
            _uv2S = ListPool<Vector2>.Get();

        if (_useCellData)
        {
            _cellWeights = ListPool<Color>.Get();
            _cellIndices = ListPool<Color>.Get();
        }
    }

    public void Apply()
    {
        for (var i = 0; i < _vertices.Count; i++)
        {
            if (_useUvCoordinates)
                _sf.SetUV(_uvS[i]);
            if (_useUv2Coordinates)
                _sf.SetUV2(_uv2S[i]);

            if (_useCellData)
            {
                _sf.SetColor(_cellWeights[i]);
                _sf.SetCustom(0, _cellIndices[i]);
            }

            _sf.SetSmoothGroup(uint.MaxValue);
            _sf.SetMaterial(_material);
            _sf.AddVertex(_vertices[i]);
        }

        foreach (var t in _verticesIndex)
            _sf.AddIndex(t);

        _sf.Index();
        _sf.GenerateNormals();

        Mesh = _sf.Commit();

        _polygonShapeShape?.SetFaces(Mesh.GetFaces());

        ListPool<Vector3>.Add(_vertices);
        ListPool<int>.Add(_verticesIndex);

        if (_useUvCoordinates)
            ListPool<Vector2>.Add(_uvS);
        if (_useUv2Coordinates)
            ListPool<Vector2>.Add(_uv2S);
        if (_useCellData)
        {
            ListPool<Color>.Add(_cellWeights);
            ListPool<Color>.Add(_cellIndices);
        }
    }
}