using Godot;

public partial class HexGridChunk : Node3D
{
    [Export] private HexFeatureManager _features;

    [Export] private HexMesh
        _terrain, _river, _roads, _water, _waterShore, _estuaries;

    private static readonly Color Weights1 = new(1f, 0f, 0f);
    private static readonly Color Weights2 = new(0f, 1f, 0f);
    private static readonly Color Weights3 = new(0f, 0f, 1f);

    private HexCell[] _cells;

    public void Init()
    {
        _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        _terrain.Init();
        _river.Init();
        _roads.Init();
        _water.Init();
        _waterShore.Init();
        _estuaries.Init();
        _features.Init();
    }

    public override void _PhysicsProcess(double delta)
    {
        Triangulate();
        SetPhysicsProcess(false);
    }

    private void Triangulate()
    {
        _terrain.Clear();
        _river.Clear();
        _roads.Clear();
        _water.Clear();
        _waterShore.Clear();
        _estuaries.Clear();

        _features.Clear();

        foreach (var cell in _cells)
            Triangulate(cell);

        _terrain.Apply();
        _river.Apply();
        _roads.Apply();
        _water.Apply();
        _waterShore.Apply();
        _estuaries.Apply();

        _features.Apply();
    }

    private void Triangulate(HexCell cell)
    {
        for (var d = HexDirection.SW; d <= HexDirection.SE; d++)
            Triangulate(d, cell);

        if (!cell.IsUnderwater)
        {
            if (!cell.HasRiver && !cell.HasRoads)
                _features.AddFeature(cell, cell.GlobalPosition);

            if (cell.IsSpecial)
                _features.AddSpecialFeature(cell, cell.GlobalPosition);
        }
    }

    #region 地形处理

