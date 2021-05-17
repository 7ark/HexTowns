using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexagonSelectionHandler : MonoBehaviour
{
    [SerializeField]
    private HexBoardChunkHandler chunkHandler;

    public static HexagonSelectionHandler Instance;

    private void Awake()
    {
        Instance = this;
    }

    public HexTile[] GetAllTilesBetweenTwoTiles(HexTile one, HexTile two)
    {
        int distance = HexCoordinates.HexDistance(one.Coordinates, two.Coordinates);
        if(distance == 0)
        {
            return new HexTile[0];
        }
        List<HexTile> tilesBetween = new List<HexTile>();
        for (int i = 0; i < distance + 1; i++)
        {
            Vector3 position = Vector3.Lerp(one.Position, two.Position, ((float)i / (float)distance));
            HexCoordinates coord = HexCoordinates.FromPosition(position);
            tilesBetween.Add(chunkHandler.GetTileFromCoordinate(coord));
        }

        return tilesBetween.ToArray();
    }

    public List<HexTile> GetBorderBetweenTiles(HexTile[] tiles)
    {
        List<HexTile> borders = new List<HexTile>();
        for (int i = 0; i < tiles.Length; i++)
        {
            int secondIndex = i + 1;
            if(i == tiles.Length - 1)
            {
                secondIndex = 0;
            }
            HexTile[] lineTiles = GetAllTilesBetweenTwoTiles(tiles[i], tiles[secondIndex]);
            for (int j = 0; j < lineTiles.Length; j++)
            {
                if (!borders.Contains(lineTiles[j]))
                {
                    borders.Add(lineTiles[j]);
                }
            }
        }

        return borders;
    }

    public List<List<HexTile>> GetBordersBetweenTilesSeperated(HexTile[] tiles)
    {
        List<List<HexTile>> borderLinesIndividual = new List<List<HexTile>>();
        for (int i = 0; i < tiles.Length; i++)
        {
            int secondIndex = i + 1;
            if (i == tiles.Length - 1)
            {
                secondIndex = 0;
            }
            borderLinesIndividual.Add(new List<HexTile>(GetAllTilesBetweenTwoTiles(tiles[i], tiles[secondIndex])));
        }

        return borderLinesIndividual;
    }

    public bool DoBordersCross(HexTile[] cornerTiles)
    {
        if(cornerTiles.Length <= 3)
        {
            return false;
        }
        List<List<HexTile>> borderLinesIndividual = GetBordersBetweenTilesSeperated(cornerTiles);

        for (int i = 0; i < borderLinesIndividual.Count; i++)
        {
            for (int i2 = i + 2; i2 < i + borderLinesIndividual.Count - 1; i2++)
            {
                int secondIndex = i2;
                if (secondIndex > borderLinesIndividual.Count - 1)
                {
                    secondIndex -= borderLinesIndividual.Count;
                }

                HexTile[] first = borderLinesIndividual[i].ToArray();
                HexTile[] second = borderLinesIndividual[secondIndex].ToArray();
                for (int j = 0; j < first.Length; j++)
                {
                    for (int k = 0; k < second.Length; k++)
                    {
                        if (first[j] == second[k])
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public HashSet<HexTile> GetFilledAreaBetweenTiles(HexTile[] tiles)
    {
        var borders = new HashSet<HexTile>(GetBorderBetweenTiles(tiles));
        if(tiles.Length >= 3)
        {
            int left = int.MaxValue;
            int right = int.MinValue;
            int top = int.MinValue;
            int bottom = int.MaxValue;
            foreach (var border in borders) {
                if (border.Coordinates.X < left)
                {
                    left = border.Coordinates.X;
                }
                if (border.Coordinates.X > right)
                {
                    right = border.Coordinates.X;
                }
                if (border.Coordinates.Y < bottom)
                {
                    bottom = border.Coordinates.Y;
                }
                if (border.Coordinates.Y > top)
                {
                    top = border.Coordinates.Y;
                }
            }

            var filledTiles = new HashSet<HexTile>(borders);
            List<HexCoordinates> allPoints = new List<HexCoordinates>();
            for (int x = left; x <= right; x++)
            {
                for (int y = bottom; y < top; y++)
                {
                    allPoints.Add(new HexCoordinates(x, y));
                }
            }

            for (int p = 0; p < allPoints.Count; p++)
            {
                HexCoordinates point = allPoints[p];

                bool c = false;
                int j = tiles.Length - 1;
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (((tiles[i].Coordinates.Y > point.Y) != (tiles[j].Coordinates.Y > point.Y)) &&
                        ((float)point.X < (float)tiles[i].Coordinates.X + ((float)tiles[j].Coordinates.X - (float)tiles[i].Coordinates.X) * ((float)point.Y - (float)tiles[i].Coordinates.Y) /
                        ((float)tiles[j].Coordinates.Y - (float)tiles[i].Coordinates.Y)))
                    {
                        c = !c;
                    }
                    j = i;
                }

                if(c)
                {
                    HexTile tile = chunkHandler.GetTileFromCoordinate(point);
                    filledTiles.Add(tile);
                }
            }

            //float left = int.MaxValue;
            //float right = int.MinValue;
            //float top = int.MinValue;
            //float bottom = int.MaxValue;
            //for (int i = 0; i < borders.Count; i++)
            //{
            //    if(borders[i].Position.x < left)
            //    {
            //        left = borders[i].Position.x;
            //    }
            //    if (borders[i].Position.x > right)
            //    {
            //        right = borders[i].Position.x;
            //    }
            //    if (borders[i].Position.z < bottom)
            //    {
            //        bottom = borders[i].Position.z;
            //    }
            //    if (borders[i].Position.z > top)
            //    {
            //        top = borders[i].Position.z;
            //    }
            //}
            //
            //bool drawShit = UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed;
            //
            //List<LineEquation> borderLines = new List<LineEquation>();
            //for (int i = 0; i < tiles.Length; i++)
            //{
            //    int secondIndex = i == tiles.Length - 1 ? 0 : i + 1;
            //    LineEquation line = new LineEquation(new Vector2(tiles[i].Position.x, tiles[i].Position.z), new Vector2(tiles[secondIndex].Position.x, tiles[secondIndex].Position.z));
            //    borderLines.Add(line);
            //    if(drawShit)
            //    Debug.DrawLine(new Vector3(tiles[i].Position.x, 100, tiles[i].Position.z), new Vector3(tiles[secondIndex].Position.x, 100, tiles[secondIndex].Position.z), Color.black, 10);
            //}
            //List<HexTile> filledTiles = new List<HexTile>(borders);
            //List<HexCoordinates> borderCoordinates = new List<HexCoordinates>();
            //for (int i = 0; i < borders.Count; i++)
            //{
            //    borderCoordinates.Add(borders[i].Coordinates);
            //}
            //
            //int timesDrawn = 0;
            //for (float y = top; y >= bottom; y--)
            //{
            //    Vector2 leftPosition = new Vector2(left - 20, y);
            //    for (float x = left; x <= right; x++)
            //    {
            //        HexCoordinates coords = HexCoordinates.FromPosition(new Vector3(x, 0, y));
            //        Vector2 pos = new Vector2(x, y);
            //        LineEquation line = new LineEquation(leftPosition, pos);
            //
            //        int borderCrosses = 0;
            //        for (int i = 0; i < borderLines.Count; i++)
            //        {
            //            if(line.Intersects(borderLines[i]))
            //            {
            //                borderCrosses++;
            //            }
            //        }
            //
            //        if (borderCrosses % 2 != 0)
            //        {
            //            HexTile tile = chunkHandler.GetTileFromCoordinate(coords);
            //            if(!filledTiles.Contains(tile))
            //            {
            //                filledTiles.Add(tile);
            //
            //                if (drawShit)
            //                {
            //                    Debug.DrawLine(new Vector3(line.Start.x, 100 + timesDrawn * 0.01f, line.Start.y), new Vector3(line.End.x, 100 + timesDrawn * 0.01f, line.End.y), Color.green, 10);
            //                    timesDrawn++;
            //                }
            //
            //            }
            //        }
            //    }
            //}

            return filledTiles;
        }
        else
        {
            return borders;
        }


    }
    public static bool AreLinesIntersecting(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2, bool shouldIncludeEndPoints)
    {
        //To avoid floating point precision issues we can add a small value
        float epsilon = 0.00001f;

        bool isIntersecting = false;

        float denominator = (l2_p2.y - l2_p1.y) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.y - l1_p1.y);

        //Make sure the denominator is > 0, if not the lines are parallel
        if (denominator != 0f)
        {
            float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.y - l2_p1.y) - (l2_p2.y - l2_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;
            float u_b = ((l1_p2.x - l1_p1.x) * (l1_p1.y - l2_p1.y) - (l1_p2.y - l1_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;

            //Are the line segments intersecting if the end points are the same
            if (shouldIncludeEndPoints)
            {
                //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
                if (u_a >= 0f + epsilon && u_a <= 1f - epsilon && u_b >= 0f + epsilon && u_b <= 1f - epsilon)
                {
                    isIntersecting = true;
                }
            }
            else
            {
                //Is intersecting if u_a and u_b are between 0 and 1
                if (u_a > 0f + epsilon && u_a < 1f - epsilon && u_b > 0f + epsilon && u_b < 1f - epsilon)
                {
                    isIntersecting = true;
                }
            }
        }

        return isIntersecting;
    }
    private void Fill(HexTile tile, ref List<HexTile> filledArea)
    {
        if(filledArea.Contains(tile))
        {
            return;
        }

        filledArea.Add(tile);

        var neighbors = tile.Neighbors;
        foreach (var neighbor in neighbors) {
            Fill(neighbor, ref filledArea);
        }
    }
}
public class LineEquation
{
    public LineEquation(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;

        IsVertical = Mathf.Abs(End.x - start.x) < 0.00001f;
        M = (End.y - Start.y) / (End.x - Start.x);
        A = -M;
        B = 1;
        C = Start.y - M * Start.x;
    }

    public bool IsVertical { get; private set; }

    public float M { get; private set; }

    public Vector2 Start { get; private set; }
    public Vector2 End { get; private set; }

    public float A { get; private set; }
    public float B { get; private set; }
    public float C { get; private set; }
    private struct Line
    {
        public Vector2 p1, p2;
    };

    private bool OnLine(Line line1, Vector2 p)
    {   //check whether p is on the line or not
        if (p.x <= Mathf.Max(line1.p1.x, line1.p2.x) && p.x <= Mathf.Min(line1.p1.x, line1.p2.x) &&
           (p.y <= Mathf.Max(line1.p1.y, line1.p2.y) && p.y <= Mathf.Min(line1.p1.y, line1.p2.y)))
        {
            return true;
        }

        return false;
    }

    private int Direction(Vector2 a, Vector2 b, Vector2 c)
    {
        int val = (int)((b.y - a.y) * (c.x - b.x) - (b.x - a.x) * (c.y - b.y));
        if (val == 0)
        {
            return 0;
        }
        else if (val < 0)
        {
            //CCW direction
            return 2;
        }
        else
        {
            //CW direction
            return 1;
        }
    }

    private bool IsIntersect(Line line1, Line line2)
    {
        int dir1 = Direction(line1.p1, line1.p2, line2.p1);
        int dir2 = Direction(line1.p1, line1.p2, line2.p2);
        int dir3 = Direction(line2.p1, line2.p2, line1.p1);
        int dir4 = Direction(line2.p1, line2.p2, line1.p2);

        if (dir1 != dir2 && dir3 != dir4)
            return true; //Intersecting

        if (dir1 == 0 && OnLine(line1, line2.p1)) //When p2 of line2 are on the line1
            return true;

        if (dir2 == 0 && OnLine(line1, line2.p2)) //When p1 of line2 are on the line1
            return true;

        if (dir3 == 0 && OnLine(line2, line1.p1)) //When p2 of line1 are on the line2
            return true;

        if (dir4 == 0 && OnLine(line2, line1.p2)) //When p1 of line1 are on the line2
            return true;

        return false;
    }

    public bool Intersects(LineEquation otherLine)
    {
        return IsIntersect(new Line() { p1 = Start, p2 = End }, new Line() { p1 = otherLine.Start, p2 = otherLine.End });
    }


    public bool IntersectsWithLine(LineEquation otherLine, out Vector2 intersectionPoint)
    {
        intersectionPoint = new Vector2(0, 0);

        if (!IsIntersect(new Line() { p1 = Start, p2 = End }, new Line() { p1 = otherLine.Start, p2 = otherLine.End }))
        {
            return false;
        }

        if (IsVertical && otherLine.IsVertical)
        {
            return false;
        }
        if (IsVertical || otherLine.IsVertical)
        {
            intersectionPoint = GetIntersectionPointIfOneIsVertical(otherLine, this);
            return true;
        }
        float delta = A * otherLine.B - otherLine.A * B;
        bool hasIntersection = Mathf.Abs(delta - 0) > 0.0001f;
        if (hasIntersection)
        {
            float x = (otherLine.B * C - B * otherLine.C) / delta;
            float y = (A * otherLine.C - otherLine.A * C) / delta;
            intersectionPoint = new Vector2(x, y);
        }
        return hasIntersection;
    }

    private static Vector2 GetIntersectionPointIfOneIsVertical(LineEquation line1, LineEquation line2)
    {
        LineEquation verticalLine = line2.IsVertical ? line2 : line1;
        LineEquation nonVerticalLine = line2.IsVertical ? line1 : line2;

        float y = (verticalLine.Start.x - nonVerticalLine.Start.x) *
                   (nonVerticalLine.End.y - nonVerticalLine.Start.y) /
                   ((nonVerticalLine.End.x - nonVerticalLine.Start.x)) +
                   nonVerticalLine.Start.y;
        float x = line1.IsVertical ? line1.Start.x : line2.Start.x;
        return new Vector2(x, y);
    }

    public override string ToString()
    {
        return "[" + Start + "], [" + End + "]";
    }
}