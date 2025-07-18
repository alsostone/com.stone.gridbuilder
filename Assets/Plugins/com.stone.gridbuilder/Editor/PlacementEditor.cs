using System.Text;
using UnityEngine;
using UnityEditor;

namespace ST.GridBuilder
{
    [CustomEditor(typeof(Placement))]
    public class PlacementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Placement placement = target as Placement;
            if (placement == null || placement.placementData == null)
            {
                EditorGUILayout.HelpBox("Placement or PlacementData is not set.", MessageType.Error);
                return;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Placement Type", GUILayout.Width(EditorGUIUtility.labelWidth));
            placement.placementData.placementType =
                (PlacedLayer)EditorGUILayout.EnumFlagsField(placement.placementData.placementType);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Placed Layer", GUILayout.Width(EditorGUIUtility.labelWidth));
            placement.placementData.placedLayer =
                (PlacedLayer)EditorGUILayout.EnumFlagsField(placement.placementData.placedLayer);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Id", GUILayout.Width(EditorGUIUtility.labelWidth));
            GUILayout.Label(placement.placementData.id.ToString());
            GUILayout.EndHorizontal();

            GUILayout.Label($"Shape", EditorStyles.boldLabel);
            for (int z = PlacementData.height - 1; z >= 0; z--)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < PlacementData.width; x++)
                {
                    int index = x + z * PlacementData.width;
                    placement.placementData.points[index] =
                        EditorGUILayout.Toggle(placement.placementData.points[index], GUILayout.MaxWidth(20));
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rotate -180°"))
                placement.placementData.Rotation(-2);
            if (GUILayout.Button("Rotate -90°"))
                placement.placementData.Rotation(-1);
            if (GUILayout.Button("Rotate 90°"))
                placement.placementData.Rotation(1);
            if (GUILayout.Button("Rotate 180°"))
                placement.placementData.Rotation(2);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Copy Shpae to Clipboard"))
            {
                bool[] points = placement.placementData.points;
                StringBuilder builder = new StringBuilder();
                builder.Append(points[0] ? '1' : '0');
                for (int index = 1; index < points.Length; index++)
                {
                    bool point = points[index];
                    builder.Append(point ? ",1" : ",0");
                }
                GUIUtility.systemCopyBuffer = builder.ToString();
            }
            {
                GUI.changed = true;
            }
            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

    }
}