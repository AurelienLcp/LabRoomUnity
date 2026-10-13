using Microsoft.Psi.Data;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PsiExporterIntBoolString : PsiExporter<(int, bool, string)>
{
    public int id;
    public bool val;
    public string mes = "Desactivated";

    public void LogWaterJet(string message)
    {
        mes = message;
        sendData();
    }


    public void sendData()
    {
        (int, bool, string) data;
        data.Item1 = id;
        data.Item2 = val;
        data.Item3 = mes;
        if (CanSend())
        {
            Out.Post(data, GetCurrentTime());
        }
    }

    public void SetandSendData(int identity, bool inside, string zone)
    {
        id = identity;
        val = inside;
        mes = zone;
        sendData();
    }


#if PSI_TCP_STREAMS
    protected override Microsoft.Psi.Interop.Serialization.IFormatSerializer<(int, bool, string)> GetSerializer()
    {
        return PsiFormatIntBoolString.GetFormat();
    }
#endif
}
