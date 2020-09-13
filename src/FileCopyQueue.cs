namespace FSync
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using FSync.Util;
    using Microsoft.Extensions.Logging;

    public sealed class FileCopyQueue : IDisposable
    {
        private readonly ConcurrentQueue<KeyValuePair<FileInfo, FileInfo>> _copyQueue;
        private readonly ILogger<FileCopyQueue> _logger;
        private bool _disposed;
        private int _workerCount;
        private int _workerId;

        public FileCopyQueue(ILogger<FileCopyQueue> logger)
        {
            _copyQueue = new ConcurrentQueue<KeyValuePair<FileInfo, FileInfo>>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Drain();
            _disposed = true;
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

            _logger.LogDebug("Started worker {Name}.", workerName);

            try
            {
                var buffer = new byte[1024 * 1024 * 4]; // 4 MiB
                int length;

                while (_copyQueue.TryDequeue(out var streamPair))
                {
                    var (sourceFileInfo, targetFileInfo) = streamPair;

                    _logger.LogDebug("[{WorkerName}] Copying {SourceFile} to {TargetFile}...",
                        workerName, sourceFileInfo.FullName, targetFileInfo.FullName);

                    try
                    {
                        using var sourceFileStream = sourceFileInfo.OpenReadSafe();
                        using var targetFileStream = targetFileInfo.Create();

                        while ((length = sourceFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            targetFileStream.Write(buffer, 0, length);
                        }
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning("[{WorkerName}] Failed to copy {SourceFile} to {TargetFile}...",
                            workerName, sourceFileInfo.FullName, targetFileInfo.FullName);
                    }
                }
            }
            finally
            {
                _logger.LogDebug("Stopped worker {Name}.", workerName);

                Interlocked.Decrement(ref _workerCount);
            }
        }
    }
}
