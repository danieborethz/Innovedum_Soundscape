using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ReverbZone))]
public class ReverbZoneEditor : Editor
{
    void OnSceneGUI()
    {
        ReverbZone zone = (ReverbZone)target;
        Transform zoneTransform = zone.transform;

        EditorGUI.BeginChangeCheck();

        // Set a larger handle size for better visibility
        float handleSize = HandleUtility.GetHandleSize(zoneTransform.position) * 1.5f; // Increased from 0.5f to 1.5f

        // Minimum handle size to prevent them from becoming too small when zoomed out
        float minHandleSize = 1.0f;
        handleSize = Mathf.Max(handleSize, minHandleSize);

        // Colors for handles
        Color colorX = Handles.xAxisColor;
        Color colorY = Handles.yAxisColor;
        Color colorZ = Handles.zAxisColor;

        // Draw and adjust radii.x
        Handles.color = colorX;
        Vector3 handlePosX = zoneTransform.position + zoneTransform.right * zone.radii.x;
        float newRadiusX = Handles.ScaleValueHandle(
            zone.radii.x,
            handlePosX,
            zoneTransform.rotation * Quaternion.LookRotation(Vector3.right),
            handleSize,
            Handles.CubeHandleCap, // Changed from ArrowHandleCap to CubeHandleCap for better visibility
            1.0f // Increased handle cap size
        );

        // Draw and adjust radii.y
        Handles.color = colorY;
        Vector3 handlePosY = zoneTransform.position + zoneTransform.up * zone.radii.y;
        float newRadiusY = Handles.ScaleValueHandle(
            zone.radii.y,
            handlePosY,
            zoneTransform.rotation * Quaternion.LookRotation(Vector3.up),
            handleSize,
            Handles.CubeHandleCap,
            1.0f
        );

        // Draw and adjust radii.z
        Handles.color = colorZ;
        Vector3 handlePosZ = zoneTransform.position + zoneTransform.forward * -zone.radii.z;
        float newRadiusZ = Handles.ScaleValueHandle(
            zone.radii.z,
            handlePosZ,
            zoneTransform.rotation * Quaternion.LookRotation(Vector3.forward),
            handleSize,
            Handles.CubeHandleCap,
            1.0f
        );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(zone, "Change Radii");
            zone.radii = new Vector3(
                Mathf.Max(newRadiusX, 0.01f),
                Mathf.Max(newRadiusY, 0.01f),
                Mathf.Max(newRadiusZ, 0.01f)
            );

           // zone.UpdateVisuals();
            EditorUtility.SetDirty(zone);
        }
    }
}
