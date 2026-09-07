using System.IO;
using System.Linq;

namespace LanternDepths
{
    public static class SaveFile
    {
        public static FileStream OpenSession(string directory)
        {
            Directory.CreateDirectory(directory);
            return new FileStream(Path.Combine(directory, "session.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        public static byte[] Read(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > RunSave.MaxBytes) throw new InvalidDataException("Save file is too large.");
            using var reader = new BinaryReader(stream);
            return reader.ReadBytes((int)stream.Length);
        }
        public static void Write(string path, byte[] data)
        {
            if (File.Exists(path) && new FileInfo(path).Length == data.Length && Read(path).SequenceEqual(data)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temporary = path + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(data, 0, data.Length); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
