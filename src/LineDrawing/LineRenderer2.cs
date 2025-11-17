using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CinematicUnityExplorer.LineDrawing
{
    internal class LineRenderer2 : SingleMonoBehaviour<LineRenderer2>
    {
        public List<ILineProvider> providers = new();
        private void OnGUI()
        {
            if (Event.current?.type != EventType.Repaint) return;

            foreach (var v in providers)
            {
                foreach (var l in v.Lines)
                {
                    var prevDepth = GUI.depth;
                    GUI.depth = l.Depth;

                    Drawing.DrawLine(l.Start, l.End, l.Color, l.Width, false);
                    GUI.depth = prevDepth;
                }
            }
        }
    }
}
