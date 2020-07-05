namespace FileFormats
{
    public interface IWriter
    {
        public abstract byte[] TryWrite(FileInfo fileInfo);
    }
}
