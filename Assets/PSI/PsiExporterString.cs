using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;
using UnityEngine.UIElements;
using Unity.Netcode;

public class PsiExporterString : PsiExporter<System.String>
{
    public void Post(string message)
    {
        //PsiManager.AddLog("ClientID_" + NetworkManager.Singleton.LocalClientId);
        PsiManager.AddLog("Before_" + message);
        if (CanSend())
        {
            var now = GetCurrentTime();
            Out.Post(message, now);
            PsiManager.AddLog(message);
        }
    }


#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<System.String> GetSerializer()
    {
        return PsiFormatString.GetFormat();
    }
#endif
}
