namespace CGProject1.FileFormat
{
    public interface IWriter
    {
        byte[] TryWrite(FileInfo fileInfo);
    }
}