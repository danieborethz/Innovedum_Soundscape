using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using System.Runtime.InteropServices;

public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }

    protected OSC osc;

    [Header("Global Settings")]
    [Range(1.0f, 10f)]
    public float windIntensity = 1.0f;

    [SerializeField]
    private float occlusionDiameterThreshold = 1.0f;

    // Add these parameters for when the player is not inside any local zone
    [Range(10f, 50000f)]
    public float globalRoomSize = 50f;

    [Range(0f, 3f)]
    public float globalDecayTime = 2f;

    [Range(0f, 1f)]
    public float globalWetDryMix = 1f;

    [Range(0f, -20f)]
    public float globalEq = -10f;

    [SerializeField]
    [HideInInspector] // Hide from default inspector so we can show a dropdown instead
    public string ambientSoundFile = "";

    // NEW: Toggle to enable/disable ambisonic sound
    [SerializeField]
    private bool enableAmbisonic = true;

    [Range(0f, 5f)]
    public float distanceScale = 0.5f;


    [SerializeField]
    [HideInInspector]
    private string _cacheFilePath;

    public string cacheFilePath
    {
        get
        {
            _cacheFilePath = Path.Combine(Application.dataPath, "AudioDataCache.json");
            return _cacheFilePath;
        }
        set
        {
            _cacheFilePath = value;
        }
    }

    [SerializeField, HideInInspector]
    public AudioLibrary audioLibrary;
    [SerializeField, HideInInspector]
    public List<AudioItem> ambisonicAudioItems = new List<AudioItem>();
    [SerializeField, HideInInspector]
    public int selectedAmbisonicIndex = 0;

    public Material windMaterial;


    private float lastWindIntensity;
    private float lastDistanceScale;

    private bool lastEnableAmbisonic;

    private float lastSentWindIntensity;
    private string lastSentAmbientSoundFile;
    private float lastSentDistanceScale;

    private bool lastSentEnableAmbisonic;

    private MeshRenderer[] windMeshes;

    // NEW: Property to access the ambisonic toggle (if you need to reference it in code)
    public bool EnableAmbisonic
    {
        get => enableAmbisonic;
        set
        {
            if (enableAmbisonic != value)
            {
                enableAmbisonic = value;
                SendChangedMessages();
            }
        }
    }

    // NEW: Property to access the occlusion threshold
    public float OcclusionDiameterThreshold => occlusionDiameterThreshold;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        lastWindIntensity = windIntensity;
        lastDistanceScale = distanceScale;
        lastEnableAmbisonic = enableAmbisonic;

        lastSentWindIntensity = float.MinValue;
        lastSentDistanceScale = float.MinValue;
        lastSentAmbientSoundFile = null;
        lastSentEnableAmbisonic = !enableAmbisonic;

        FindWindMaterialMeshes();
        UpdateWindMaterials();

        osc = FindObjectOfType<OSC>();
        SendChangedMessages();

        if (osc == null) return;
        OscMessage message = new OscMessage { address = "/master/start" };
        message.values.Add(0);
        osc.Send(message);
    }

    private void Update()
    {

        if (Math.Abs(lastWindIntensity - windIntensity) > Mathf.Epsilon)
        {
            lastWindIntensity = windIntensity;
            UpdateWindMaterials();
            SendChangedMessages();
        }

        if (Math.Abs(lastDistanceScale - distanceScale) > Mathf.Epsilon)
        {
            lastDistanceScale = distanceScale;
            SendChangedMessages();
        }

        if (ambientSoundFile != lastSentAmbientSoundFile)
        {
            SendChangedMessages();
        }

        if (enableAmbisonic != lastSentEnableAmbisonic)
        {
            lastEnableAmbisonic = enableAmbisonic;
            SendChangedMessages();
        }
    }

    private void FindWindMaterialMeshes()
    {
        MeshRenderer[] allMeshes = FindObjectsOfType<MeshRenderer>();
        var meshList = new List<MeshRenderer>();

        foreach (var mesh in allMeshes)
        {
            Material[] mats = mesh.sharedMaterials;
            foreach (var mat in mats)
            {
                if (mat == windMaterial)
                {
                    meshList.Add(mesh);
                    break;
                }
            }
        }

        windMeshes = meshList.ToArray();
    }

    private void UpdateWindMaterials()
    {
        if (windMaterial == null || windMeshes == null) return;

        foreach (var mesh in windMeshes)
        {
            Material[] mats = mesh.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i].name.Contains(windMaterial.name))
                {
                    mats[i].SetFloat("_MotionSpeed", windIntensity);
                }
            }
        }
    }

    private void SendChangedMessages()
    {
        if (osc == null) return;

        if (Math.Abs(lastSentWindIntensity - windIntensity) > Mathf.Epsilon)
        {
            OscMessage message = new OscMessage { address = "/master/windintensity" };
            message.values.Add(windIntensity);
            osc.Send(message);
            lastSentWindIntensity = windIntensity;
        }

        if (Math.Abs(lastSentDistanceScale - distanceScale) > Mathf.Epsilon)
        {
            OscMessage message = new OscMessage { address = "/master/scaledist" };
            message.values.Add(distanceScale);
            osc.Send(message);
            lastSentDistanceScale = distanceScale;
        }

        if (lastSentEnableAmbisonic != enableAmbisonic)
        {
            OscMessage message = new OscMessage { address = "/source/bformat/1/status" };
            message.values.Add(enableAmbisonic ? 1 : 0);
            osc.Send(message);
            lastSentEnableAmbisonic = enableAmbisonic;
        }

        // NEW: Only send the ambisonic OSC message if enabled
        if (enableAmbisonic)
        {
            if (ambientSoundFile != lastSentAmbientSoundFile && !string.IsNullOrEmpty(ambientSoundFile))
            {
                OscMessage message = new OscMessage { address = "/source/bformat/1/soundpath" };

                string filePath = ambientSoundFile;

                // Check if the operating system is Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    filePath = filePath.Replace("\\", "/");
                }

                message.values.Add(filePath);
                osc.Send(message);
                lastSentAmbientSoundFile = ambientSoundFile;
            }
        }
        else
        {
            // Optionally, reset the cached ambient sound so it will send again when re-enabled.
            lastSentAmbientSoundFile = "";
        }
    }

    public void LoadAudioLibrary()
    {
        if (File.Exists(cacheFilePath))
        {
            string json = File.ReadAllText(cacheFilePath);
            audioLibrary = JsonUtility.FromJson<AudioLibrary>(json);

            var ambisonicsCategory = audioLibrary.categories.FirstOrDefault(c =>
                string.Equals(c.categoryName, "ambisonics", StringComparison.OrdinalIgnoreCase));
            if (ambisonicsCategory != null)
            {
                ambisonicAudioItems = ambisonicsCategory.audioItems;
            }
            else
            {
                ambisonicAudioItems = new List<AudioItem>();
            }

            // Immediately update the ambientSoundFile based on the selected index.
            if (ambisonicAudioItems.Count > 0)
            {
                if (selectedAmbisonicIndex < 0 || selectedAmbisonicIndex >= ambisonicAudioItems.Count)
                {
                    selectedAmbisonicIndex = 0;
                }
                ambientSoundFile = ambisonicAudioItems[selectedAmbisonicIndex].audioFilePath;
            }
            else
            {
                ambientSoundFile = "";
            }
        }
        else
        {
            Debug.LogError("Cache file not found at: " + cacheFilePath);
        }
    }


    private void OnApplicationQuit()
    {
        if (osc == null) return;
        OscMessage message = new OscMessage { address = "/master/stop" };
        message.values.Add(0);
        osc.Send(message);
    }
}
