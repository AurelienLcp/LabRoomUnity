using Microsoft.Psi;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PsiExporterPiecesState : PsiExporter<PieceStatus>
{

    public override void Start()
    {
        base.Start();
    }
    
    public void Post(PieceStatus message)
    {
        //Debug.Log("BEFORE_State_" + $"_{message.userID}_{message.state}_{message.objectID}_{message.lastZone}");
        //Debug.Log("AFTER_State_" + $"_{message.userID}_{message.state}_{message.objectID}_{message.lastZone}");
        if (CanSend())
        {
            var now = GetCurrentTime();
            Out.Post(message, now);
            /*if(PsiManager != null) */
            PsiManager.AddLog(message.userID + "_" + message.state + "_" + message.objectID);
            //PsiManager.AddLog($"{message.timeRemaining}_{message.status}");
        }
    }

    public void SendTest(string message)
    {
        PieceStatus test = new PieceStatus(int.Parse(message), "test", State.Grab, false, "test", Location.Hand);
        Post(test);
    }

#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<PieceStatus> GetSerializer()
    {
        return PsiFormatPiecesState.GetFormat();
    }
#endif
}
