using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        IServiceProvider services) : BackgroundService
    {
        // Thread-safe queue so FileSystemWatcher events don't block the OS callback thread
        private readonly ConcurrentQueue<string> _pendingFiles = new();
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
            var folders = configuration.GetSection("CameraFolders").Get<string[]>() ?? [];
            foreach (var folder in folders)
                StartWatcher(folder);

            // Process any *.lpr files that existed before the service started
            foreach (var folder in folders)
                EnqueueExisting(folder);

            logger.LogInformation("ANPR Worker started. Watching {Count} folder(s).", folders.Length);

            while (!stoppingToken.IsCancellationRequested)
            {
                while (_pendingFiles.TryDequeue(out var filePath))
                    await ProcessFileAsync(filePath);

                await Task.Delay(500, stoppingToken);
            }

            foreach (var w in _watchers) w.Dispose();
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

            watcher.Created += (_, e) => _pendingFiles.Enqueue(e.FullPath);
            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);

            logger.LogInformation("Watching: {Folder}", folder);
        }

        private void EnqueueExisting(string folder)
        {
            if (!Directory.Exists(folder)) return;
            foreach (var file in Directory.GetFiles(folder, "*.lpr"))
                _pendingFiles.Enqueue(file);
        }

        private async Task ProcessFileAsync(string filePath)
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process {File}", filePath);
            }
        }
    }
}
