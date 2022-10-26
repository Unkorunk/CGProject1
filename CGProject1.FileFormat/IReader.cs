namespace FileFormats
{
    public interface IReader
    {
        bool TryRead(byte[] data, out FileInfo fileInfo);
    }
}