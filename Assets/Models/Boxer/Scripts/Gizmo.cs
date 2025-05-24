using UnityEngine;

[ExecuteAlways] // работает и в редакторе, и во время игры
public class ColliderGizmoDrawer : MonoBehaviour
{
    public Color gizmoColor = Color.green;
    public Vector3 boxSize = new Vector3(0.2f, 0.2f, 0.2f);

    void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }
}
