using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CinematicUnityExplorer.LineDrawing
{
    public class LineData
    {
        public Vector2 Start { get; }
        public Vector2 End { get; }
        public Color Color { get; }
        public int Depth { get; }
        public float Width { get; }

        public LineData(Vector2 start, Vector2 end, Color color, int depth, float width = 0.7f)
        {
            Start = start;
            End = end;
            Color = color;
            Depth = depth;
            Width = width;
        }
    }
    internal interface ILineProvider
    {
        List<LineData> Lines { get; }
    }
}
