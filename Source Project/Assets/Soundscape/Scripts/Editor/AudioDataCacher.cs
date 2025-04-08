using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class AudioDataCacher : EditorWindow
{
    public string rootFolderPath = "";
    public string cacheFilePath = "Assets/AudioDataCache.json";

    private int monoSources = 16;
    private int stereoSources = 8;
    private int multiSources = 3;

    [MenuItem("Soundscape/Audio Updater Settings")]
    public static void ShowWindow()
    {
        GetWindow<AudioDataCacher>("Audio Updater Settings");
    }

    private void OnEnable()
    {
        if (File.Exists(cacheFilePath))
        {
            LoadSourcesFromFile();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Audio Data Cacher", EditorStyles.boldLabel);

        // Root Folder Path input
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Root Folder Path", GUILayout.Width(100));
        rootFolderPath = EditorGUILayout.TextField(rootFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string defaultPath = string.IsNullOrEmpty(rootFolderPath) ? Application.dataPath : rootFolderPath;
            string selectedFolder = EditorUtility.OpenFolderPanel("Select Root Folder", defaultPath, "");
            if (!string.IsNullOrEmpty(selectedFolder))
            {
                rootFolderPath = selectedFolder;
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Cache File Path input
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Cache File Path", GUILayout.Width(100));
        cacheFilePath = EditorGUILayout.TextField(cacheFilePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedFile = EditorUtility.SaveFilePanel("Select Cache File", Application.dataPath, "AudioDataCache", "json");
            if (!string.IsNullOrEmpty(selectedFile))
            {
                cacheFilePath = selectedFile;
            }
        }
        if (GUILayout.Button("Load Sources", GUILayout.Width(100)))
        {
            LoadSourcesFromFile();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Source settings input
        monoSources = EditorGUILayout.IntField("Mono Sources", monoSources);
        stereoSources = EditorGUILayout.IntField("Stereo Sources", stereoSources);
        multiSources = EditorGUILayout.IntField("Multi Sources", multiSources);

        GUILayout.Space(10);

        if (GUILayout.Button("Update Audio Library"))
        {
            CacheAudioData();
        }
    }

    private void LoadSourcesFromFile()
    {
        if (File.Exists(cacheFilePath))
        {
            string json = File.ReadAllText(cacheFilePath);
            AudioLibrary loadedLibrary = JsonUtility.FromJson<AudioLibrary>(json);
            if (loadedLibrary != null)
            {
                monoSources = loadedLibrary.monoSources;
                stereoSources = loadedLibrary.stereoSources;
                multiSources = loadedLibrary.ambisonicSources;
                rootFolderPath = loadedLibrary.rootFolderPath;
                Debug.Log("Loaded source settings and root folder path from cache file.");
            }
            else
            {
                Debug.LogError("Failed to parse the cache file.");
            }
        }
        else
        {
            Debug.LogWarning("Cache file does not exist at: " + cacheFilePath);
        }
    }

    public void CacheAudioData()
    {
        AudioLibrary audioLibrary = null;

        if (!string.IsNullOrEmpty(rootFolderPath) && Directory.Exists(rootFolderPath))
        {
            audioLibrary = new AudioLibrary
            {
                monoSources = monoSources,
                stereoSources = stereoSources,
                ambisonicSources = multiSources,
                rootFolderPath = rootFolderPath,
                categories = new List<AudioCategory>()
            };
            ScanFolder(rootFolderPath, audioLibrary.categories, "");
        }
        else
        {
            if (File.Exists(cacheFilePath))
            {
                string json = File.ReadAllText(cacheFilePath);
                audioLibrary = JsonUtility.FromJson<AudioLibrary>(json);
                if (audioLibrary == null)
                {
                    Debug.LogWarning("Failed to parse existing cache file; creating new AudioLibrary.");
                    audioLibrary = new AudioLibrary { categories = new List<AudioCategory>() };
                }
            }
            else
            {
                audioLibrary = new AudioLibrary { categories = new List<AudioCategory>() };
            }

            audioLibrary.monoSources = monoSources;
            audioLibrary.stereoSources = stereoSources;
            audioLibrary.ambisonicSources = multiSources;
            audioLibrary.rootFolderPath = rootFolderPath;
            Debug.LogWarning("No valid root folder set; preserving existing categories and updating only source settings and root folder path.");
        }

        string updatedJson = JsonUtility.ToJson(audioLibrary, true);
        File.WriteAllText(cacheFilePath, updatedJson);
        AssetDatabase.Refresh();
        Debug.Log("Audio data cached successfully at: " + cacheFilePath);

        UpdateAllSoundSourceAudioPrefabs();
        UpdateAllSoundSourceAudioComponents();
    }

    private void UpdateAllSoundSourceAudioComponents()
    {
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                var rootObjects = scene.GetRootGameObjects();
                foreach (var rootObject in rootObjects)
                {
                    var components = rootObject.GetComponentsInChildren<SoundSourceAudio>(true);
                    foreach (var component in components)
                    {
                        component.cacheFilePath = cacheFilePath;
                        component.LoadAudioLibrary();
                        EditorUtility.SetDirty(component);
                        EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
                    }
                }
            }
        }
        EditorSceneManager.SaveOpenScenes();
        InternalEditorUtility.RepaintAllViews();
    }

    private void ScanFolder(string folderPath, List<AudioCategory> categories, string relativePath)
    {
        foreach (var directory in Directory.GetDirectories(folderPath))
        {
            string dirName = Path.GetFileName(directory);
            string newRelativePath = string.IsNullOrEmpty(relativePath) ? dirName : System.IO.Path.Combine(relativePath, dirName);
            AudioCategory category = new AudioCategory
            {
                categoryName = newRelativePath.Replace("\\", "/"),
                audioItems = new List<AudioItem>()
            };

            string parametersFilePath = System.IO.Path.Combine(directory, "Parameters.txt");
            List<Parameter> parameters = null;
            if (File.Exists(parametersFilePath))
            {
                parameters = ParseParametersFile(parametersFilePath);
            }

            var audioFiles = Directory.GetFiles(directory, "*.mp3")
                                      .Concat(Directory.GetFiles(directory, "*.wav"))
                                      .Concat(Directory.GetFiles(directory, "*.aiff"))
                                      .ToArray();
            foreach (var audioFile in audioFiles)
            {
                AudioItem item = new AudioItem
                {
                    displayName = System.IO.Path.GetFileNameWithoutExtension(audioFile),
                    audioFilePath = audioFile,
                    parametersFilePath = File.Exists(parametersFilePath) ? parametersFilePath : null,
                    parameters = parameters ?? new List<Parameter>()
                };
                category.audioItems.Add(item);
            }

            if (category.audioItems.Count > 0)
            {
                categories.Add(category);
            }

            ScanFolder(directory, categories, newRelativePath);
        }
    }

    private List<Parameter> ParseParametersFile(string filePath)
    {
        var parameters = new List<Parameter>();
        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            var keyValue = line.Split(':');
            if (keyValue.Length == 2)
            {
                string key = keyValue[0].Trim();
                string range = keyValue[1].Trim();
                var minMax = range.Split('-');
                if (minMax.Length == 2)
                {
                    if (float.TryParse(minMax[0], out float minValue) && float.TryParse(minMax[1], out float maxValue))
                    {
                        Parameter param = new Parameter
                        {
                            key = key,
                            minValue = minValue,
                            maxValue = maxValue,
                            defaultValue = minValue
                        };
                        parameters.Add(param);
                    }
                    else
                    {
                        Debug.LogError($"Invalid range values for parameter '{key}' in file '{filePath}'.");
                    }
                }
                else
                {
                    Debug.LogError($"Invalid range format for parameter '{key}' in file '{filePath}'. Expected format 'minValue-maxValue'.");
                }
            }
            else
            {
                Debug.LogError($"Invalid parameter line format in file '{filePath}': '{line}'. Expected format 'ParameterName: minValue-maxValue'.");
            }
        }
        return parameters;
    }

    private void UpdateAllSoundSourceAudioPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null)
                continue;

            SoundSourceAudio soundSource = prefabAsset.GetComponent<SoundSourceAudio>();
            if (soundSource != null)
            {
                soundSource.cacheFilePath = cacheFilePath;
                soundSource.LoadAudioLibrary();
                EditorUtility.SetDirty(prefabAsset);
                PrefabUtility.SavePrefabAsset(prefabAsset);
                Debug.Log("Updated prefab: " + path);
            }
        }
        AssetDatabase.Refresh();
    }

    // Existing menu item for manual update.
    [MenuItem("Soundscape/Update Audio Cache")]
    public static void AutoUpdateAudioCache()
    {
        string defaultCacheFilePath = "Assets/AudioDataCache.json";
        string savedRootFolder = "";

        if (File.Exists(defaultCacheFilePath))
        {
            string json = File.ReadAllText(defaultCacheFilePath);
            AudioLibrary audioLibrary = JsonUtility.FromJson<AudioLibrary>(json);
            if (audioLibrary != null)
            {
                savedRootFolder = audioLibrary.rootFolderPath;
            }
        }

        if (string.IsNullOrEmpty(savedRootFolder) || !Directory.Exists(savedRootFolder))
        {
            EditorUtility.DisplayDialog("Warning", "No valid root folder is set. Please set a root folder in the Audio Updater Settings first.", "OK");
            return;
        }

        AudioDataCacher cacher = CreateInstance<AudioDataCacher>();
        cacher.rootFolderPath = savedRootFolder;
        cacher.CacheAudioData();
    }
}

// This static class ensures that the AutoUpdateAudioCache method is also called on editor startup.
[InitializeOnLoad]
public static class AudioCacheUpdaterStartup
{
    private const string SessionKey = "AudioCacheUpdater_Initialized";

    static AudioCacheUpdaterStartup()
    {
        if (!SessionState.GetBool(SessionKey, false)) // Runs only when the editor starts
        {
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += () =>
            {
                AudioDataCacher.AutoUpdateAudioCache();
            };
        }
    }
}
