using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Listener : MonoBehaviour
{
    private OSC osc;
    // Start is called before the first frame update
    void Start()
    {
        osc = FindObjectOfType<OSC>();
    }

    // Update is called once per frame
    void Update()
    {
        OscMessage message = new OscMessage();

        message.address = "/listener/ypr";
        message.values.Add(transform.rotation.eulerAngles.y);
        message.values.Add(transform.rotation.eulerAngles.x);
        message.values.Add(transform.rotation.eulerAngles.z);
        osc.Send(message);
    }
}
