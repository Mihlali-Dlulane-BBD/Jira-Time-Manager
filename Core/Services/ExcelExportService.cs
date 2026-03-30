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
                .ThenInclude(p => p.Client)
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


            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);


            worksheet.Cell(1, 1).Value = "Staff Report";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;



            worksheet.Cell(3, 1).Value = $"Date: {startDate:yyyy/MM/dd}";
            worksheet.Cell(4, 1).Value = $"Date: {endDate:yyyy/MM/dd}";
            worksheet.Range(3, 1, 4, 1).Style.Font.Bold = true;


            WriteReportHierarchy(worksheet, logs, 6);

            worksheet.Columns().AdjustToContents();

            return workbook;
        }

        private void WriteHeaders(IXLWorksheet worksheet, int row)
        {
            var headers = new[] { "DESCRIPTION", "STAFF NO.", "STAFF NAME", "WORK CODE", "COMMENT", "REF NO.", "DATE", "HOURS", "APPROVED" };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(row, i + 1).Value = headers[i];
            }

            var headerRow = worksheet.Range(row, 1, row, headers.Length);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRow.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }


        private void WriteReportHierarchy(IXLWorksheet worksheet, List<WorkLog> logs, int startingRow)
        {
            int currentRow = startingRow;


            var logsByEmployee = logs.GroupBy(l => l.Employee);

            foreach (var empGroup in logsByEmployee)
            {
                var employee = empGroup.Key;
                if (employee == null) continue;

                WriteHeaders(worksheet, currentRow);
                currentRow++;


                worksheet.Cell(currentRow, 1).Value = $"User: {employee.FirstName} {employee.LastName}";
                worksheet.Cell(currentRow, 2).Value = employee.StaffNo;
                worksheet.Range(currentRow, 1, currentRow, 2).Style.Font.Bold = true;
                currentRow++;


                var logsByClient = empGroup.GroupBy(l => l.Project?.Client);
                foreach (var clientGroup in logsByClient)
                {
                    var client = clientGroup.Key;
                    string clientName = client?.ClientName ?? "Unassigned Client";

                    worksheet.Cell(currentRow, 1).Value = $"Client: {clientName}";
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Style.Font.Italic = true;
                    currentRow++;


                    var logsByProject = clientGroup.GroupBy(l => l.Project);
                    foreach (var projGroup in logsByProject)
                    {
                        var project = projGroup.Key;
                        string projectName = project?.ProjectName ?? "Unassigned Project";

                        worksheet.Cell(currentRow, 1).Value = $"Project: {projectName}";
                        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                        currentRow++;


                        foreach (var log in projGroup)
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


                        var projectTotal = projGroup.Sum(l => l.Hours);
                        worksheet.Cell(currentRow, 1).Value = $"Project Total: {projectName}";
                        worksheet.Cell(currentRow, 8).Value = projectTotal; 
                        
                        var projectTotalRange = worksheet.Range(currentRow, 1, currentRow, 9);
                        projectTotalRange.Style.Font.Bold = true;
                        projectTotalRange.Style.Fill.BackgroundColor = XLColor.LightGray; 
                        currentRow++;
                    }


                    currentRow++; 

                    var clientTotal = clientGroup.Sum(l => l.Hours);
                    worksheet.Cell(currentRow, 1).Value = $"Client Total: {clientName}";
                    worksheet.Cell(currentRow, 8).Value = clientTotal;

                    var clientTotalRange = worksheet.Range(currentRow, 1, currentRow, 9);
                    clientTotalRange.Style.Font.Bold = true;
                    clientTotalRange.Style.Fill.BackgroundColor = XLColor.LightGray; 
                    currentRow++;
                }

                currentRow++;

                var userTotal = empGroup.Sum(l => l.Hours);
                worksheet.Cell(currentRow, 1).Value = $"User Total: {employee.FirstName} {employee.LastName}";
                worksheet.Cell(currentRow, 8).Value = userTotal;


                var userTotalRange = worksheet.Range(currentRow, 1, currentRow, 9);
                userTotalRange.Style.Font.Bold = true;
                userTotalRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                userTotalRange.Style.Border.TopBorder = XLBorderStyleValues.Medium;

                currentRow += 2;
            }
        }
    }
}
