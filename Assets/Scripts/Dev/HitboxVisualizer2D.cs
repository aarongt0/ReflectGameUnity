using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// Scene-view visualization for 2D hitboxes (BoxCollider2D + CapsuleCollider2D).
[ExecuteAlways]
[DisallowMultipleComponent]
public class HitboxVisualizer2D : MonoBehaviour
{
    public bool StopAllVisualizations = true;

    [Header("Visibility")]
    [Tooltip("If false, only shows when the collider is enabled and GameObject is active in hierarchy.")]
    public bool showWhenInactive = false;

    [Header("Style")]
    public Color hitboxColor = Color.red;
    [Range(0f, 1f)] public float fillAlpha = 0.15f;
    [Range(0f, 1f)] public float outlineAlpha = 0.9f;
    public string label = "HITBOX";

    private CapsuleCollider2D _capsule;
    private BoxCollider2D _box;

    private void OnEnable()
    {
        Cache();
    }

    private void OnValidate()
    {
        Cache();
    }

    private void Cache()
    {
        _capsule = GetComponent<CapsuleCollider2D>();
        _box = GetComponent<BoxCollider2D>();
    }

    private bool IsActiveForViz(Collider2D col)
    {
        if (!gameObject.activeInHierarchy) return false;
        if (col == null) return false;
        return col.enabled;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (StopAllVisualizations) return;

        if (_capsule == null && _box == null) Cache();
        Collider2D chosen = (Collider2D)_capsule != null ? _capsule : _box;
        if (chosen == null) return;

        bool active = IsActiveForViz(chosen);
        if (!active && !showWhenInactive) return;

        float aFill = active ? fillAlpha : fillAlpha * 0.25f;
        float aLine = active ? outlineAlpha : outlineAlpha * 0.4f;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        // Draw in a matrix aligned to the collider's offset/rotation/scale.
        Vector3 worldCenter = transform.TransformPoint(chosen.offset);
        Matrix4x4 old = Handles.matrix;
        Handles.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, transform.lossyScale);

        Color fill = new Color(hitboxColor.r, hitboxColor.g, hitboxColor.b, aFill);
        Color line = new Color(hitboxColor.r, hitboxColor.g, hitboxColor.b, aLine);

        if (_capsule != null)
            DrawCapsuleLocal(_capsule.size, _capsule.direction, fill, line);
        else if (_box != null)
            DrawBoxLocal(_box.size, fill, line);

        Handles.matrix = old;

        // Label at the collider center in world space
        Handles.Label(worldCenter, active ? label : $"{label} (inactive)");
    }

    private static void DrawBoxLocal(Vector2 size, Color fill, Color outline)
    {
        Vector2 half = size * 0.5f;

        Vector3[] verts =
        {
            new Vector3(-half.x, -half.y, 0f),
            new Vector3(-half.x,  half.y, 0f),
            new Vector3( half.x,  half.y, 0f),
            new Vector3( half.x, -half.y, 0f),
        };

        Handles.DrawSolidRectangleWithOutline(verts, fill, outline);
    }

    private static void DrawCapsuleLocal(Vector2 size, CapsuleDirection2D direction, Color fill, Color outline)
    {
        // Capsule = center rectangle + two end discs
        bool vertical = direction == CapsuleDirection2D.Vertical;

        float width = size.x;
        float height = size.y;

        // Radius is half of the minor axis
        float radius = vertical ? (width * 0.5f) : (height * 0.5f);

        // Length of the straight section
        float rectLength = vertical
            ? Mathf.Max(0f, height - 2f * radius)
            : Mathf.Max(0f, width - 2f * radius);

        // Middle rectangle
        if (rectLength > 0.0001f)
        {
            Vector2 rectSize = vertical ? new Vector2(width, rectLength) : new Vector2(rectLength, height);
            Vector2 half = rectSize * 0.5f;

            Vector3[] verts =
            {
                new Vector3(-half.x, -half.y, 0f),
                new Vector3(-half.x,  half.y, 0f),
                new Vector3( half.x,  half.y, 0f),
                new Vector3( half.x, -half.y, 0f),
            };

            Handles.DrawSolidRectangleWithOutline(verts, fill, outline);
        }

        // End disc centers
        Vector3 a, b;
        if (vertical)
        {
            float y = rectLength * 0.5f;
            a = new Vector3(0f, y, 0f);
            b = new Vector3(0f, -y, 0f);
        }
        else
        {
            float x = rectLength * 0.5f;
            a = new Vector3(x, 0f, 0f);
            b = new Vector3(-x, 0f, 0f);
        }

        // Filled end discs
        Handles.color = fill;
        Handles.DrawSolidDisc(a, Vector3.forward, radius);
        Handles.DrawSolidDisc(b, Vector3.forward, radius);

        // Outline end discs
        Handles.color = outline;
        Handles.DrawWireDisc(a, Vector3.forward, radius);
        Handles.DrawWireDisc(b, Vector3.forward, radius);

        // Connectors (cleaner outline silhouette)
        Handles.color = outline;
        if (vertical)
        {
            float x = radius;
            float y = rectLength * 0.5f;
            Handles.DrawLine(new Vector3(-x, -y, 0f), new Vector3(-x, y, 0f));
            Handles.DrawLine(new Vector3(x, -y, 0f), new Vector3(x, y, 0f));
        }
        else
        {
            float y = radius;
            float x = rectLength * 0.5f;
            Handles.DrawLine(new Vector3(-x, -y, 0f), new Vector3(x, -y, 0f));
            Handles.DrawLine(new Vector3(-x, y, 0f), new Vector3(x, y, 0f));
        }

        // Editor hygiene
        Handles.color = Color.white;
    }

    [ContextMenu("Hitbox Viz/Stop ALL Visualizations")]
    private void StopAll() => StopAllVisualizations = true;

    [ContextMenu("Hitbox Viz/Resume ALL Visualizations")]
    private void ResumeAll() => StopAllVisualizations = false;
#endif
}
