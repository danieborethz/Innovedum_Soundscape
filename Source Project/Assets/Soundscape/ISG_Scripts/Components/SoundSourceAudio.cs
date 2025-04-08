using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.IO;

[System.Serializable]
public class SoundSourceAudio : SoundSource
{
    [SerializeField]
    [HideInInspector]
    private string _cacheFilePath;

    public string cacheFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(_cacheFilePath))
            {
                _cacheFilePath = Path.Combine(Application.dataPath, "AudioDataCache.json");
            }
            return _cacheFilePath;
        }
        set
        {
            _cacheFilePath = value;
        }
    }


    [SerializeField]
    [HideInInspector]
    public int selectedCategoryIndex = 0;

    [SerializeField]
    [HideInInspector]
    public string selectedCategoryName;

    [SerializeField]
    [HideInInspector]
    public int selectedAudioIndex = 0;

    [SerializeField]
    [HideInInspector]
    public string selectedAudioName;

    [SerializeField]
    [HideInInspector]
    public int sourceTypeSelection = 0;

    [SerializeField]
    [HideInInspector]
    public int sourceSelection = 0;

    [SerializeField]
    [HideInInspector]
    public float multiSize = 1.0f;

    [SerializeField]
    [HideInInspector]
    public int monoSources;

    [SerializeField]
    [HideInInspector]
    public int stereoSources;

    [SerializeField]
    [HideInInspector]
    public int multiSources;

    [SerializeField]
    [HideInInspector]
    public AudioLibrary audioLibrary;

    [SerializeField]
    [HideInInspector]
    public List<AudioCategory> categories;

    [SerializeField]
    [HideInInspector]
    public List<AudioItem> currentAudioItems;

    [SerializeField]
    [HideInInspector]
    public List<ParameterValue> parameterValues = new List<ParameterValue>();

    // Override properties to provide data to the base class
    protected override string SourceType => sourceTypes[sourceTypeSelection];
    protected override int SourceSelection => sourceSelection;
    protected override float MultiSize => multiSize;
    protected override List<ParameterValue> ParameterValues => parameterValues;

    private bool enableAudio = true;
    private bool isQuitting = false;

    [SerializeField]
    [HideInInspector]
    public bool _initialized = false;

    private void Reset()
    {
        LoadAudioLibrary();
    }

    protected override void Start()
    {
        base.Start();
        UpdateSoundFile();
        UpdateStatus();
    }

    protected override void Update()
    {
        base.Update();
        // Any additional update logic specific to SoundSourceAudio
    }

    private void OnEnable()
    {
        enableAudio = true;
        UpdateStatus();
    }

    private void OnDisable()
    {
        if (isQuitting) return;
        enableAudio = false;
        UpdateStatus();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    void UpdateSoundFile()
    {
        // If sourceSelection is -1, skip
        if (sourceSelection == -1) return;

        if (osc != null && currentAudioItems != null && selectedAudioIndex < currentAudioItems.Count)
        {
            OscMessage message = new OscMessage
            {
                address = $"/source/{SourceType}/{SourceSelection + 1}/soundpath"
            };

            string filePath = currentAudioItems[selectedAudioIndex].audioFilePath;

            // Check if the operating system is Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                filePath = filePath.Replace("\\", "/");
            }

            message.values.Add(filePath);
            osc.Send(message);
        }
    }


    private void UpdateStatus()
    {
        // If sourceSelection is -1, skip
        if (sourceSelection == -1) return;
        if (osc == null) return;

        OscMessage msg = new OscMessage
        {
            address = $"/status/source/{SourceType}/{SourceSelection + 1}/status"
        };
        msg.values.Add((enableAudio) ? 1 : 0);
        osc.Send(msg);

    }

    public void LoadAudioLibrary()
    {
        if (File.Exists(cacheFilePath))
        {
            string json = File.ReadAllText(cacheFilePath);
            audioLibrary = JsonUtility.FromJson<AudioLibrary>(json);
            categories = audioLibrary.categories;
            monoSources = audioLibrary.monoSources;
            stereoSources = audioLibrary.stereoSources;
            multiSources = audioLibrary.ambisonicSources;

            // Update currentAudioItems based on the currently selected category
            if (selectedCategoryIndex >= 0 && selectedCategoryIndex < categories.Count)
            {
                currentAudioItems = categories[selectedCategoryIndex].audioItems;
            }
            else
            {
                currentAudioItems = new List<AudioItem>();
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        else
        {
            Debug.LogError("Cache file not found at: " + cacheFilePath);
        }
    }

}
