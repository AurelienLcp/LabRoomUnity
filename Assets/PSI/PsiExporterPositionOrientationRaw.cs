using System;
using UnityEngine;

public class PsiExporterPositionOrientationRaw
    : PsiExporter<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>>
{


    public void SendRawData(Vector3 position, Quaternion rot)
    {
        Vector3 orientation = rot.eulerAngles;
        if (CanSend())
        {
            Out.Post(new Tuple<System.Numerics.Vector3, System.Numerics.Vector3>(new System.Numerics.Vector3(position.x, position.y, position.z), new System.Numerics.Vector3(orientation.x, orientation.y, orientation.z)), GetCurrentTime());
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<Tuple<System.Numerics.Vector3, System.Numerics.Vector3>> GetSerializer()
    {
        return PsiFormatPositionOrientation.GetFormat();
    }
#endif
}