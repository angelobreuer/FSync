namespace FSync
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using FSync.Util;

    public sealed class FileCopyQueue : IDisposable
    {
        private readonly ConcurrentQueue<KeyValuePair<FileInfo, FileInfo>> _copyQueue;
        private bool _disposed;
        private int _workerCount;
        private int _workerId;

        public FileCopyQueue()
        {
            _copyQueue = new ConcurrentQueue<KeyValuePair<FileInfo, FileInfo>>();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Drain();
        }

        public void Drain() => SpinWait.SpinUntil(() => _workerCount == 0 && _copyQueue.IsEmpty);

        public void Enqueue(FileInfo sourceFileInfo, FileInfo targetFileInfo)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FileCopyQueue));
            }

            if (sourceFileInfo is null)
            {
                throw new ArgumentNullException(nameof(sourceFileInfo));
            }

            if (targetFileInfo is null)
            {
                throw new ArgumentNullException(nameof(targetFileInfo));
            }

            _copyQueue.Enqueue(KeyValuePair.Create(sourceFileInfo, targetFileInfo));

            if (_workerCount < Environment.ProcessorCount)
            {
                new Thread(RunCopyWorker).Start();
            }
        }

        private void RunCopyWorker()
        {
            if (_copyQueue.IsEmpty)
            {
                return;
            }

            var workerName = "Worker-" + _workerId++;
            Thread.CurrentThread.Name = workerName;
            Interlocked.Increment(ref _workerCount);

            try
            {
                var buffer = new byte[1024 * 1024 * 4]; // 4 MiB
                var misses = 0;
                int length;

                while (misses++ < 100 && !_disposed)
                {
                    while (_copyQueue.TryDequeue(out var streamPair))
                    {
                        misses = 0;

                        var (sourceFileInfo, targetFileInfo) = streamPair;
                        using var sourceFileStream = sourceFileInfo.OpenReadSafe();
                        using var targetFileStream = targetFileInfo.Create();

                        try
                        {
                            while ((length = sourceFileStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                targetFileStream.Write(buffer, 0, length);
                            }
                        }
                        catch (Exception)
                        {
                            Console.Error.WriteLine("[{WorkerName}] Failed to copy {SourceFile} to {TargetFile}...", workerName, streamPair.Key.Name, streamPair.Value.Name);
                        }
                    }

                    Thread.Sleep(100);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _workerCount);
            }
        }
    }
}
