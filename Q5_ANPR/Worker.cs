using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Q5_ANPR.Data;
using Q5_ANPR.Services;

namespace Q5_ANPR
{
    public class Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IServiceProvider services) : BackgroundService
    {
        private const int MaxReadAttempts = 5;
        private static readonly TimeSpan ReadRetryDelay = TimeSpan.FromMilliseconds(200);

        // Thread-safe queue so FileSystemWatcher events don't block the OS callback thread
        private readonly ConcurrentQueue<string> _pendingFiles = new();
        private readonly ConcurrentDictionary<string, byte> _scheduledFiles = new();
        private readonly List<FileSystemWatcher> _watchers = [];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Ensure DB schema exists
            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AnprDbContext>();
                await db.Database.EnsureCreatedAsync(stoppingToken);
            }

            // Watch each configured camera folder
            var configuredFolders = configuration.GetSection("CameraFolders").Get<string[]>() ?? [];
            var folders = new string[configuredFolders.Length];

            for (int i = 0; i < configuredFolders.Length; i++)
                folders[i] = ResolveFolderPath(configuredFolders[i]);

            if (folders.Length == 0)
                logger.LogWarning("No camera folders configured.");

            foreach (var folder in folders)
                StartWatcher(folder);

            // Process any *.lpr files that existed before the service started
            foreach (var folder in folders)
                EnqueueExisting(folder);

            logger.LogInformation("ANPR Worker started. Watching {Count} folder(s).", folders.Length);

            while (!stoppingToken.IsCancellationRequested)
            {
                while (_pendingFiles.TryDequeue(out var filePath))
                {
                    try
                    {
                        await ProcessFileAsync(filePath, stoppingToken);
                    }
                    finally
                    {
                        _scheduledFiles.TryRemove(filePath, out _);
                    }
                }

                await Task.Delay(500, stoppingToken);
            }

            foreach (var w in _watchers) w.Dispose();
        }

        private string ResolveFolderPath(string folder)
        {
            if (Path.IsPathRooted(folder))
                return Path.GetFullPath(folder);

            return Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, folder));
        }

        private void StartWatcher(string folder)
        {
            if (!Directory.Exists(folder))
            {
                logger.LogWarning("Camera folder not found, skipping: {Folder}", folder);
                return;
            }

            var watcher = new FileSystemWatcher(folder, "*.lpr")
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            watcher.Created += (_, e) => QueueFile(e.FullPath);
            watcher.Changed += (_, e) => QueueFile(e.FullPath);
            watcher.Renamed += (_, e) => QueueFile(e.FullPath);
            watcher.Error += (_, e) =>
                logger.LogError(e.GetException(), "File watcher error in {Folder}", folder);
            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);

            logger.LogInformation("Watching: {Folder}", folder);
        }

        private void EnqueueExisting(string folder)
        {
            if (!Directory.Exists(folder)) return;
            foreach (var file in Directory.GetFiles(folder, "*.lpr"))
                QueueFile(file);
        }

        private void QueueFile(string filePath)
        {
            var normalizedPath = Path.GetFullPath(filePath);

            if (_scheduledFiles.TryAdd(normalizedPath, 0))
                _pendingFiles.Enqueue(normalizedPath);
        }

        private async Task<string> ReadFileWithRetriesAsync(string filePath, CancellationToken stoppingToken)
        {
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return await File.ReadAllTextAsync(filePath, stoppingToken);
                }
                catch (IOException ex) when (attempt < MaxReadAttempts)
                {
                    logger.LogDebug(ex,
                        "File still being written, retrying {File} ({Attempt}/{MaxAttempts})",
                        filePath, attempt, MaxReadAttempts);
                }
                catch (UnauthorizedAccessException ex) when (attempt < MaxReadAttempts)
                {
                    logger.LogDebug(ex,
                        "File not accessible yet, retrying {File} ({Attempt}/{MaxAttempts})",
                        filePath, attempt, MaxReadAttempts);
                }

                await Task.Delay(ReadRetryDelay, stoppingToken);
            }
        }

        private async Task ProcessFileAsync(string filePath, CancellationToken stoppingToken)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    logger.LogDebug("Skipping missing file: {File}", filePath);
                    return;
                }

                var content = await ReadFileWithRetriesAsync(filePath, stoppingToken);
                var read = LprParser.Parse(filePath, content);

                using var scope = services.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<PlateReadRepository>();

                bool saved = await repo.TrySaveAsync(read);

                if (saved)
                    logger.LogInformation("Saved plate {Plate} from {Camera} ({File})",
                        read.RegNumber, read.CameraName, Path.GetFileName(filePath));
                else
                    logger.LogDebug("Skipped already-processed file: {File}", filePath);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (FileNotFoundException)
            {
                logger.LogDebug("Skipping file that no longer exists: {File}", filePath);
            }
            catch (DirectoryNotFoundException)
            {
                logger.LogDebug("Skipping file from missing directory: {File}", filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process {File}", filePath);
            }
        }
    }
}
