using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Small line icon drawn in code, so the UI needs no icon image files.</summary>
public class UiIcon : VisualElement
{
    public enum Kind { Play, Pause, Previous, Next, List, Move, Close }

    Kind kind;
    static readonly Color Tint = new Color(0.94f, 0.85f, 0.65f);

    public UiIcon(Kind kind)
    {
        this.kind = kind;
        pickingMode = PickingMode.Ignore;
        style.width = 20;
        style.height = 20;
        generateVisualContent += Draw;
    }

    public void Set(Kind k)
    {
        kind = k;
        MarkDirtyRepaint();
    }

    /// <summary>Puts an icon inside a button and returns it.</summary>
    public static UiIcon AddTo(VisualElement button, Kind kind)
    {
        var icon = new UiIcon(kind);
        if (button != null) button.Add(icon);
        return icon;
    }

    void Draw(MeshGenerationContext ctx)
    {
        float s = contentRect.width / 24f;
        if (s <= 0f) return;
        var p = ctx.painter2D;
        p.strokeColor = Tint;
        p.fillColor = Tint;
        p.lineWidth = 2f * s;
        p.lineCap = LineCap.Round;
        p.lineJoin = LineJoin.Round;

        switch (kind)
        {
            case Kind.Play:
                Poly(p, s, true, 7, 4, 20, 12, 7, 20);
                break;
            case Kind.Pause:
                Line(p, s, 8, 5, 8, 19);
                Line(p, s, 16, 5, 16, 19);
                break;
            case Kind.Previous:
                Poly(p, s, true, 19, 20, 9, 12, 19, 4);
                Line(p, s, 5, 5, 5, 19);
                break;
            case Kind.Next:
                Poly(p, s, true, 5, 4, 15, 12, 5, 20);
                Line(p, s, 19, 5, 19, 19);
                break;
            case Kind.List:
                Line(p, s, 8, 6, 21, 6);
                Line(p, s, 8, 12, 21, 12);
                Line(p, s, 8, 18, 21, 18);
                Line(p, s, 3, 6, 3.5f, 6);
                Line(p, s, 3, 12, 3.5f, 12);
                Line(p, s, 3, 18, 3.5f, 18);
                break;
            case Kind.Move:
                Line(p, s, 2, 12, 22, 12);
                Line(p, s, 12, 2, 12, 22);
                Poly(p, s, false, 5, 9, 2, 12, 5, 15);
                Poly(p, s, false, 9, 5, 12, 2, 15, 5);
                Poly(p, s, false, 15, 19, 12, 22, 9, 19);
                Poly(p, s, false, 19, 9, 22, 12, 19, 15);
                break;
            case Kind.Close:
                Line(p, s, 6, 6, 18, 18);
                Line(p, s, 18, 6, 6, 18);
                break;
        }
    }

    static void Line(Painter2D p, float s, float x1, float y1, float x2, float y2)
    {
        p.BeginPath();
        p.MoveTo(new Vector2(x1 * s, y1 * s));
        p.LineTo(new Vector2(x2 * s, y2 * s));
        p.Stroke();
    }

    static void Poly(Painter2D p, float s, bool filled, params float[] pts)
    {
        p.BeginPath();
        p.MoveTo(new Vector2(pts[0] * s, pts[1] * s));
        for (int i = 2; i < pts.Length; i += 2)
            p.LineTo(new Vector2(pts[i] * s, pts[i + 1] * s));
        if (filled)
        {
            p.ClosePath();
            p.Fill();
        }
        p.Stroke();
    }
}
