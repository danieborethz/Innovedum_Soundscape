using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(SoundSourceGenerator))]
[CanEditMultipleObjects]
public class SoundSourceGeneratorEditor : Editor
{
    private SerializedProperty enableGenerator;
    private SerializedProperty selectedGeneratorTypeIndex;
    private SerializedProperty selectedFoliageTypeIndex;
    private SerializedProperty selectedWaterTypeIndex;
    private SerializedProperty leavesTreeSize;
    private SerializedProperty size;
    private SerializedProperty sourceSelectionProp;
    private SerializedProperty splashingTime;
    private SerializedProperty splashingBreak;

    // 0 => Water, 1 => Wind
    private string[] generatorTypes = { "Water", "Wind" };

    // Water = channels 1..4 => internal [0..3]
    private string[] waterChannelOptions = {
        "Stereo channel 1",
        "Stereo channel 2",
        "Stereo channel 3",
        "Stereo channel 4"
    };

    // Wind = channels 5..8 => internal [4..7]
    private string[] windChannelOptions = {
        "Stereo channel 5",
        "Stereo channel 6",
        "Stereo channel 7",
        "Stereo channel 8"
    };

    private SoundSourceGenerator[] targetsList;

    private void OnEnable()
    {
        enableGenerator = serializedObject.FindProperty("enableGenerator");
        selectedGeneratorTypeIndex = serializedObject.FindProperty("selectedGeneratorTypeIndex");
        selectedFoliageTypeIndex = serializedObject.FindProperty("selectedFoliageTypeIndex");
        selectedWaterTypeIndex = serializedObject.FindProperty("selectedWaterTypeIndex");
        leavesTreeSize = serializedObject.FindProperty("leavesTreeSize");
        size = serializedObject.FindProperty("size");
        sourceSelectionProp = serializedObject.FindProperty("sourceSelectionIndex");
        splashingTime = serializedObject.FindProperty("splashingTime");
        splashingBreak = serializedObject.FindProperty("splashingBreak");

        targetsList = targets.Cast<SoundSourceGenerator>().ToArray();

        // Make sure each object is assigned in SourceSelectionManager
        foreach (var t in targetsList)
        {
            if (!SourceSelectionManager.IsSourceTaken("stereo", t.SourceSelectionIndex))
            {
                SourceSelectionManager.AssignSource("stereo", t.SourceSelectionIndex, t);
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Generator Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableGenerator, new GUIContent("Enable Generator"));

        // Draw the generator type dropdown
        DrawGeneratorTypeDropdown();
        GUILayout.Space(10);

        // If multiple objects have different types, just show info
        if (selectedGeneratorTypeIndex.hasMultipleDifferentValues)
        {
            EditorGUILayout.HelpBox("Generator type varies across selected objects.", MessageType.Info);
        }
        else
        {
            // 0 => Water, 1 => Wind
            if (selectedGeneratorTypeIndex.intValue == 0)
            {
                // Water
                DrawWaterTypeDropdown();
                DrawSizeSlider();
                DrawChannelSelection(isWater: true);

                // If "Splashing Fountain" (2), show splashing fields
                if (!selectedWaterTypeIndex.hasMultipleDifferentValues && selectedWaterTypeIndex.intValue == 2)
                {
                    EditorGUI.showMixedValue = splashingTime.hasMultipleDifferentValues;
                    float newSplashTime = EditorGUILayout.Slider("Splashing Time", splashingTime.floatValue, 0.0f, 10.0f);
                    if (!Mathf.Approximately(newSplashTime, splashingTime.floatValue))
                    {
                        splashingTime.floatValue = newSplashTime;
                    }
                    EditorGUI.showMixedValue = false;

                    EditorGUI.showMixedValue = splashingBreak.hasMultipleDifferentValues;
                    float newSplashBreak = EditorGUILayout.Slider("Splashing Break", splashingBreak.floatValue, 0.0f, 10.0f);
                    if (!Mathf.Approximately(newSplashBreak, splashingBreak.floatValue))
                    {
                        splashingBreak.floatValue = newSplashBreak;
                    }
                    EditorGUI.showMixedValue = false;
                }
            }
            else
            {
                // Wind
                DrawFoliageTypeDropdown();
                DrawLeavesTreeSizeSlider();
                DrawChannelSelection(isWater: false);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGeneratorTypeDropdown()
    {
        EditorGUI.showMixedValue = selectedGeneratorTypeIndex.hasMultipleDifferentValues;
        int oldType = selectedGeneratorTypeIndex.intValue;
        int newType = EditorGUILayout.Popup("Generator Type", oldType, generatorTypes);
        EditorGUI.showMixedValue = false;

        if (newType != oldType)
        {
            selectedGeneratorTypeIndex.intValue = newType;
            serializedObject.ApplyModifiedProperties(); // apply the new type

            // SHIFT the channel for each object
            foreach (var t in targetsList)
            {
                // Unassign old
                if (SourceSelectionManager.IsSourceTaken("stereo", t.SourceSelectionIndex))
                {
                    var assignedObj = SourceSelectionManager.GetAssignedObject("stereo", t.SourceSelectionIndex);
                    if (assignedObj == t)
                        SourceSelectionManager.UnassignSource("stereo", t.SourceSelectionIndex);
                }

                // Water->Wind => +4, Wind->Water => -4
                if (oldType == 0 && newType == 1) // water->wind
                {
                    t.SourceSelectionIndex += 4;
                }
                else if (oldType == 1 && newType == 0) // wind->water
                {
                    t.SourceSelectionIndex -= 4;
                }

                // Reassign new channel
                SourceSelectionManager.AssignSource("stereo", t.SourceSelectionIndex, t);

                // -- FORCE an immediate OSC update --
                // (OnValidate may not be called reliably at this moment)
                t.ForceSendMessages();

                EditorUtility.SetDirty(t);
            }
        }
    }

    private void DrawWaterTypeDropdown()
    {
        EditorGUI.showMixedValue = selectedWaterTypeIndex.hasMultipleDifferentValues;
        int newVal = EditorGUILayout.Popup("Water Type", selectedWaterTypeIndex.intValue,
            new string[] { "Flow", "Drinking Fountain", "Splashing Fountain" });
        if (newVal != selectedWaterTypeIndex.intValue)
        {
            selectedWaterTypeIndex.intValue = newVal;
        }
        EditorGUI.showMixedValue = false;
    }

    private void DrawFoliageTypeDropdown()
    {
        EditorGUI.showMixedValue = selectedFoliageTypeIndex.hasMultipleDifferentValues;
        int newVal = EditorGUILayout.Popup("Foliage Type", selectedFoliageTypeIndex.intValue,
            new string[] { "Needles", "Leaves" });
        if (newVal != selectedFoliageTypeIndex.intValue)
        {
            selectedFoliageTypeIndex.intValue = newVal;
        }
        EditorGUI.showMixedValue = false;
    }

    private void DrawLeavesTreeSizeSlider()
    {
        EditorGUI.showMixedValue = leavesTreeSize.hasMultipleDifferentValues;
        float newVal = EditorGUILayout.Slider("Leaves/Tree Size", leavesTreeSize.floatValue, 0.1f, 10.0f);
        if (!Mathf.Approximately(newVal, leavesTreeSize.floatValue))
        {
            leavesTreeSize.floatValue = newVal;
        }
        EditorGUI.showMixedValue = false;
    }

    private void DrawSizeSlider()
    {
        EditorGUI.showMixedValue = size.hasMultipleDifferentValues;
        float newVal = EditorGUILayout.Slider("Size", size.floatValue, 0.1f, 10.0f);
        if (!Mathf.Approximately(newVal, size.floatValue))
        {
            size.floatValue = newVal;
        }
        EditorGUI.showMixedValue = false;
    }

    /// <summary>
    /// Draws the channel selection for Water (0..3 -> "1..4") or Wind (4..7 -> "5..8").
    /// </summary>
    private void DrawChannelSelection(bool isWater)
    {
        // Gather all SourceSelectionIndex from selected objects
        int[] selections = targetsList.Select(t => t.SourceSelectionIndex).ToArray();
        bool multipleValues = (selections.Distinct().Count() > 1);
        EditorGUI.showMixedValue = multipleValues;

        // Water => waterChannelOptions, Wind => windChannelOptions
        string[] options = isWater ? waterChannelOptions : windChannelOptions;

        int currentSelection = selections[0];

        // Convert the internal index to the display index
        int displayIndex;
        if (isWater)
        {
            // Water => internal 0..3 => displayed as 1..4
            displayIndex = currentSelection;
            if (displayIndex < 0) displayIndex = 0;
            if (displayIndex > 3) displayIndex = 3;
        }
        else
        {
            // Wind => internal 4..7 => displayed as 5..8
            displayIndex = currentSelection - 4;
            if (displayIndex < 0) displayIndex = 0;
            if (displayIndex > 3) displayIndex = 3;
        }

        int newDisplayIndex = EditorGUILayout.Popup("Channel", displayIndex, options);
        EditorGUI.showMixedValue = false;

        if (newDisplayIndex == displayIndex) return; // no change

        // Convert the display index back to the actual internal index
        int newSelection = isWater ? newDisplayIndex : (newDisplayIndex + 4);

        // Apply to all selected objects
        foreach (var t in targetsList)
        {
            int oldSel = t.SourceSelectionIndex;

            // Unassign old
            if (SourceSelectionManager.IsSourceTaken("stereo", oldSel))
            {
                var assignedObj = SourceSelectionManager.GetAssignedObject("stereo", oldSel);
                if (assignedObj == t)
                    SourceSelectionManager.UnassignSource("stereo", oldSel);
            }

            // Set the new selection
            t.SourceSelectionIndex = newSelection;
            SourceSelectionManager.AssignSource("stereo", newSelection, t);

            // FORCE an immediate OSC update
            t.ForceSendMessages();

            EditorUtility.SetDirty(t);
        }
    }
}
