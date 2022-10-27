namespace CGProject1.FileFormat
{
    public interface IReader
    {
        bool TryRead(byte[] data, out FileInfo fileInfo);
    }
}