using Jira_Time_Manager.Core.Interface;

namespace Jira_Time_Manager.Core.Services.Background
{
    public class FolderScannerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<FolderScannerService> _logger;

        public FolderScannerService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<FolderScannerService> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string dropFolder = _config["ImportSettings:DropFolder"] ?? @"C:\TimeManager\DropFolder";
            string processedFolder = _config["ImportSettings:ProcessedFolder"] ?? @"C:\TimeManager\Processed";

            Directory.CreateDirectory(dropFolder);
            Directory.CreateDirectory(processedFolder);

            _logger.LogInformation($"Folder Scanner Started. Watching: {dropFolder}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var files = Directory.GetFiles(dropFolder, "*.xlsx");

                    foreach (var file in files)
                    {
                        _logger.LogInformation($"Found new file: {Path.GetFileName(file)}");
                        await ProcessFileAsync(file, processedFolder);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Scanner error: {ex.Message}");
                }

                int delayTimeBeforeNextScanInMiliseconds = 5000;
                await Task.Delay(delayTimeBeforeNextScanInMiliseconds, stoppingToken);
            }
        }

        private async Task ProcessFileAsync(string filePath, string processedFolder)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var parser = scope.ServiceProvider.GetRequiredService<IWorkLogParser>();
                var importer = scope.ServiceProvider.GetRequiredService<IDataImportService>();
                   
                string fileName = Path.GetFileName(filePath);
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
         
                    var rawLogs = await parser.ParseAsync(stream);

                 
                    if (rawLogs.Any())
                    {
                        await importer.ImportWorkLogsAsync(rawLogs,fileName);
                        _logger.LogInformation($"Successfully imported {rawLogs.Count()} logs from {Path.GetFileName(filePath)}");
                    }
                }

                string destPath = Path.Combine(processedFolder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{fileName}");
                File.Move(filePath, destPath);
            }
            catch (IOException)
            {
              
                _logger.LogWarning($"File {Path.GetFileName(filePath)} is locked. Will retry next cycle.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to process {Path.GetFileName(filePath)}: {ex.Message}");

            }
        }
    }
}
