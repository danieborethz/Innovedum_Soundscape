using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(SceneManager))]
public class SceneManagerEditor : Editor
{
    private SerializedProperty windIntensityProp;
    private SerializedProperty distanceScaleProp;
    // NEW: add property for ambisonic toggle
    private SerializedProperty enableAmbisonicProp;

    // NEW: property for occlusion threshold
    private SerializedProperty occlusionDiameterThresholdProp;

    // NEW: references to the global reverb parameters
    private SerializedProperty globalRoomSizeProp;
    private SerializedProperty globalDecayTimeProp;
    private SerializedProperty globalWetDryMixProp;
    private SerializedProperty globalEqProp;

    private SerializedProperty selectedAmbisonicIndexProp;
    private SerializedProperty ambientSoundFileProp;
    private SerializedProperty windMaterialProp;
    private SceneManager sceneManager;
    private string[] ambisonicAudioNames;

    private void OnEnable()
    {
        sceneManager = (SceneManager)target;

        // Find properties you want to show
        windIntensityProp = serializedObject.FindProperty("windIntensity");
        windMaterialProp = serializedObject.FindProperty("windMaterial");
        distanceScaleProp = serializedObject.FindProperty("distanceScale");
        occlusionDiameterThresholdProp = serializedObject.FindProperty("occlusionDiameterThreshold");

        // NEW: find ambisonic audio toggle property
        enableAmbisonicProp = serializedObject.FindProperty("enableAmbisonic");

        // NEW: find global reverb properties
        globalRoomSizeProp = serializedObject.FindProperty("globalRoomSize");
        globalDecayTimeProp = serializedObject.FindProperty("globalDecayTime");
        globalWetDryMixProp = serializedObject.FindProperty("globalWetDryMix");
        globalEqProp = serializedObject.FindProperty("globalEq");

        selectedAmbisonicIndexProp = serializedObject.FindProperty("selectedAmbisonicIndex");
        ambientSoundFileProp = serializedObject.FindProperty("ambientSoundFile");

        // Load the audio library and update the dropdown names.
        sceneManager.LoadAudioLibrary();
        UpdateAmbisonicAudioNames();

        // Ensure the selected index is valid.
        if (sceneManager.ambisonicAudioItems != null && sceneManager.ambisonicAudioItems.Count > 0)
        {
            if (selectedAmbisonicIndexProp.intValue < 0 ||
                selectedAmbisonicIndexProp.intValue >= sceneManager.ambisonicAudioItems.Count)
            {
                selectedAmbisonicIndexProp.intValue = 0;
            }
            // Update the underlying ambientSoundFile string based on the current selection.
            ambientSoundFileProp.stringValue = sceneManager.ambisonicAudioItems[selectedAmbisonicIndexProp.intValue].audioFilePath;
        }
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(windIntensityProp);
        EditorGUILayout.PropertyField(windMaterialProp, new GUIContent("Wind Material"));
        EditorGUILayout.PropertyField(distanceScaleProp);
        EditorGUILayout.PropertyField(occlusionDiameterThresholdProp, new GUIContent("Occlusion Diameter Threshold"));

        // Show the Global Reverb Parameters
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Global Reverb Parameters", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(globalRoomSizeProp);
        EditorGUILayout.PropertyField(globalDecayTimeProp);
        EditorGUILayout.PropertyField(globalWetDryMixProp);
        EditorGUILayout.PropertyField(globalEqProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ambisonic Audio Settings", EditorStyles.boldLabel);
        // NEW: Draw toggle for ambisonic audio
        EditorGUILayout.PropertyField(enableAmbisonicProp, new GUIContent("Enable Ambisonic Audio"));

        if (enableAmbisonicProp.boolValue)
        {
            // Draw the ambisonic dropdown if available
            if (sceneManager.ambisonicAudioItems != null && sceneManager.ambisonicAudioItems.Count > 0)
            {
                int currentIndex = selectedAmbisonicIndexProp.intValue;
                int newIndex = EditorGUILayout.Popup("Ambisonic File", currentIndex, ambisonicAudioNames);

                if (newIndex != currentIndex && newIndex >= 0 && newIndex < ambisonicAudioNames.Length)
                {
                    selectedAmbisonicIndexProp.intValue = newIndex;
                    ambientSoundFileProp.stringValue = sceneManager.ambisonicAudioItems[newIndex].audioFilePath;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No ambisonic category or audio files found. Make sure your JSON file contains a 'ambisonics' category.", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Ambisonic audio is disabled.", MessageType.Info);
        }


        serializedObject.ApplyModifiedProperties();
    }

    private void UpdateAmbisonicAudioNames()
    {
        if (sceneManager.ambisonicAudioItems != null && sceneManager.ambisonicAudioItems.Count > 0)
        {
            ambisonicAudioNames = sceneManager.ambisonicAudioItems.Select(a => a.displayName).ToArray();
        }
        else
        {
            ambisonicAudioNames = new string[0];
        }
    }
}
