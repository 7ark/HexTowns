using UnityEngine;

#if UNITY_EDITOR
namespace CodeRoadOne
{
    public static class CRO_DebugBoxCollider
    {
        public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color)
        {
            Vector3 localFrontTopLeft = RotatePointAroundPivot(new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z), Vector3.zero, orientation);
            Vector3 localFrontTopRight = RotatePointAroundPivot(new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z), Vector3.zero, orientation);
            Vector3 localFrontBottomLeft = RotatePointAroundPivot(new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z), Vector3.zero, orientation);
            Vector3 localFrontBottomRight = RotatePointAroundPivot(new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z), Vector3.zero, orientation);
            Vector3 localBackTopLeft = -localFrontBottomRight;
            Vector3 localBackTopRight = -localFrontBottomLeft;
            Vector3 localBackBottomLeft = -localFrontTopRight;
            Vector3 localBackBottomRight = -localFrontTopLeft;

            Vector3 frontTopLeft = localFrontTopLeft + origin;
            Vector3 frontTopRight = localFrontTopRight + origin;
            Vector3 frontBottomLeft = localFrontBottomLeft + origin;
            Vector3 frontBottomRight = localFrontBottomRight + origin;
            Vector3 backTopLeft = localBackTopLeft + origin;
            Vector3 backTopRight = localBackTopRight + origin;
            Vector3 backBottomLeft = localBackBottomLeft + origin;
            Vector3 backBottomRight = localBackBottomRight + origin;

            Debug.DrawLine(frontTopLeft, frontTopRight, color);
            Debug.DrawLine(frontTopRight, frontBottomRight, color);
            Debug.DrawLine(frontBottomRight, frontBottomLeft, color);
            Debug.DrawLine(frontBottomLeft, frontTopLeft, color);

            Debug.DrawLine(backTopLeft, backTopRight, color);
            Debug.DrawLine(backTopRight, backBottomRight, color);
            Debug.DrawLine(backBottomRight, backBottomLeft, color);
            Debug.DrawLine(backBottomLeft, backTopLeft, color);

            Debug.DrawLine(frontTopLeft, backTopLeft, color);
            Debug.DrawLine(frontTopRight, backTopRight, color);
            Debug.DrawLine(frontBottomRight, backBottomRight, color);
            Debug.DrawLine(frontBottomLeft, backBottomLeft, color);
        }

        static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            Vector3 direction = point - pivot;
            return pivot + rotation * direction;
        }
    }
}
#endif //#if UNITY_EDITOR
