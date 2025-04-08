using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SourceSelectionManager
{
    // The dictionary now stores any SoundSource object regardless of specific subclass
    private static Dictionary<string, Dictionary<int, SoundSource>> assignedMap
        = new Dictionary<string, Dictionary<int, SoundSource>>()
    {
        { "mono", new Dictionary<int, SoundSource>() },
        { "stereo", new Dictionary<int, SoundSource>() },
        { "multi", new Dictionary<int, SoundSource>() }
    };

    // Static constructor for subscribing to scene events
    static SourceSelectionManager()
    {
        // Subscribe to the sceneOpened event so that OnSceneLoaded is called whenever a scene is loaded in the editor.
        EditorSceneManager.sceneOpened += OnSceneLoaded;
    }

    // This method is called every time a scene is loaded/reloaded in the editor.
    private static void OnSceneLoaded(Scene scene, OpenSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name} with mode: {mode}");
        UpdateSources();
    }

    // This method is called on project startup in the editor.
    [InitializeOnLoadMethod]
    private static void OnProjectStartup()
    {
        Debug.Log("Project startup: Unity Editor loaded or assemblies reloaded.");
        UpdateSources();
    }

    // Updated function to get all SoundSource components in loaded scenes, read their source data, and save it to the map.
    public static void UpdateSources()
    {
        // Clear all the entries in the dictionary.
        foreach (var key in assignedMap.Keys)
        {
            assignedMap[key].Clear();
        }

        // Find all SoundSource objects including those that might be disabled.
        // Note: Using Resources.FindObjectsOfTypeAll ensures we get all instances, not just those active in the hierarchy.
        SoundSource[] sources = Resources.FindObjectsOfTypeAll<SoundSource>();

        // Cache the reflection object for SourceType (which is used for reading only).
        PropertyInfo typeProperty = typeof(SoundSource).GetProperty("SourceType", BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (var source in sources)
        {
            // Filter out prefabs and assets: only consider objects from a loaded scene.
            if (!source.gameObject.scene.isLoaded)
                continue;

            // Retrieve the source type using reflection.
            string sourceType = typeProperty?.GetValue(source) as string;
            if (sourceType == null)
                continue;

            // Get the current selection value.
            // Since SourceSelection is read-only, we instead use the public field on SoundSourceAudio if available.
            int sourceSelection = -1;
            SoundSourceAudio audioSource = source as SoundSourceAudio;
            if (audioSource != null)
            {
                sourceSelection = audioSource.sourceSelection;
            }
            else
            {
                // If not SoundSourceAudio, you may need another method of obtaining the selection.
                // For now, skip non-SoundSourceAudio instances.
                continue;
            }

            // Ensure our dictionary contains the source type.
            if (!assignedMap.ContainsKey(sourceType))
            {
                assignedMap[sourceType] = new Dictionary<int, SoundSource>();
            }

            // Check if the current selection is already taken.
            if (assignedMap[sourceType].ContainsKey(sourceSelection))
            {
                // If it's a mono source, try to find an available mono channel.
                if (sourceType == "mono")
                {
                    bool assigned = false;
                    // Use the monoSources field to determine the maximum number of channels.
                    for (int i = 0; i < audioSource.monoSources; i++)
                    {
                        if (!assignedMap[sourceType].ContainsKey(i))
                        {
                            // Update the underlying field directly.
                            audioSource.sourceSelection = i;
                            assignedMap[sourceType][i] = source;
                            Debug.Log($"Mono duplicate fixed on GameObject '{source.gameObject.name}': Changed from {sourceSelection} to {i}");
                            assigned = true;
                            break;
                        }
                    }
                    if (!assigned)
                    {
                        Debug.LogWarning($"No available mono channel for GameObject '{source.gameObject.name}'. It remains on channel {sourceSelection}.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Duplicate SoundSource registration for type {sourceType} and selection {sourceSelection} on GameObject '{source.gameObject.name}'.");
                }
            }
            else
            {
                // If the selection is free, assign it.
                assignedMap[sourceType][sourceSelection] = source;
                Debug.Log($"Registered SoundSource on GameObject '{source.gameObject.name}': Type = {sourceType}, Selection = {sourceSelection}");
            }
        }
        Debug.Log("SourceSelectionManager: Sources have been updated.");
    }



    public static bool IsSourceTaken(string type, int index)
    {
        return assignedMap.ContainsKey(type) && assignedMap[type].ContainsKey(index);
    }

    public static SoundSource GetAssignedObject(string type, int index)
    {
        if (IsSourceTaken(type, index))
        {
            return assignedMap[type][index];
        }
        return null;
    }

    public static void AssignSource(string type, int index, SoundSource obj)
    {
        if (!assignedMap.ContainsKey(type))
        {
            assignedMap[type] = new Dictionary<int, SoundSource>();
        }

        assignedMap[type][index] = obj;
    }

    public static void UnassignSource(string type, int index)
    {
        if (assignedMap.ContainsKey(type) && assignedMap[type].ContainsKey(index))
        {
            assignedMap[type].Remove(index);
        }
    }
}
