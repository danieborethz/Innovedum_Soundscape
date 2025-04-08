using System;
using UnityEngine;

public class PlayerReverbController : MonoBehaviour
{
    private ReverbZone[] allReverbZones;  // All zones in the scene

    protected OSC osc;

    // Accumulated reverb parameters
    private float accumulatedRoomSize = 0f;
    private float lastSentAccumulatedRoomSize = 0f;

    private float accumulatedDecayTime = 0f;
    private float lastSentAccumulatedDecayTime = 0f;

    private float accumulatedWetDryMix = 0f;
    private float lastSentAccumulatedWetDryMix = 0f;

    private float accumulatedEq = 0f;
    private float lastSentAccumulatedEq = 0f;

    private SceneManager sceneManager;

    private void Start()
    {
        // Find references
        osc = FindObjectOfType<OSC>();
        sceneManager = SceneManager.Instance;

        // Grab all ReverbZones in the scene
        allReverbZones = FindObjectsOfType<ReverbZone>();
    }

    private void Update()
    {
        // Blend parameters from all zones
        InterpolateAndLogParameters();

        // Send changes via OSC if needed
        SendChangedMessages();
    }

    private void InterpolateAndLogParameters()
    {
        float totalWeight = 0f;

        // Reset accumulators
        accumulatedRoomSize = 0f;
        accumulatedDecayTime = 0f;
        accumulatedWetDryMix = 0f;
        accumulatedEq = 0f;

        // For each zone, compute how much it contributes
        foreach (ReverbZone zone in allReverbZones)
        {
            float weight = CalculateZoneWeight(zone);
            if (weight <= 0f) continue;

            totalWeight += weight;
            accumulatedRoomSize += zone.RoomSize * weight;
            accumulatedDecayTime += zone.DecayTime * weight;
            accumulatedWetDryMix += zone.WetDryMix * weight;
            accumulatedEq += zone.Eq * weight;
        }

        float weightDifference = 1.0f - totalWeight;
        accumulatedRoomSize += sceneManager.globalRoomSize * weightDifference;
        accumulatedDecayTime += sceneManager.globalDecayTime * weightDifference;
        accumulatedWetDryMix += sceneManager.globalWetDryMix * weightDifference;
        accumulatedEq += sceneManager.globalEq * weightDifference;

        // If total weight is zero => fallback to global reverb
        /*if (Mathf.Approximately(totalWeight, 0f))
        {
            accumulatedRoomSize = sceneManager.globalRoomSize;
            accumulatedDecayTime = sceneManager.globalDecayTime;
            accumulatedWetDryMix = sceneManager.globalWetDryMix;
            accumulatedEq = sceneManager.globalEq;
        }
        else
        {
            // Normalize final values
            accumulatedRoomSize /= totalWeight;
            accumulatedDecayTime /= totalWeight;
            accumulatedWetDryMix /= totalWeight;
            accumulatedEq /= totalWeight;
        }*/
    }

    private float CalculateZoneWeight(ReverbZone zone)
    {
        // 1) Transform the player's position into the zone's local space
        Vector3 localPos = zone.transform.InverseTransformPoint(transform.position);

        // 2) Normalize the local position using the zone's radii
        Vector3 normalizedPos = new Vector3(
            localPos.x / zone.radii.x,
            localPos.y / zone.radii.y,
            localPos.z / zone.radii.z
        );

        // 3) distance=1 means exactly on the zone boundary. 
        //    distance>1 is outside the zone. 
        float distance = normalizedPos.magnitude;

        // 4) Incorporate fadeRadius:
        //    If distance >= 1 + fadeRadius => weight=0
        float outerBoundary = 1f + zone.fadeRadius;
        if (distance >= outerBoundary)
        {
            return 0f;
        }

        // If within the inner boundary (distance <= 1.0) => full weight
        if (distance <= 1f)
        {
            return 1f;
        }

        // Otherwise, we are between 1 and (1+fadeRadius):
        // weight linearly decreases from 1 -> 0 across that range
        float fadeDist = distance - 1f;            // how far beyond the inner boundary
        float fadeRange = zone.fadeRadius;         // total fade distance
        float fadeFactor = 1f - (fadeDist / fadeRange); // linear ramp from 1 down to 
        return Mathf.Clamp01(fadeFactor);
    }

    private void SendChangedMessages()
    {
        if (osc == null) return;

        // Compare with last sent values; send if changed

        if (!Mathf.Approximately(lastSentAccumulatedRoomSize, accumulatedRoomSize))
        {
            OscMessage msg = new OscMessage { address = "/reverb/roomsize" };
            msg.values.Add(accumulatedRoomSize);
            osc.Send(msg);
            lastSentAccumulatedRoomSize = accumulatedRoomSize;
        }

        if (!Mathf.Approximately(lastSentAccumulatedDecayTime, accumulatedDecayTime))
        {
            OscMessage msg = new OscMessage { address = "/reverb/decaytime" };
            msg.values.Add(accumulatedDecayTime);
            osc.Send(msg);
            lastSentAccumulatedDecayTime = accumulatedDecayTime;
        }

        if (!Mathf.Approximately(lastSentAccumulatedWetDryMix, accumulatedWetDryMix))
        {
            OscMessage msg = new OscMessage { address = "/reverb/mix" };
            msg.values.Add(accumulatedWetDryMix);
            osc.Send(msg);
            lastSentAccumulatedWetDryMix = accumulatedWetDryMix;
        }

        if (!Mathf.Approximately(lastSentAccumulatedEq, accumulatedEq))
        {
            OscMessage msg = new OscMessage { address = "/reverb/eq" };
            msg.values.Add(accumulatedEq);
            osc.Send(msg);
            lastSentAccumulatedEq = accumulatedEq;
        }
    }
}
