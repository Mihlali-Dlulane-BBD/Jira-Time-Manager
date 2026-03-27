using Jira_Time_Manager.Core.Interface;
using Jira_Time_Manager.Core.Models;
using System.Globalization;

namespace Jira_Time_Manager.Core.Services.Parsers
{
    public class CsvWorkLogParser : IWorkLogParser
    {
        private readonly ILogger<CsvWorkLogParser> _logger;

        public CsvWorkLogParser(ILogger<CsvWorkLogParser> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<WorkLogImportDto>> ParseAsync(Stream fileStream)
        {
            var result = new List<WorkLogImportDto>();
            using var reader = new StreamReader(fileStream);

            string currentClient = string.Empty;
            string currentProject = string.Empty;

         
            int lineNumber = 0;
            string? line;

            _logger.LogInformation("--- Starting CSV Parse ---");

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var cols = line.Split(',');
                var firstCol = cols[0].Trim();

                if (IsIgnorableRow(firstCol))
                    continue;

                if (TryUpdateContext(firstCol, ref currentClient, ref currentProject))
                {
                    _logger.LogInformation($"Line {lineNumber}: Context updated -> Client: '{currentClient}', Project: '{currentProject}'");
                    continue;
                }

                
                var parsedLog = TryParseDataRow(cols, currentClient, currentProject, lineNumber);

                if (parsedLog != null)
                {
                    result.Add(parsedLog);
                }
            }

            _logger.LogInformation($"--- Finished CSV Parse. Found {result.Count} valid logs. ---");
            return result;
        }

        private bool IsIgnorableRow(string firstCol)
        {
            return firstCol.StartsWith("Staff Report") ||
                   firstCol.StartsWith("Date From:") ||
                   firstCol.StartsWith("Date To:") ||
                   firstCol == "DESCRIPTION" ||
                   firstCol.StartsWith("User:") ||
                   firstCol.StartsWith("Project Total:") ||
                   firstCol.StartsWith("Client Total:") ||
                   firstCol.StartsWith("User Total:");
        }

        private bool TryUpdateContext(string firstCol, ref string currentClient, ref string currentProject)
        {
            if (firstCol.StartsWith("Client:"))
            {
                currentClient = firstCol.Replace("Client:", "").Trim();
                return true;
            }

            if (firstCol.StartsWith("Project:"))
            {
                currentProject = firstCol.Replace("Project:", "").Trim();
                return true;
            }

            return false;
        }

        private WorkLogImportDto? TryParseDataRow(string[] cols, string currentClient, string currentProject, int lineNumber)
        {
            if (cols.Length < 8)
            {
                _logger.LogWarning($"Line {lineNumber}: REJECTED. Not enough columns. Found {cols.Length}.");
                return null;
            }

            if (string.IsNullOrEmpty(currentProject))
            {
                _logger.LogWarning($"Line {lineNumber}: REJECTED. No active Project context.");
                return null;
            }

            string description = cols[0].Trim();
            string staffNo = cols[1].Trim();
            string staffName = cols[2].Trim();
            string workCode = cols[3].Trim();

            string hoursStr = cols[^1].Trim();
            string dateStr = cols[^2].Trim();
            string refNo = cols[^3].Trim();

            string comment = string.Join(",", cols[4..^3]).Trim();

            comment = comment.Trim('"');
          
            if (!DateOnly.TryParseExact(dateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly logDate))
            {
               
                if (!DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out logDate))
                {
                    _logger.LogWarning($"Line {lineNumber}: REJECTED. Invalid Date -> '{dateStr}'");
                    return null;
                }
            }

      
            if (!decimal.TryParse(hoursStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal hours))
            {
                _logger.LogWarning($"Line {lineNumber}: REJECTED. Invalid Hours -> '{hoursStr}'");
                return null;
            }

         
            return new WorkLogImportDto
            {
                Description = description,
                StaffNo = staffNo,
                StaffName = staffName,
                WorkCode = workCode,
                Comment = comment,
                ReferenceNumber = refNo,
                Date = logDate,
                Hours = hours,
                ClientName = currentClient,
                ProjectName = currentProject
            };
        }
    }
}
