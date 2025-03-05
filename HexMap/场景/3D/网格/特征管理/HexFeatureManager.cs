using Godot;
using Godot.Collections;

public partial class HexFeatureManager : Node3D
{
    [Export] private HexMesh _walls;
    [Export] private Array<Array<BoxMesh>> _urbanMesh, _farmMesh, _plantMesh;
    [Export] private PackedScene[] _special;
    [Export] private PackedScene _wallTower, _bridge;

    private Node _container;


    private BoxMesh PickMesh(Array<Array<BoxMesh>> mesh, int level, float hash, float choice)
    {
        if (level > 0)
        {
            var thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (var i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                    return mesh[i][(int)(choice * _urbanMesh[i].Count)];
            }
        }

        return null;
    }


    public void Init()
    {
        _walls.Init();
    }

    public void Clear()
    {
        _container?.QueueFree();

        _container = new Node();
        AddChild(_container);

        _walls.Clear();
    }

    public void Apply()
    {
        _walls.Apply();
    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        if (cell.IsSpecial)
            return;

        var hash = HexMetrics.SampleHashGrid(position);
        var mesh = PickMesh(_urbanMesh, cell.UrbanLevel, hash.A, hash.D);
        var otherMesh = PickMesh(_farmMesh, cell.FarmLevel, hash.B, hash.D);

        var usedHash = hash.A;
        if (mesh != null)
        {
            if (otherMesh != null && hash.B < hash.A)
            {
                mesh = otherMesh;
                usedHash = hash.B;
            }
        }
        else if (otherMesh != null)
        {
            mesh = otherMesh;
            usedHash = hash.B;
        }

        otherMesh = PickMesh(_plantMesh, cell.PlantLevel, hash.C, hash.D);

        if (mesh != null)
        {
            if (otherMesh != null && hash.C < usedHash)
                mesh = otherMesh;
        }
        else if (otherMesh != null)
            mesh = otherMesh;
        else
            return;

        var feature = new MeshInstance3D();
        feature.Mesh = mesh;
        _container.AddChild(feature);
        feature.GlobalPosition = HexMetrics.Perturb(position);
        feature.GlobalTranslate(Vector3.Up * mesh.Size.Y / 2f);
        feature.GlobalRotate(Vector3.Up, hash.E * Mathf.Pi * 2f);
    }

    #region 城墙

    public void AddWall(
        EdgeVertices near, HexCell nearCell,
        EdgeVertices far, HexCell farCell,
        bool hasRiver, bool hasRoad)
    {
        if (
            nearCell.Walled != farCell.Walled &&
            !nearCell.IsUnderwater && !farCell.IsUnderwater &&
            nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
        {
            AddWallSegment(near.V1, far.V1, near.V2, far.V2);
            if (hasRoad || hasRiver)
            {
                AddWallCap(near.V2, far.V2);
                AddWallCap(far.V4, near.V4);
            }
            else
            {
                AddWallSegment(near.V2, far.V2, near.V3, far.V3);
                AddWallSegment(near.V3, far.V3, near.V4, far.V4);
            }

            AddWallSegment(near.V4, far.V4, near.V5, far.V5);
        }
    }

    public void AddWall(
        Vector3 c1, HexCell cell1,
        Vector3 c2, HexCell cell2,
        Vector3 c3, HexCell cell3)
    {
        if (cell1.Walled)
        {
            if (cell2.Walled)
            {
                if (!cell3.Walled)
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
            }
            else if (cell3.Walled)
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            else
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
        }
        else if (cell2.Walled)
        {
            if (cell3.Walled)
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            else
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
        }
        else if (cell3.Walled)
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
    }

    private void AddWallSegment(
        Vector3 pivot, HexCell pivotCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        if (pivotCell.IsUnderwater)
            return;

        var hasLeftWall =
            !leftCell.IsUnderwater && pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
        var hasRightWall =
            !rightCell.IsUnderwater && pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall)
        {
            if (hasRightWall)
            {
                var hasTower = false;
                if (leftCell.Elevation == rightCell.Elevation)
                {
                    var hash = HexMetrics.SampleHashGrid((pivot + left + right) * (1f / 3f));
                    hasTower = hash.E < HexMetrics.WallTowerThreshold;
                }

                AddWallSegment(pivot, left, pivot, right, hasTower);
            }
            else if (leftCell.Elevation < rightCell.Elevation)
                AddWallWedge(pivot, left, right);
            else
                AddWallCap(pivot, left);
        }
        else if (hasRightWall)
        {
            if (rightCell.Elevation < leftCell.Elevation)
                AddWallWedge(right, pivot, left);
            else
                AddWallCap(right, pivot);
        }
    }