    private void Triangulate(HexDirection direction, HexCell cell)
    {
        var center = cell.GlobalPosition;

        var e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction));

        //路面六边形中心处理
        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                e.V3.Y = cell.StreamBedY;

                if (cell.HasRiverBeginOrEnd)
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                else
                    TriangulateWithRiver(direction, cell, center, e);
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else
        {
            TriangulateWithoutRiver(direction, cell, center, e);

            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
            {
                _features.AddFeature(cell, (center + e.V1 + e.V5) * (1f / 3f));
            }
        }

        //路面六边形连接处理
        if (direction >= HexDirection.NE)
            TriangulateConnection(direction, cell, e);

        //水面六边形处理
        if (cell.IsUnderwater)
            TriangulateWater(direction, cell, center);
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        var neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
            return;

        var bridge = HexMetrics.GetBridge(direction);
        bridge.Y = neighbor.Position.Y - cell.Position.Y;
        var e2 = new EdgeVertices(e1.V1 + bridge, e1.V5 + bridge);

        var hasRiver = cell.HasRiverThroughEdge(direction);
        var hasRoad = cell.HasRoadThroughEdge(direction);

        if (hasRiver)
        {
            e2.V3.Y = neighbor.StreamBedY;

            if (!cell.IsUnderwater)
            {
                if (!neighbor.IsUnderwater)
                    TriangulateRiverQuad(e1.V2, e1.V4, e2.V2, e2.V4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                        cell.HasIncomingRiver && cell.IncomingRiver == direction);
                else if (cell.Elevation > neighbor.Elevation)
                    TriangulateWaterfallInWater(e1.V2, e1.V4, e2.V2, e2.V4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        neighbor.WaterSurfaceY);
            }
            else if (!neighbor.IsUnderwater && neighbor.Elevation > cell.Elevation)
            {
                TriangulateWaterfallInWater(e2.V4, e2.V2, e1.V4, e1.V2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                    cell.WaterSurfaceY);
            }
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
            TriangulateEdgeTerraces(e1, cell, e2, neighbor, hasRoad);
        else
            TriangulateEdgeStrip(e1, Weights1, cell.Index, e2, Weights2, neighbor.Index, hasRoad);

        _features.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);

        var nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction >= HexDirection.E && nextNeighbor != null)
        {
            var v5 = e1.V5 + HexMetrics.GetBridge(direction.Next());
            v5.Y = nextNeighbor.GlobalPosition.Y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                    TriangulateCorner(e1.V5, cell, e2.V5, neighbor, v5, nextNeighbor);
                else
                    TriangulateCorner(v5, nextNeighbor, e1.V5, cell, e2.V5, neighbor);
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(e2.V5, neighbor, v5, nextNeighbor, e1.V5, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, e1.V5, cell, e2.V5, neighbor);
            }
        }
    }

    //斜坡三角
    private void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var leftEdgeType = bottomCell.GetEdgeType(leftCell);
        var rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            else if (rightEdgeType == HexEdgeType.Flat)
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            else
                TriangulateCornerTerracesCliff(
                    bottom, bottomCell,
                    left, leftCell,
                    right, rightCell);
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            else
                TriangulateCornerCliffTerraces(
                    bottom, bottomCell,
                    left, leftCell,
                    right, rightCell);
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation > rightCell.Elevation)
                TriangulateCornerTerracesCliff(
                    left, leftCell,
                    right, rightCell,
                    bottom, bottomCell);
            else
                TriangulateCornerCliffTerraces(
                    right, rightCell,
                    bottom, bottomCell,
                    left, leftCell);
        }
        else
        {
            _terrain.AddTriangle(bottom, left, right);

            var indices = new Color(bottomCell.Index, leftCell.Index, rightCell.Index);

            _terrain.AddTriangleCellData(indices, Weights1, Weights2, Weights3);
        }

        _features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
    }

    private void TriangulateWithoutRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        TriangulateEdgeFan(center, e, cell.Index);
        if (cell.HasRoads)
        {
            var interpolators = GetRoadInterpolators(direction, cell);
            TriangulateRoad(
                center,
                center.Lerp(e.V1, interpolators.X),
                center.Lerp(e.V5, interpolators.Y),
                e, cell.HasRoadThroughEdge(direction));
        }
    }

    #endregion

    #region 地形差连接处理

    private void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0)
            b = -b;
        var boundary = HexMetrics.Perturb(begin).Lerp(HexMetrics.Perturb(right), b);
        var boundaryWeights = Weights1.Lerp(Weights3, b);

        var indexes = new Color(beginCell.Index, leftCell.Index, rightCell.Index);

        TriangulateBoundaryTriangle(
            begin, Weights1, left, Weights2, boundary, boundaryWeights, indexes);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, Weights2, right, Weights3, boundary, boundaryWeights, indexes);
        }
        else
        {
            _terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            _terrain.AddTriangleCellData(indexes, Weights2, Weights3, boundaryWeights);
        }
    }

    private void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0)
            b = -b;
        var boundary = HexMetrics.Perturb(begin).Lerp(HexMetrics.Perturb(left), b);
        var boundaryWeights = Weights1.Lerp(Weights2, b);

        var indexes = new Color(beginCell.Index, leftCell.Index, rightCell.Index);

        TriangulateBoundaryTriangle(
            right, Weights3, begin, Weights1, boundary, boundaryWeights, indexes);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, Weights2, right, Weights3, boundary, boundaryWeights, indexes);
        }
        else
        {
            _terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            _terrain.AddTriangleCellData(indexes, Weights2, Weights3, boundaryWeights);
        }
    }

    private void TriangulateBoundaryTriangle(
        Vector3 begin, Color beginWeights,
        Vector3 left, Color leftWeights,
        Vector3 boundary, Color boundaryWeights, Color indices)
    {
        var v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        var w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, 1);

        _terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        _terrain.AddTriangleCellData(indices, beginWeights, w2, boundaryWeights);

        for (var i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            var v1 = v2;
            var w1 = w2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, i);
            _terrain.AddTriangleUnperturbed(v1, v2, boundary);
            _terrain.AddTriangleCellData(indices, w1, w2, boundaryWeights);
        }

        _terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        _terrain.AddTriangleCellData(indices, w2, leftWeights, boundaryWeights);
    }

    private void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var v3 = HexMetrics.TerraceLerp(begin, left, 1);
        var v4 = HexMetrics.TerraceLerp(begin, right, 1);
        var w3 = HexMetrics.TerraceLerp(Weights1, Weights2, 1);
        var w4 = HexMetrics.TerraceLerp(Weights1, Weights3, 1);

        var indices = new Color(beginCell.Index, leftCell.Index, rightCell.Index);

        _terrain.AddTriangle(begin, v3, v4);
        _terrain.AddTriangleCellData(indices, Weights1, w3, w4);

        for (var i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            var w1 = w3;
            var w2 = w4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            w3 = HexMetrics.TerraceLerp(Weights1, Weights2, i);
            w4 = HexMetrics.TerraceLerp(Weights1, Weights3, i);
            _terrain.AddQuad(v1, v2, v3, v4);
            _terrain.AddQuadCellData(indices, w1, w2, w3, w4);
        }

        _terrain.AddQuad(v3, v4, left, right);
        _terrain.AddQuadCellData(indices, w3, w4, Weights2, Weights3);
    }

    //斜坡阶梯分化
    private void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell,
        bool hasRoad)
    {
        var e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        var w2 = HexMetrics.TerraceLerp(Weights1, Weights2, 1);

        var i1 = beginCell.Index;
        var i2 = endCell.Index;

        TriangulateEdgeStrip(begin, Weights1, i1, e2, w2, i2, hasRoad);

        for (var i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            var e1 = e2;
            var w1 = w2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            w2 = HexMetrics.TerraceLerp(Weights1, Weights2, i);
            TriangulateEdgeStrip(e1, w1, i1, e2, w2, i2, hasRoad);
        }

        TriangulateEdgeStrip(e2, w2, i1, end, Weights2, i2, hasRoad);
    }

    #endregion

    #region 道路处理

    private void TriangulateRoadSegment(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 v4, Vector3 v5, Vector3 v6
    )
    {
        // V4 \ V5 \ V6
        // V1 \ V2 \ V3
        _roads.AddQuad(v1, v2, v4, v5);
        _roads.AddQuadUv(0f, 1f, 0f, 0f);
        _roads.AddQuad(v2, v3, v5, v6);
        _roads.AddQuadUv(1f, 0f, 0f, 0f);
    }

    private void TriangulateRoad(
        Vector3 center, Vector3 mL, Vector3 mR,
        EdgeVertices e, bool hasRoadThroughCellEdge)
    {
        //没有这个方向的路就组成一个三角形 有就通路
        if (hasRoadThroughCellEdge)
        {
            var mC = mL.Lerp(mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, e.V2, e.V3, e.V4);
            _roads.AddTriangle(center, mL, mC);
            _roads.AddTriangleUv(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            _roads.AddTriangle(center, mC, mR);
            _roads.AddTriangleUv(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
        }
        else
        {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
    {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction))
        {
            interpolators.X = interpolators.Y = 0.5f;
        }
        else
        {
            interpolators.X = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.Y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }

        return interpolators;
    }

    private void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        var hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);

        var previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        var nextHasRiver = cell.HasRiverThroughEdge(direction.Next());

        var interpolators = GetRoadInterpolators(direction, cell);
        var roadCenter = center;

        if (cell.HasRiverBeginOrEnd)
        {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
        {
            Vector3 corner;
            if (previousHasRiver)
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
                    return;

                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else
            {
                if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
                    return;

                corner = HexMetrics.GetFirstSolidCorner(direction);
            }

            roadCenter += corner * 0.5f;

            if (cell.IncomingRiver == direction.Next() &&
                (cell.HasRoadThroughEdge(direction.Next2()) || cell.HasRoadThroughEdge(direction.Opposite())))
            {
                _features.AddBridge(roadCenter, center - corner * 0.5f);
            }

            center += corner * 0.25f;
        }
        // else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
        // {
        //     roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        // }
        // else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
        // {
        //     roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        // }
        // else if (previousHasRiver && nextHasRiver)
        // {
        //     if (!hasRoadThroughEdge)
        //         return;
        //
        //     var offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.InnerToOuter;
        //     roadCenter += offset * 0.7f;
        //     center += offset * 0.5f;
        // }
        // else
        // {
        //     HexDirection middle;
        //     if (previousHasRiver)
        //         middle = direction.Next();
        //     else if (nextHasRiver)
        //         middle = direction.Previous();
        //     else
        //         middle = direction;
        //
        //     if (!cell.HasRoadThroughEdge(middle) &&
        //         !cell.HasRoadThroughEdge(middle.Previous()) &&
        //         !cell.HasRoadThroughEdge(middle.Next()))
        //         return;
        //
        //     var offset = HexMetrics.GetSolidEdgeMiddle(middle);
        //     roadCenter += offset * 0.25f;
        //     if (direction == middle && cell.HasRoadThroughEdge(direction.Opposite()))
        //     {
        //         _features.AddBridge(roadCenter, center - offset * (HexMetrics.InnerToOuter * 0.7f));
        //     }
        // }


        var mL = roadCenter.Lerp(e.V1, interpolators.X);
        var mR = roadCenter.Lerp(e.V5, interpolators.Y);

        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        if (previousHasRiver)
            TriangulateRoadEdge(roadCenter, center, mL);

        if (nextHasRiver)
            TriangulateRoadEdge(roadCenter, mR, center);
    }

    #endregion

    #region 河沟处理

    private void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;

        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = center.Lerp(e.V5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = center.Lerp(e.V1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.InnerToOuter);
        }
        else
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) *
                (0.5f * HexMetrics.InnerToOuter);
            centerR = center;
        }

        //三角形转化为梯形后横截线
        var m = new EdgeVertices(centerL.Lerp(e.V1, 0.5f), centerR.Lerp(e.V5, 0.5f), 1f / 6f);
        m.V3.Y = center.Y = e.V3.Y;

        TriangulateEdgeStrip(m, Weights1, cell.Index, e, Weights1, cell.Index);

        _terrain.AddTriangle(centerL, m.V1, m.V2);
        _terrain.AddQuad(centerL, center, m.V2, m.V3);
        _terrain.AddQuad(center, centerR, m.V3, m.V4);
        _terrain.AddTriangle(centerR, m.V4, m.V5);

        var indices = new Color(cell.Index, cell.Index, cell.Index);

        _terrain.AddTriangleCellData(indices, Weights1);
        _terrain.AddQuadCellData(indices, Weights1);
        _terrain.AddQuadCellData(indices, Weights1);
        _terrain.AddTriangleCellData(indices, Weights1);

        if (!cell.IsUnderwater)
        {
            var reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, m.V2, m.V4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.V2, m.V4, e.V2, e.V4, cell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    private void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        var m = new EdgeVertices(center.Lerp(e.V1, 0.5f), center.Lerp(e.V5, 0.5f));
        m.V3.Y = e.V3.Y;

        TriangulateEdgeStrip(m, Weights1, cell.Index, e, Weights1, cell.Index);
        TriangulateEdgeFan(center, m, cell.Index);

        center.Y = m.V2.Y = m.V4.Y = cell.RiverSurfaceY;

        if (!cell.IsUnderwater)
        {
            var reversed = cell.HasIncomingRiver;

            TriangulateRiverQuad(m.V2, m.V4, e.V2, e.V4, cell.RiverSurfaceY, 0.6f, reversed);
            _river.AddTriangle(center, m.V2, m.V4);

            if (reversed)
                _river.AddTriangleUv(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
            else
                _river.AddTriangleUv(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
        }
    }

    private void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        if (cell.HasRoads)
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);

        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.InnerToOuter * 0.5f;
            }
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        var m = new EdgeVertices(center.Lerp(e.V1, 0.5f), center.Lerp(e.V5, 0.5f));

        TriangulateEdgeStrip(m, Weights1, cell.Index, e, Weights1, cell.Index);
        TriangulateEdgeFan(center, m, cell.Index);

        if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
        {
            _features.AddFeature(cell, (center + e.V1 + e.V5) * (1f / 3f));
        }
    }

    #endregion

    #region 流水处理

    private void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed)
    {
        v1.Y = v2.Y = y1;
        v3.Y = v4.Y = y2;

        _river.AddQuad(v1, v2, v3, v4);

        if (reversed)
            _river.AddQuadUv(1f, 0f, 0.8f - v, 0.6f - v);
        else
            _river.AddQuadUv(0f, 1f, v, v + 0.2f);
    }

    private void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y, float v, bool reversed)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

    private void TriangulateWaterfallInWater(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float waterY)
    {
        v1.Y = v2.Y = y1;
        v3.Y = v4.Y = y2;

        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);

        var t = (waterY - y2) / (y1 - y2);
        v3 = v3.Lerp(v1, t);
        v4 = v4.Lerp(v2, t);

        _river.AddQuadUnperturbed(v1, v2, v3, v4);
        _river.AddQuadUv(0f, 1f, 0.8f, 1f);
    }

    #endregion

    #region 水面处理

    private void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center)
    {
        center.Y = cell.WaterSurfaceY;

        var neighbor = cell.GetNeighbor(direction);

        if (neighbor != null && !neighbor.IsUnderwater)
            TriangulateWaterShore(direction, cell, neighbor, center);
        else
            TriangulateOpenWater(direction, cell, neighbor, center);
    }

    private void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        var c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        var c2 = center + HexMetrics.GetSecondWaterCorner(direction);
        _water.AddTriangle(center, c1, c2);

        //水面连接处理
        if (direction >= HexDirection.NE && neighbor != null)
        {
            var bridge = HexMetrics.GetWaterBridge(direction);
            var e1 = c1 + bridge;
            var e2 = c2 + bridge;

            _water.AddQuad(c1, c2, e1, e2);

            if (direction is >= HexDirection.NE and <= HexDirection.E)
            {
                var nextCell = cell.GetNeighbor(direction.Next());
                if (nextCell == null || !nextCell.IsUnderwater)
                    return;

                _water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
            }
        }
    }

    private void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center)
    {
        var e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction));

        _water.AddTriangle(center, e1.V1, e1.V2);
        _water.AddTriangle(center, e1.V2, e1.V3);
        _water.AddTriangle(center, e1.V3, e1.V4);
        _water.AddTriangle(center, e1.V4, e1.V5);

        var center2 = neighbor.Position;
        center2.Y = center.Y;

        var e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite()));

        if (cell.HasRiverThroughEdge(direction))
        {
            TriangulateEstuary(e1, e2, cell.IncomingRiver == direction);
        }
        else
        {
            _waterShore.AddQuad(e1.V1, e1.V2, e2.V1, e2.V2);
            _waterShore.AddQuad(e1.V2, e1.V3, e2.V2, e2.V3);
            _waterShore.AddQuad(e1.V3, e1.V4, e2.V3, e2.V4);
            _waterShore.AddQuad(e1.V4, e1.V5, e2.V4, e2.V5);

            _waterShore.AddQuadUv(0f, 0f, 0f, 1f);
            _waterShore.AddQuadUv(0f, 0f, 0f, 1f);
            _waterShore.AddQuadUv(0f, 0f, 0f, 1f);
            _waterShore.AddQuadUv(0f, 0f, 0f, 1f);
        }


        var nextNeighbor = cell.GetNeighbor(direction.Next());
        if (nextNeighbor != null)
        {
            var v3 = nextNeighbor.Position + (
                nextNeighbor.IsUnderwater
                    ? HexMetrics.GetFirstWaterCorner(direction.Previous())
                    : HexMetrics.GetFirstSolidCorner(direction.Previous()));
            v3.Y = center.Y;

            _waterShore.AddTriangle(e1.V5, e2.V5, v3);
            _waterShore.AddTriangleUv(
                new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0, nextNeighbor.IsUnderwater ? 0 : 1));
        }
    }

    private void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver)
    {
        _waterShore.AddTriangle(e2.V1, e1.V2, e1.V1);
        _waterShore.AddTriangle(e2.V5, e1.V5, e1.V4);
        _waterShore.AddTriangleUv(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        _waterShore.AddTriangleUv(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

        _estuaries.AddQuad(e2.V1, e1.V2, e2.V2, e1.V3);
        _estuaries.AddTriangle(e1.V3, e2.V2, e2.V4);
        _estuaries.AddQuad(e1.V3, e1.V4, e2.V4, e2.V5);
        _estuaries.AddQuadUv(
            new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 0f));
        _estuaries.AddTriangleUv(new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        _estuaries.AddQuadUv(
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        );

        if (incomingRiver)
        {
            _estuaries.AddQuadUv2(
                new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
                new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
            _estuaries.AddTriangleUv2(new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
            _estuaries.AddQuadUv2(
                new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
        }
        else
        {
            _estuaries.AddQuadUv2(
                new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                new Vector2(0f, 0f), new Vector2(0.5f, -0.3f)
            );
            _estuaries.AddTriangleUv2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
            _estuaries.AddQuadUv2(
                new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
            );
        }
    }

    #endregion

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
    {
        _terrain.AddTriangle(center, edge.V1, edge.V2);
        _terrain.AddTriangle(center, edge.V2, edge.V3);
        _terrain.AddTriangle(center, edge.V3, edge.V4);
        _terrain.AddTriangle(center, edge.V4, edge.V5);

        var indices = new Color(index, index, index);
        _terrain.AddTriangleCellData(indices, Weights1);
        _terrain.AddTriangleCellData(indices, Weights1);
        _terrain.AddTriangleCellData(indices, Weights1);
        _terrain.AddTriangleCellData(indices, Weights1);
    }

    private void TriangulateEdgeStrip(
        EdgeVertices e1, Color w1, float index1,
        EdgeVertices e2, Color w2, float index2,
        bool hasRoad = false)
    {
        _terrain.AddQuad(e1.V1, e1.V2, e2.V1, e2.V2);
        _terrain.AddQuad(e1.V2, e1.V3, e2.V2, e2.V3);
        _terrain.AddQuad(e1.V3, e1.V4, e2.V3, e2.V4);
        _terrain.AddQuad(e1.V4, e1.V5, e2.V4, e2.V5);

        var indices = new Color(index1, index2, index1);
        _terrain.AddQuadCellData(indices, w1, w2);
        _terrain.AddQuadCellData(indices, w1, w2);
        _terrain.AddQuadCellData(indices, w1, w2);
        _terrain.AddQuadCellData(indices, w1, w2);

        if (hasRoad)
            TriangulateRoadSegment(e1.V2, e1.V3, e1.V4, e2.V2, e2.V3, e2.V4);
    }

    private void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
    {
        _roads.AddTriangle(center, mL, mR);
        _roads.AddTriangleUv(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
    }

    public void Refresh() => SetPhysicsProcess(true);

    public void AddCell(int index, HexCell cell)
    {
        _cells[index] = cell;
        AddChild(cell);
    }
}