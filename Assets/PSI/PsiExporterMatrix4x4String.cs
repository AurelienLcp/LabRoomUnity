using System;
using System.Numerics;
using Matrix4x4 = System.Numerics.Matrix4x4;
using UnityEngine;

public class PsiExporterMatrix4x4String: PsiExporter<string>
{
    private System.Numerics.Matrix4x4 PreviousMatrix4x4;

    void Update()
    {
        System.Numerics.Matrix4x4 newMatrix = new System.Numerics.Matrix4x4(transform.localToWorldMatrix[0, 0], transform.localToWorldMatrix[1, 0], transform.localToWorldMatrix[2, 0], transform.localToWorldMatrix[3, 0],
                                                                            transform.localToWorldMatrix[0, 1], transform.localToWorldMatrix[1, 1], transform.localToWorldMatrix[2, 1], transform.localToWorldMatrix[3, 1],
                                                                            transform.localToWorldMatrix[0, 2], transform.localToWorldMatrix[1, 2], transform.localToWorldMatrix[2, 2], transform.localToWorldMatrix[3, 2],
                                                                            transform.localToWorldMatrix[0, 3], transform.localToWorldMatrix[1, 3], transform.localToWorldMatrix[2, 3], transform.localToWorldMatrix[3, 3]);

        string message = $"{newMatrix.M11};{newMatrix.M12};{newMatrix.M13};{newMatrix.M14};{newMatrix.M21};{newMatrix.M22};{newMatrix.M23};{newMatrix.M24};{newMatrix.M31};{newMatrix.M32};{newMatrix.M33};{newMatrix.M34};{newMatrix.M41};{newMatrix.M42};{newMatrix.M43};{newMatrix.M44}";
        if (CanSend() && PreviousMatrix4x4 != newMatrix)
        {
            Debug.Log($"OT:{Timestamp}_1_{newMatrix.M11}_{newMatrix.M12}_{newMatrix.M13}_{newMatrix.M14}\n2_{newMatrix.M21}_{newMatrix.M22}_{newMatrix.M23}_{newMatrix.M24}");
            Out.Post(message, Timestamp);
            PreviousMatrix4x4 = newMatrix;
        }
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<System.String> GetSerializer()
    {
        return PsiFormatString.GetFormat();
    }
#endif
}