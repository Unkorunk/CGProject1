namespace FileFormats
{
    public interface IReader
    {
        public abstract bool TryRead(byte[] data, out FileInfo waveFile);
    }
}
