namespace CGProject1.FileFormat.API
{
    public interface IWriter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream">stream is open after calling this function</param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        bool TryWrite(Stream stream, FileInfo fileInfo);
    }
}