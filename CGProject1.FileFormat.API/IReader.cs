namespace CGProject1.FileFormat.API
{
    public interface IReader
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream">stream is open after calling this function</param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        bool TryRead(Stream stream, out FileInfo fileInfo);
    }
}