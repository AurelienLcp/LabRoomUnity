using System;
using UnityEngine;

public class PsiExporterPositionOrientation
    : PsiExporter<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>
{
    private DateTime TimestampNow = DateTime.MinValue;
    private DateTime Timestamp = DateTime.UtcNow;
    private UnityEngine.Vector3 PreviousPosition = Vector3.down;
    private UnityEngine.Vector3 PreviousOrientation = Vector3.down;
    public bool isLocal = false;
    [SerializeField] public double FrequencyMilliseconds = 250;

    void Update()
    {
        TimestampNow = DateTime.UtcNow;
        var position = Vector3.zero;
        var orientation = Vector3.zero;
        var now = GetCurrentTime();
        if (isLocal)
        {
            position = gameObject.transform.localPosition;
            orientation = gameObject.transform.localEulerAngles;
        }
        else
        {
            position = gameObject.transform.position;
            orientation = gameObject.transform.eulerAngles;
        }

        if (CanSend())
        {
            if (Timestamp.AddMilliseconds(FrequencyMilliseconds) < TimestampNow)
            {
                Out.Post(new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3(position.x, position.y, position.z), new System.Numerics.Vector3(orientation.x, orientation.y, orientation.z)), TimestampNow);
                Timestamp = TimestampNow;
                PreviousPosition = position;
                PreviousOrientation = orientation;
            }
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetSerializer()
    {
        return PsiFormatPositionOrientation.GetFormat();
    }
#endif
}