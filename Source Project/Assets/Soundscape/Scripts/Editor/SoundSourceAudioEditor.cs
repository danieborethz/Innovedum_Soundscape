using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;

[CustomEditor(typeof(SoundSourceAudio), true)]
[CanEditMultipleObjects] // Enables multi-object editing
public class SoundSourceAudioEditor : Editor
{
    private SerializedProperty selectedCategoryIndex;
    private SerializedProperty selectedAudioIndex;
    private SerializedProperty sourceTypeSelection;
    private SerializedProperty sourceSelection;
    private SerializedProperty multiSize;

    private string[] categoryNames;
    private string[] audioNames;
    private string[] sourceOptions;

    private List<SoundSourceAudio> targetsList;


    private void OnEnable()
    {
        selectedCategoryIndex = serializedObject.FindProperty("selectedCategoryIndex");
        selectedAudioIndex = serializedObject.FindProperty("selectedAudioIndex");
        sourceTypeSelection = serializedObject.FindProperty("sourceTypeSelection");
        sourceSelection = serializedObject.FindProperty("sourceSelection");
        multiSize = serializedObject.FindProperty("multiSize");

        targetsList = targets.Cast<SoundSourceAudio>().ToList();

        UpdateCategoryNames();
        UpdateAudioNames();
        UpdateSourceOptions();
        
        EnsureSourceIsNotTaken(); // 2) If source is taken -> next free or -1

    }

    private void EnsureSourceIsNotTaken()
    {
        // 2) Ensure that the assigned source is free or reassign
        var so = (SoundSourceAudio)target;
        if (so == null) return;
        if (so._initialized) return;

        string st = so.sourceTypes[so.sourceTypeSelection];
        int idx = so.sourceSelection;

        SourceSelectionManager.UpdateSources();

        // If the user just created this component, or if re-Reset is called, we must ensure we aren't stepping on another object
        if (idx >= 0 && SourceSelectionManager.IsSourceTaken(st, idx))
        {
            // Source is taken, find next free
            int freeIndex = FindNextFreeIndex(so);
            if (freeIndex == -1)
            {
                // No free index
                Debug.LogWarning($"No free {st} indices available for {so.name}, setting to -1.");
                so.sourceSelection = -1;
            }
            else
            {
                so.sourceSelection = freeIndex;
                SourceSelectionManager.AssignSource(st, freeIndex, so);
            }
        }
        else if (idx >= 0)
        {
            // It's free, so assign it
            SourceSelectionManager.AssignSource(st, idx, so);
        }
        else
        {
            // If idx is already -1, do nothing (meaning no assignment).
            // This situation might happen if user had set -1 manually.
        }

        // Save changes to the object
        EditorUtility.SetDirty(so);

        so._initialized = true;
    }

    /// <summary>
    /// Finds the first free index for the given SoundSourceAudio object, based on its sourceTypeSelection.
    /// Returns -1 if none is free.
    /// </summary>
    private int FindNextFreeIndex(SoundSourceAudio so)
    {
        string st = so.sourceTypes[so.sourceTypeSelection];
        int sourceCount = 0;

        // Determine how many sources exist of this type
        switch (so.sourceTypeSelection)
        {
            case 0: sourceCount = so.monoSources; break;
            case 1: sourceCount = so.stereoSources; break;
            case 2: sourceCount = so.multiSources; break;
        }

        for (int i = 0; i < sourceCount; i++)
        {
            if (!SourceSelectionManager.IsSourceTaken(st, i))
            {
                return i;
            }
        }
        return -1;
    }

