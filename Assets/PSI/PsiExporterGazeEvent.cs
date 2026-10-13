using System;
using UnityEngine;

public class PsiExporterGazeEvent : PsiExporter<ObjectGazeEvent>
{

    public void SendGazeEvent(int userId, int objectId, bool isGazing, string objectType)
    {
        var gazeEvent = new ObjectGazeEvent(
            objectType,
            userId,
            $"{objectId}",
            isGazing);

        Post(gazeEvent);
    }

    public void Post(ObjectGazeEvent message)
    {
        //PsiManager.AddLog($"BEFORE _ {message.type}_{message.userID}_{message.objectID}_{message.status}");
        if (CanSend())
        {

            var now = GetCurrentTime();
            Out.Post(message, now);
        }
    }


#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<ObjectGazeEvent> GetSerializer()
    {
        return PsiFormatGazeObjectEvent.GetFormat();
    }
#endif
}
