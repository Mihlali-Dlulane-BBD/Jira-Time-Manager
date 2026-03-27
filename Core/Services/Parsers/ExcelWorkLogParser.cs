using ClosedXML.Excel;
using Jira_Time_Manager.Core.Interface;
using Jira_Time_Manager.Core.Models;
using System.Globalization;

namespace Jira_Time_Manager.Core.Services.Parsers
{
    public class ExcelWorkLogParser : IWorkLogParser
    {
        private readonly ILogger<ExcelWorkLogParser> _logger;

        public ExcelWorkLogParser(ILogger<ExcelWorkLogParser> logger)
        {
            _logger = logger;
        }

        public Task<IEnumerable<WorkLogImportDto>> ParseAsync(Stream fileStream)
        {
            var result = new List<WorkLogImportDto>();

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheet(1);

                string currentClient = string.Empty;
                string currentProject = string.Empty;

                _logger.LogInformation("--- Starting Excel Parse ---");
               
                foreach (var row in worksheet.RowsUsed())
                {
                    int lineNumber = row.RowNumber();

                    string firstCol = row.Cell(1).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(firstCol) || IsIgnorableRow(firstCol))
                        continue;

                    if (TryUpdateContext(firstCol, ref currentClient, ref currentProject))
                    {
                        _logger.LogInformation($"Line {lineNumber}: Context updated -> Client: '{currentClient}', Project: '{currentProject}'");
                        continue;
                    }

                   
                    var parsedLog = TryParseDataRow(row, currentClient, currentProject, lineNumber);
                    if (parsedLog != null)
                    {
                        result.Add(parsedLog);
                    }
                }

                _logger.LogInformation($"--- Finished Excel Parse. Found {result.Count} valid logs. ---");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to read Excel file: {ex.Message}");
            }

            return Task.FromResult(result.AsEnumerable());
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

        private WorkLogImportDto? TryParseDataRow(IXLRow row, string currentClient, string currentProject, int lineNumber)
        {
            if (string.IsNullOrEmpty(currentProject))
            {
                _logger.LogWarning($"Line {lineNumber}: REJECTED. No active Project context.");
                return null;
            }

         
            string description = row.Cell(1).GetString().Trim();
            string staffNo = row.Cell(2).GetString().Trim();
            string staffName = row.Cell(3).GetString().Trim();
            string workCode = row.Cell(4).GetString().Trim();
            string comment = row.Cell(5).GetString().Trim();
            string refNo = row.Cell(6).GetString().Trim();

            if (string.IsNullOrEmpty(description))
            {
                _logger.LogWarning($"Line {lineNumber}: REJECTED. Missing description.");
                return null;
            }

        
            var dateCell = row.Cell(7);
            var hoursCell = row.Cell(8);

          
            DateOnly logDate;
            if (dateCell.TryGetValue<DateTime>(out DateTime rawDate))
            {
                logDate = DateOnly.FromDateTime(rawDate);
            }
            else
            {
                string dateStr = dateCell.GetString().Trim();
                if (!DateOnly.TryParseExact(dateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out logDate))
                {
                    if (!DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, out logDate))
                    {
                        _logger.LogWarning($"Line {lineNumber}: REJECTED. Invalid Date -> '{dateCell.Value}'");
                        return null;
                    }
                }
            }

       
            decimal hours;
            if (hoursCell.TryGetValue<double>(out double rawHours))
            {
                hours = (decimal)rawHours;
            }
            else
            {
                string fallbackStr = hoursCell.GetString().Trim().Replace(',', '.');
                if (!decimal.TryParse(fallbackStr, NumberStyles.Any, CultureInfo.InvariantCulture, out hours))
                {
                    _logger.LogWarning($"Line {lineNumber}: REJECTED. Invalid Hours -> '{hoursCell.Value}'");
                    return null;
                }
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
