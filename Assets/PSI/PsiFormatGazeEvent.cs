using Microsoft.Psi.Interop.Serialization;
using System.IO;

public class PsiFormatGazeObjectEvent
{
    public static Format<ObjectGazeEvent> GetFormat()
    {
        return new Format<ObjectGazeEvent>(WriteGazeEvent, ReadGazeEvent);
    }

    public static void WriteGazeEvent(ObjectGazeEvent gazeEvent, BinaryWriter writer)
    {
        writer.Write(gazeEvent.userID);
        writer.Write(gazeEvent.objectID);
        writer.Write(gazeEvent.type);
        writer.Write(gazeEvent.status);
    }

    public static ObjectGazeEvent ReadGazeEvent(BinaryReader reader)
    {
        int gazerid = reader.ReadInt32();
        string objectid = reader.ReadString();
        string type = reader.ReadString();
        bool status = reader.ReadBoolean();

        return new ObjectGazeEvent(type, gazerid, objectid, status);
    }
}