    private void AddWallSegment(
        Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight,
        bool addTower = false)
    {
        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        var left = HexMetrics.WallLerp(nearLeft, farLeft);
        var right = HexMetrics.WallLerp(nearRight, farRight);

        var leftThicknessOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        var rightThicknessOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);

        var leftTop = left.Y + HexMetrics.WallHeight;
        var rightTop = right.Y + HexMetrics.WallHeight;

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.Y = leftTop;
        v4.Y = rightTop;
        _walls.AddQuadUnperturbed(v1, v2, v3, v4);

        var t1 = v3;
        var t2 = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.Y = leftTop;
        v4.Y = rightTop;
        _walls.AddQuadUnperturbed(v2, v1, v4, v3);
        _walls.AddQuadUnperturbed(t1, t2, v3, v4);

        if (addTower)
        {
            var towerIns = _wallTower.Instantiate<Node3D>();
            _container.AddChild(towerIns);
            towerIns.GlobalPosition = (left + right) * 0.5f;
            var targetDir = right - left;
            targetDir.Y = 0;
            towerIns.LookAt(towerIns.GlobalPosition + targetDir);
        }
    }

    private void AddWallCap(Vector3 near, Vector3 far)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);

        var center = HexMetrics.WallLerp(near, far);
        var thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.Y = v4.Y = center.Y + HexMetrics.WallHeight;
        _walls.AddQuadUnperturbed(v1, v2, v3, v4);
    }

    private void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        var center = HexMetrics.WallLerp(near, far);
        var thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        var pointTop = point;
        point.Y = center.Y;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.Y = v4.Y = pointTop.Y = center.Y + HexMetrics.WallHeight;
        _walls.AddQuadUnperturbed(v1, point, v3, pointTop);
        _walls.AddQuadUnperturbed(point, v2, pointTop, v4);
        _walls.AddTriangleUnperturbed(pointTop, v3, v4);
    }

    #endregion

    #region 桥梁

    public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
    {
        roadCenter1 = HexMetrics.Perturb(roadCenter1);
        roadCenter2 = HexMetrics.Perturb(roadCenter2);

        var bridge = _bridge.Instantiate<Node3D>();
        _container.AddChild(bridge);
        bridge.GlobalPosition = (roadCenter1 + roadCenter2) * 0.5f;
        bridge.LookAt(bridge.GlobalPosition + (roadCenter2 - roadCenter1));

        var length = roadCenter1.DistanceTo(roadCenter2);
        bridge.GlobalScale(new Vector3(1f, 1f, length * (1f / HexMetrics.BridgeDesignLength)));
    }

    #endregion

    #region 特殊地形

    public void AddSpecialFeature(HexCell cell, Vector3 position)
    {
        var special = _special[cell.SpecialIndex - 1].Instantiate<Node3D>();
        _container.AddChild(special);
        special.GlobalPosition = HexMetrics.Perturb(position);
        var hash = HexMetrics.SampleHashGrid(position);
        special.GlobalRotate(Vector3.Up, hash.E * Mathf.Pi * 2f);
    }

    #endregion
}