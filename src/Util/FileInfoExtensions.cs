namespace FSync.Util
{
    using System.IO;
    using System.Threading;

    internal static class FileInfoExtensions
    {
        public static FileStream OpenReadSafe(this FileInfo fileInfo, int maximumAttempts = 100)
        {
            var attempts = 0;

            while (true)
            {
                try
                {
                    return fileInfo.OpenRead();
                }
                catch (IOException ex) when (ex.HResult == -2147024864 && attempts++ < maximumAttempts)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}