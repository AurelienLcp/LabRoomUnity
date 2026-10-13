using Microsoft.Psi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsiExporterObjectInteraction : PsiExporter<ObjectInteraction>
{

    public override void Start()
    {
        base.Start();
    }
    public void Post(ObjectInteraction message)
    {
        Debug.Log("AFTER_State_" + $"_{message.userID}_{message.state}_{message.objectID}_{message.currentLocation}");
        if (CanSend())
        {
            var now = GetCurrentTime();
            Out.Post(message, now);
            //PsiManager.AddLog(message.userID + "_" + message.state + "_" + message.objectID);
        }
    }


#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<ObjectInteraction> GetSerializer()
    {
        return PsiFormatObjectInteraction.GetFormat();
    }
#endif
}
