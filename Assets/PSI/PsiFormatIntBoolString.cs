using Microsoft.Psi.Interop.Serialization;
using System.IO;

public class PsiFormatIntBoolString
{
    public static Format<(int, bool, string)> GetFormat()
    {
        return new Format<(int, bool, string)>(Write, Read);
    }

    public static void Write((int,bool,string) data, BinaryWriter writer)
    {
        writer.Write(data.Item1);
        writer.Write(data.Item2);
        writer.Write(data.Item3);
    }

    public static (int,bool,string) Read(BinaryReader reader)
    {
        return new(reader.ReadInt32(), reader.ReadBoolean(),reader.ReadString());
    }
}
