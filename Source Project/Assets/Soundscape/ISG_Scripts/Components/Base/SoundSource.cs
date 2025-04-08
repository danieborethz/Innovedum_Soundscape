using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
public class SoundSource : MonoBehaviour
{
    [SerializeField]
    [HideInInspector]
    public string[] sourceTypes = { "mono", "stereo", "multi" };

    protected OSC osc;
    protected Transform mainCameraTransform;
    protected Vector3 relativePosition;
    protected float occlusion;
    protected Camera mainCamera;

    // Cached previous values to check for changes
    private Vector3 lastRelativePosition = Vector3.zero;
    private float lastOcclusion = -1f;
    private float lastMultiSize = -1f;
    private Dictionary<string, float> lastParameterValues = new Dictionary<string, float>();

    private int lastSourceSelection = -1;

    // Properties required for SendMessages
    protected virtual string SourceType { get; }
    protected virtual int SourceSelection { get; }
    protected virtual float MultiSize { get; }
    protected virtual List<ParameterValue> ParameterValues { get; }

    [System.Serializable]
    public class ParameterValue
    {
        public string key;
        public float minValue;
        public float maxValue;
        public float currentValue;
    }

    protected virtual void Awake()
    {
        // Base class Awake logic
    }

    protected virtual void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
            mainCameraTransform = mainCamera.transform;

        osc = FindObjectOfType<OSC>();
    }

    protected virtual void Update()
    {
        CalculateRelativePosition();
        CalculateOcclusion();
        SendMessages();
    }

    protected virtual void CalculateRelativePosition()
    {
        if (mainCameraTransform != null)
        {
            Vector3 soundSourcePosition = transform.position;
            Vector3 cameraPosition = mainCameraTransform.position;
            relativePosition = soundSourcePosition - cameraPosition;
        }
    }

    protected void CalculateOcclusion()
    {
        if (mainCameraTransform != null)
        {
            Vector3 direction = transform.position - mainCameraTransform.position;
            float distance = direction.magnitude;
            Ray ray = new Ray(mainCamera.transform.position, direction);
            RaycastHit hitInfo;

            if (Physics.Raycast(ray, out hitInfo, distance))
            {
                if (hitInfo.collider.gameObject != gameObject)
                {
                    float objectDiameter = 0f;
                    Collider collider = hitInfo.collider;

                    // Calculate the object's diameter (or area for a box) while ignoring the up (Y) axis.
                    if (collider is SphereCollider sphereCollider)
                    {
                        Vector3 scale = sphereCollider.transform.lossyScale;
                        float horizontalScale = Mathf.Max(scale.x, scale.z);
                        objectDiameter = sphereCollider.radius * 2f * horizontalScale;
                    }
                    else if (collider is BoxCollider boxCollider)
                    {
                        Vector3 scale = boxCollider.transform.lossyScale;
                        float effectiveX = boxCollider.size.x * scale.x;
                        float effectiveZ = boxCollider.size.z * scale.z;
                        objectDiameter = effectiveX * effectiveZ;  // Note: This calculates an area.
                    }
                    else if (collider is CapsuleCollider capsuleCollider)
                    {
                        Vector3 scale = capsuleCollider.transform.lossyScale;
                        float horizontalScale = Mathf.Max(scale.x, scale.z);
                        objectDiameter = capsuleCollider.radius * 2f * horizontalScale;
                    }
                    else
                    {
                        objectDiameter = collider.bounds.size.x * collider.bounds.size.z;
                    }

                    float threshold = SceneManager.Instance != null ? SceneManager.Instance.OcclusionDiameterThreshold : 1.0f;
                    occlusion = objectDiameter > threshold ? 1.0f : 0.0f;
                }
                else
                {
                    occlusion = 0.0f;
                }
            }
            else
            {
                occlusion = 0.0f;
            }
        }
    }

    protected virtual void SendMessages()
    {
        if (osc == null) return;

        string sourceType = SourceType;
        int sourceSelection = SourceSelection;
        float multiSize = MultiSize;
        List<ParameterValue> parameterValues = ParameterValues;
        string source = $"/source/{sourceType}/{sourceSelection + 1}";

        // Define an epsilon for float comparisons.
        const float epsilon = 0.0001f;

        if (Mathf.Abs(lastSourceSelection - sourceSelection) > epsilon)
        {
            OscMessage nameMessage = new OscMessage
            {
                address = $"/name{source}"
            };
            nameMessage.values.Add(name.Replace(" ", "_"));
            osc.Send(nameMessage);

            lastSourceSelection = sourceSelection;
        }

        // Send relative position only if it changed (using Vector3.Distance for tolerance)
        if (Vector3.Distance(lastRelativePosition, relativePosition) > epsilon)
        {
            OscMessage posMessage = new OscMessage
            {
                address = $"{source}/xyz"
            };
            posMessage.values.Add(relativePosition.x);
            posMessage.values.Add(relativePosition.z);
            posMessage.values.Add(relativePosition.y);
            osc.Send(posMessage);

            lastRelativePosition = relativePosition;
        }

        // Send occlusion if it changed
        if (Mathf.Abs(lastOcclusion - occlusion) > epsilon)
        {
            OscMessage occMessage = new OscMessage
            {
                address = $"/occlusion/{sourceType}/{sourceSelection + 1}"
            };
            occMessage.values.Add(occlusion);
            osc.Send(occMessage);
            lastOcclusion = occlusion;
        }

        // For multi sources, send multiSize if it changed
        if (sourceType == "multi" && Mathf.Abs(lastMultiSize - multiSize) > epsilon)
        {
            OscMessage sizeMessage = new OscMessage
            {
                address = $"{source}/size"
            };
            sizeMessage.values.Add(multiSize);
            osc.Send(sizeMessage);

            lastMultiSize = multiSize;
        }

        // Send parameter messages only if their value changed.
        if (parameterValues != null)
        {
            foreach (var parameter in parameterValues)
            {
                // Check if we've sent a value before for this parameter.
                float lastValue = 0f;
                bool hasPrevious = lastParameterValues.TryGetValue(parameter.key, out lastValue);

                if (!hasPrevious || Mathf.Abs(lastValue - parameter.currentValue) > epsilon)
                {
                    OscMessage paramMessage = new OscMessage
                    {
                        address = $"{source}/{parameter.key}"
                    };
                    paramMessage.values.Add(parameter.currentValue);
                    osc.Send(paramMessage);

                    lastParameterValues[parameter.key] = parameter.currentValue;
                }
            }
        }
    }
}