    // Rest of your editor code remains unchanged below (except for small housekeeping):
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (targetsList.All(t => t.audioLibrary != null))
        {
            DrawCategoryDropdown();
            DrawAudioDropdown();
            DrawSourceTypeDropdown();
            DrawSourceDropdown();

            // Only draw the MultiSize slider if the multi-source type is selected
            if (targetsList.All(t => t.sourceTypeSelection == 2)) // Assuming 2 corresponds to multi-source
            {
                DrawMultiSizeSlider();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("One or more selected objects have no audio library loaded.", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCategoryDropdown()
    {
        int commonCategoryIndex = selectedCategoryIndex.intValue;
        if (targetsList.Select(t => t.selectedCategoryIndex).Distinct().Count() > 1)
            commonCategoryIndex = -1;

        int newCategoryIndex = EditorGUILayout.Popup("Category", commonCategoryIndex, categoryNames);
        if (newCategoryIndex != commonCategoryIndex && newCategoryIndex >= 0)
        {
            foreach (var target in targetsList)
            {
                target.selectedCategoryIndex = newCategoryIndex;
                target.selectedCategoryName = categoryNames[newCategoryIndex];

                // Update currentAudioItems based on the newly selected category
                if (target.categories != null && target.categories.Count > newCategoryIndex)
                {
                    target.currentAudioItems = target.categories[newCategoryIndex].audioItems;
                }

                EditorUtility.SetDirty(target);
            }

            // Now that currentAudioItems have been updated, refresh the audioNames array
            UpdateAudioNames();
        }
    }


    private void DrawAudioDropdown()
    {
        int commonAudioIndex = selectedAudioIndex.intValue;
        if (targetsList.Select(t => t.selectedAudioIndex).Distinct().Count() > 1)
            commonAudioIndex = -1;

        int newAudioIndex = EditorGUILayout.Popup("Audio", commonAudioIndex, audioNames);
        if (newAudioIndex != commonAudioIndex && newAudioIndex >= 0)
        {
            foreach (var target in targetsList)
            {
                target.selectedAudioIndex = newAudioIndex;
                target.selectedAudioName = audioNames[newAudioIndex];
                EditorUtility.SetDirty(target);
            }
        }
    }

    private void DrawSourceTypeDropdown()
    {
        int commonSourceType = sourceTypeSelection.intValue;
        if (targetsList.Select(t => t.sourceTypeSelection).Distinct().Count() > 1)
            commonSourceType = -1;

        int newSourceType = EditorGUILayout.Popup("Source Type", commonSourceType, targetsList[0].sourceTypes);
        if (newSourceType != commonSourceType && newSourceType >= 0)
        {
            // Before changing the type, unassign current sources
            foreach (var t in targetsList)
            {
                string oldType = t.sourceTypes[t.sourceTypeSelection];
                if (t.sourceSelection >= 0)
                    SourceSelectionManager.UnassignSource(oldType, t.sourceSelection);
            }

            foreach (var target in targetsList)
            {
                target.sourceTypeSelection = newSourceType;
                // Reset source selection when type changes (to 0 for example)
                target.sourceSelection = 0;
                EditorUtility.SetDirty(target);

                // Assign the new default source
                string newTypeName = target.sourceTypes[newSourceType];
                if (!SourceSelectionManager.IsSourceTaken(newTypeName, target.sourceSelection))
                {
                    SourceSelectionManager.AssignSource(newTypeName, target.sourceSelection, target);
                }
                else
                {
                    // If the default is taken, try to find the next free:
                    int freeIndex = FindNextFreeIndex(target);
                    if (freeIndex == -1)
                    {
                        Debug.LogWarning($"No free {newTypeName} index available for {target.name}; setting to -1.");
                        target.sourceSelection = -1;
                    }
                    else
                    {
                        target.sourceSelection = freeIndex;
                        SourceSelectionManager.AssignSource(newTypeName, freeIndex, target);
                    }
                    EditorUtility.SetDirty(target);
                }
            }

            UpdateSourceOptions();
        }
    }

    private void DrawSourceDropdown()
    {
        // Determine a common sourceSelection; if they differ, we default to -1 (no common selection)
        int commonSourceSelection = sourceSelection.intValue;
        if (targetsList.Select(t => t.sourceSelection).Distinct().Count() > 1)
            commonSourceSelection = -1;

        // Adjust the display index for the dropdown: index 0 corresponds to "no source selected"
        int displayIndex = (commonSourceSelection >= 0) ? commonSourceSelection + 1 : 0;

        int newIndexInArray = EditorGUILayout.Popup("Source", displayIndex, sourceOptions);

        // When the user changes the selection:
        if (newIndexInArray != displayIndex)
        {
            // Map dropdown selection to the actual source index (-1 if the empty option is selected)
            int newSourceSelection = (newIndexInArray == 0) ? -1 : newIndexInArray - 1;

            foreach (var target in targetsList)
            {
                string currentType = target.sourceTypes[target.sourceTypeSelection];
                int oldIndex = target.sourceSelection;
                if (oldIndex >= 0)
                    SourceSelectionManager.UnassignSource(currentType, oldIndex);

                // If the user chooses a valid source index, check if it is already taken
                if (newSourceSelection >= 0 && SourceSelectionManager.IsSourceTaken(currentType, newSourceSelection))
                {
                    // Swap logic as before (or you can modify it as needed)
                    SoundSource assignedSource = SourceSelectionManager.GetAssignedObject(currentType, newSourceSelection);
                    SoundSourceAudio otherObject = assignedSource as SoundSourceAudio;

                    if (otherObject != null && otherObject != target)
                    {
                        int otherOldIndex = otherObject.sourceSelection;
                        SourceSelectionManager.UnassignSource(currentType, newSourceSelection);

                        target.sourceSelection = newSourceSelection;
                        SourceSelectionManager.AssignSource(currentType, newSourceSelection, target);

                        otherObject.sourceSelection = oldIndex;
                        if (oldIndex >= 0)
                            SourceSelectionManager.AssignSource(currentType, oldIndex, otherObject);

                        EditorUtility.SetDirty(otherObject);
                        EditorUtility.SetDirty(target);
                    }
                    else
                    {
                        target.sourceSelection = newSourceSelection;
                        SourceSelectionManager.AssignSource(currentType, newSourceSelection, target);
                        EditorUtility.SetDirty(target);
                    }
                }
                else
                {
                    // Simply assign if not taken or if the empty option is chosen
                    target.sourceSelection = newSourceSelection;
                    if (newSourceSelection >= 0)
                        SourceSelectionManager.AssignSource(currentType, newSourceSelection, target);
                    EditorUtility.SetDirty(target);
                }
            }
        }

        // If no free source is available (i.e. commonSourceSelection is -1), display a warning
        if (commonSourceSelection == -1)
        {
            EditorGUILayout.HelpBox("No free source available. Please select a different Source Type or free up an existing source.", MessageType.Warning);
        }
    }


    private void DrawMultiSizeSlider()
    {
        float commonMultiSize = multiSize.floatValue;
        if (targetsList.Select(t => t.multiSize).Distinct().Count() > 1)
            commonMultiSize = -1;

        float newMultiSize = EditorGUILayout.Slider("Multi Size", commonMultiSize, 1.0f, 10.0f);
        if (Math.Abs(newMultiSize - commonMultiSize) > 0.0001f && newMultiSize >= 1.0f)
        {
            foreach (var target in targetsList)
            {
                target.multiSize = newMultiSize;
                EditorUtility.SetDirty(target);
            }
        }
    }

    private void UpdateCategoryNames()
    {
        if (targetsList.All(t => t.categories != null))
        {
            categoryNames = targetsList[0].categories.Select(c => c.categoryName).ToArray();
        }
        else
        {
            categoryNames = new string[0];
        }
    }

    private void UpdateAudioNames()
    {
        if (targetsList.All(t => t.currentAudioItems != null))
        {
            audioNames = targetsList[0].currentAudioItems.Select(a => a.displayName).ToArray();
        }
        else
        {
            audioNames = new string[0];
        }
    }

    private void UpdateSourceOptions()
    {
        int sourceCount = 0;
        string sourceType = targetsList[0].sourceTypes[targetsList[0].sourceTypeSelection];
        switch (targetsList[0].sourceTypeSelection)
        {
            case 0:
                sourceCount = targetsList[0].monoSources;
                break;
            case 1:
                sourceCount = targetsList[0].stereoSources;
                break;
            case 2:
                sourceCount = targetsList[0].multiSources;
                break;
        }

        List<string> availableOptions = new List<string>();
        // Add an empty option as the first entry
        availableOptions.Add("");
        for (int i = 0; i < sourceCount; i++)
        {
            availableOptions.Add($"{sourceType} source {i + 1}");
        }
        sourceOptions = availableOptions.ToArray();
    }

}
