namespace FileFormats
{
    public interface IWriter
    {
        byte[] TryWrite(FileInfo fileInfo);
    }
}