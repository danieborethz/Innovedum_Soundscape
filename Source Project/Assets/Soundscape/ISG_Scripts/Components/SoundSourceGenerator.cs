using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SoundSourceGenerator : SoundSource
{
    public bool enableGenerator = true;

    // Inverted so 0 => Water, 1 => Wind
    public string[] generatorTypes = { "Water", "Wind" };
    public int selectedGeneratorTypeIndex = 0;

    // ---- Water stuff ----
    public string[] waterTypes = { "Flow", "Drinking Fountain", "Splashing Fountain" };
    public int selectedWaterTypeIndex = 0;
    public float size = 1.0f;
    public float splashingTime = 5.0f;
    public float splashingBreak = 0.0f;

    // ---- Wind stuff ----
    public string[] foliageTypes = { "Needles", "Leaves" };
    public int selectedFoliageTypeIndex = 0;
    public float leavesTreeSize = 1.0f;

    [SerializeField, HideInInspector]
    private int sourceSelection = 0;

    // For bounding box / forest width
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Bounds combinedBounds;
    private float forestWidth;
    private Vector3 forestCenter;
    private Vector3 flowWaterCenter;

    // Keep track of the last values we sent for OSC
    private bool lastSentEnableGenerator;
    private int lastSentSelectedGeneratorTypeIndex;
    private int lastSentSelectedFoliageTypeIndex;
    private float lastSentLeavesTreeSize;
    private int lastSentSelectedWaterTypeIndex;
    private float lastSentSplashingTime;
    private float lastSentSplashingBreak;
    private float lastSentSize;
    private float lastSentForestWidth;
    private Vector3 lastSentForestCenter;
    private Vector3 lastSentFlowWaterCenter;

    // Implementation from the base SoundSource
    protected override string SourceType => "stereo";
    protected override int SourceSelection => sourceSelection;
    protected override float MultiSize => 1.0f;
    protected override List<ParameterValue> ParameterValues => null;

    private GameObject debugMesh;

    private bool isQuitting = false;

    public int SourceSelectionIndex
    {
        get => sourceSelection;
        set => sourceSelection = value;
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();

        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            AddOrReplaceMeshCollider();
        }

        // If started as Wind (1) and wind is globally enabled
        if (selectedGeneratorTypeIndex == 1)
        {
            CalculateBounds();
        }

        // Initialize the "last sent" trackers so first SendMessages() definitely sends
        lastSentEnableGenerator = !enableGenerator;
        lastSentSelectedGeneratorTypeIndex = -1;
        lastSentSelectedFoliageTypeIndex = -1;
        lastSentLeavesTreeSize = float.MinValue;
        lastSentSelectedWaterTypeIndex = -1;
        lastSentSplashingTime = float.MinValue;
        lastSentSplashingBreak = float.MinValue;
        lastSentSize = float.MinValue;
        lastSentForestWidth = 0f;
        lastSentForestCenter = Vector3.zero;

        SendMessages();

        int layer = LayerMask.NameToLayer("Debug");
        if (layer == -1)
        {
            Debug.LogWarning($"Layer 'Debug' does not exist!");
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<GameObject> objectsInLayer = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == layer)
            {
                debugMesh = obj;
                break;
            }
        }
    }

    private void OnEnable()
    {
        enableGenerator = true;

        SendMessages();
    }

    private void OnDisable()
    {
        if (isQuitting) return;
        enableGenerator = false;

        SendMessages();
    }
    private void OnApplicationQuit()
    {
        isQuitting = true;
    }


    protected override void Update()
    {
        base.Update();

        // If water (0), type=Flow (0), and globally water is enabled, track the flow
        if (selectedGeneratorTypeIndex == 0 && selectedWaterTypeIndex == 0)
        {
            if (meshCollider != null && mainCamera != null)
            {
                Vector3 cameraPosition = mainCamera.transform.position;
                Vector3 closestPoint = Physics.ClosestPoint(cameraPosition, meshCollider, meshCollider.transform.position, meshCollider.transform.rotation);
                flowWaterCenter = closestPoint - cameraPosition;

                if (debugMesh != null)
                {
                    debugMesh.transform.position = closestPoint;
                }
            }
        }
    }

    protected override void CalculateRelativePosition()
    {
        // If wind & globally enabled, use forest center - camera
        if (selectedGeneratorTypeIndex == 1 && transform.childCount > 1)
        {
            relativePosition = forestCenter - mainCamera.transform.position;
            Debug.Log("Calculate relative position custom");
        }
        // If water = Flow, use flowWaterCenter
        else if (selectedGeneratorTypeIndex == 0 && selectedWaterTypeIndex == 0)
        {
            relativePosition = flowWaterCenter;
        }
        else
        {
            // fallback
            Debug.Log("Calculate relative position fallback");
            base.CalculateRelativePosition();
        }
    }

    private void CalculateBounds()
    {
        if (transform.childCount > 1)
        {
            bool boundsInitialized = false;
            combinedBounds = new Bounds(transform.position, Vector3.zero);
            foreach (Transform child in transform)
            {
                Renderer childRenderer = child.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    if (!boundsInitialized)
                    {
                        combinedBounds = childRenderer.bounds;
                        boundsInitialized = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(childRenderer.bounds);
                    }
                }
            }

            forestWidth = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
            forestCenter = combinedBounds.center;
        }
        else
        {
            forestWidth = 0f;
        }
    }

    private void AddOrReplaceMeshCollider()
    {
        MeshCollider existingCollider = GetComponent<MeshCollider>();
        if (existingCollider != null)
        {
            Destroy(existingCollider);
        }

        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = true;
    }

    /// <summary>
    /// Protected method from base. This sends the actual OSC messages.
    /// Called in Start, OnValidate, or anytime we suspect changes.
    /// </summary>
    protected override void SendMessages()
    {
        base.SendMessages();
        if (osc == null) return;

        // generatorTypeIndex: 0 => Water, 1 => Wind
        string sourceBase = "/source/generator";
        string genType = (selectedGeneratorTypeIndex == 0) ? "water" : "wind";
        // +1 so internal index 0..7 => OSC channel 1..8
        int customSourceValue = SourceSelection + 1;

       // Debug.Log($"[SendMessages] genType={genType}, SourceSelection={SourceSelection}, customSourceValue={customSourceValue}");

        // Send the "status" if changed
        if (lastSentEnableGenerator != enableGenerator)
        {
            OscMessage statusMsg = new OscMessage
            {
                address = $"{sourceBase}/{genType}/{customSourceValue}/status"
            };
            statusMsg.values.Add(enableGenerator ? 1 : 0);
            osc.Send(statusMsg);

            lastSentEnableGenerator = enableGenerator;
        }

        // If not enabled, no need to send rest
        if (!enableGenerator) return;

        // If Water (0)
        if (selectedGeneratorTypeIndex == 0)
        {
            // Water type
            if (selectedWaterTypeIndex != lastSentSelectedWaterTypeIndex ||
                selectedGeneratorTypeIndex != lastSentSelectedGeneratorTypeIndex)
            {
                OscMessage msg = new OscMessage
                {
                    address = $"{sourceBase}/water/{customSourceValue}/type"
                };
                msg.values.Add(selectedWaterTypeIndex);
                osc.Send(msg);
                lastSentSelectedWaterTypeIndex = selectedWaterTypeIndex;
            }

            // Water size
            if (!Mathf.Approximately(size, lastSentSize) ||
                selectedGeneratorTypeIndex != lastSentSelectedGeneratorTypeIndex)
            {
                OscMessage msg = new OscMessage
                {
                    address = $"{sourceBase}/water/{customSourceValue}/size"
                };
                msg.values.Add(size);
                osc.Send(msg);
                lastSentSize = size;
            }

            // If "Splashing Fountain" (2), send timing
            if (selectedWaterTypeIndex == 2)
            {
                if (!Mathf.Approximately(splashingTime, lastSentSplashingTime) ||
                    selectedGeneratorTypeIndex != lastSentSelectedGeneratorTypeIndex)
                {
                    OscMessage tMsg = new OscMessage
                    {
                        address = $"{sourceBase}/water/{customSourceValue}/splashingtiming"
                    };
                    tMsg.values.Add(splashingTime);
                    osc.Send(tMsg);
                    lastSentSplashingTime = splashingTime;
                }

                if (!Mathf.Approximately(splashingBreak, lastSentSplashingBreak) ||
                    selectedGeneratorTypeIndex != lastSentSelectedGeneratorTypeIndex)
                {
                    OscMessage bMsg = new OscMessage
                    {
                        address = $"{sourceBase}/water/{customSourceValue}/splashingbreak"
                    };
                    bMsg.values.Add(splashingBreak);
                    osc.Send(bMsg);
                    lastSentSplashingBreak = splashingBreak;
                }
            }
        }
        else
        {
            // "Wind" (1)
            if (selectedFoliageTypeIndex != lastSentSelectedFoliageTypeIndex ||
                selectedGeneratorTypeIndex != lastSentSelectedGeneratorTypeIndex)
            {
                OscMessage msg = new OscMessage
                {
                    address = $"{sourceBase}/wind/{customSourceValue}/type"
                };
                msg.values.Add(selectedFoliageTypeIndex);
                osc.Send(msg);
                lastSentSelectedFoliageTypeIndex = selectedFoliageTypeIndex;
            }

            if (!Mathf.Approximately(leavesTreeSize, lastSentLeavesTreeSize) ||
                selectedGeneratorTypeIndex != lastSentSelectedGeneratorTypeIndex)
            {
                OscMessage msg = new OscMessage
                {
                    address = $"{sourceBase}/wind/{customSourceValue}/size"
                };
                msg.values.Add(leavesTreeSize);
                osc.Send(msg);
                lastSentLeavesTreeSize = leavesTreeSize;
            }

            if (!Mathf.Approximately(forestWidth, lastSentForestWidth) ||
                selectedGeneratorTypeIndex != lastSentSelectedGeneratorTypeIndex)
            {
                OscMessage msg = new OscMessage
                {
                    address = $"{sourceBase}/wind/{customSourceValue}/width"
                };

                // Example of mapping your forest size
                float mapped = MapValue(forestWidth, 1f, 100f, 1f, 10f);
                msg.values.Add((forestWidth > 0) ? mapped : 0);
                osc.Send(msg);
                lastSentForestWidth = forestWidth;
            }
        }

        // Update last-sent generator type
        if (selectedGeneratorTypeIndex != lastSentSelectedGeneratorTypeIndex)
        {
            lastSentSelectedGeneratorTypeIndex = selectedGeneratorTypeIndex;
        }
    }

    private float MapValue(float value, float inMin, float inMax, float outMin, float outMax)
    {
        if (value > inMax) value = inMax;
        if (value < inMin) value = inMin;
        return outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);
    }

#if UNITY_EDITOR
    protected void OnValidate()
    {
        // Called in editor if you tweak values in the default inspector
        // If you are modifying via a *custom* editor script, OnValidate() might not be always triggered
        //base.OnValidate();
        SendMessages();
    }
#endif

    /// <summary>
    /// PUBLIC method that forcibly calls SendMessages() from the Editor script.
    /// This is needed if OnValidate() isn't invoked for some reason.
    /// </summary>
    public void ForceSendMessages()
    {
        SendMessages();
    }
}
