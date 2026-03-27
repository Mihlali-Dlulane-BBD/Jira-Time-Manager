using ClosedXML.Excel;
using Jira_Time_Manager.Core.Data;
using Jira_Time_Manager.Core.Interface;
using Jira_Time_Manager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Jira_Time_Manager.Core.Services
{
    public class ExcelExportService : IWorkLogExportService
    {
        private readonly IDbContextFactory<JiraTimeManagerDbContext> _dbFactory;

        public ExcelExportService(IDbContextFactory<JiraTimeManagerDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<byte[]> GenerateExportAsync(int year, int month, int? specificEmployeeId = null)
        {
            var logsToExport = await FetchLogsFromDatabaseAsync(year, month, specificEmployeeId);

            using var workbook = BuildExcelWorkbook(logsToExport, year, month);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }



        private async Task<List<WorkLog>> FetchLogsFromDatabaseAsync(int year, int month, int? specificEmployeeId)
        {
            using var context = await _dbFactory.CreateDbContextAsync();

            var query = context.WorkLogs
                .Include(w => w.Employee)
                .Include(w => w.Project)
                .Where(w => w.LogDate.Year == year && w.LogDate.Month == month);

            if (specificEmployeeId.HasValue && specificEmployeeId.Value > 0)
            {
                query = query.Where(w => w.EmployeeId == specificEmployeeId.Value);
            }

            return await query
              .OrderBy(w => w.Employee.FirstName)
              .ThenBy(w => w.Employee.LastName)
              .ThenBy(w => w.LogDate)
              .ToListAsync();

        }

        private XLWorkbook BuildExcelWorkbook(List<WorkLog> logs, int year, int month)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"Timesheet_{year}_{month:D2}");

            WriteHeaders(worksheet);
            WriteDataRows(worksheet, logs);

            worksheet.Columns().AdjustToContents();

            return workbook;
        }

        private void WriteHeaders(IXLWorksheet worksheet)
        {
            var headers = new[] { "DESCRIPTION", "STAFF NO.", "STAFF NAME", "WORK CODE", "COMMENT", "REF NO.", "DATE", "HOURS", "APPROVED" };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var headerRow = worksheet.Range(1, 1, 1, headers.Length);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        private void WriteDataRows(IXLWorksheet worksheet, List<WorkLog> logs)
        {
            int currentRow = 2;

            var groupedLogs = logs.GroupBy(l => l.Employee);

            foreach (var group in groupedLogs)
            {
                var employee = group.Key;


                if (employee != null)
                {

                    var totalHours = group.Sum(l => l.Hours);


                    var headerRange = worksheet.Range(currentRow, 1, currentRow, 9);
                    headerRange.Merge();

                    headerRange.Value = $"👤 EMPLOYEE: {employee.FirstName} {employee.LastName}   |   STAFF NO: {employee.StaffNo}   |   TOTAL HOURS: {totalHours:0.0}";


                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Fill.BackgroundColor = XLColor.SlateGray;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    currentRow++;
                }


                foreach (var log in group)
                {
                    worksheet.Cell(currentRow, 1).Value = log.Description;
                    worksheet.Cell(currentRow, 2).Value = log.Employee?.StaffNo ?? "";
                    worksheet.Cell(currentRow, 3).Value = $"{log.Employee?.FirstName} {log.Employee?.LastName}".Trim();
                    worksheet.Cell(currentRow, 4).Value = log.WorkCode;
                    worksheet.Cell(currentRow, 5).Value = log.Comment;
                    worksheet.Cell(currentRow, 6).Value = log.ReferenceNumber;
                    worksheet.Cell(currentRow, 7).Value = log.LogDate.ToString("yyyy/MM/dd");
                    worksheet.Cell(currentRow, 8).Value = log.Hours;
                    worksheet.Cell(currentRow, 9).Value = log.IsApproved ? "Yes" : "No";

                    currentRow++;
                }


                currentRow++;
            }
        }
    }
}
