namespace TeisterMask.DataProcessor
{
    using Data;
    using Microsoft.VisualBasic;
    using Newtonsoft.Json;
    using System.Globalization;
    using System.Text;
    using System.Xml.Serialization;
    using TeisterMask.Data.Models.Enums;
    using TeisterMask.DataProcessor.ExportDto;

    public class Serializer
    {
        public static string ExportProjectWithTheirTasks(TeisterMaskContext context)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExportProjectsDTO[]), new XmlRootAttribute("Projects"));

            StringBuilder sb = new StringBuilder();

            using var writer = new StringWriter(sb);

            var xns = new XmlSerializerNamespaces();
            xns.Add(string.Empty, string.Empty);

            var projectsAndTasks = context.Projects
                .Where(p => p.Tasks.Any())
                .Select(p => new ExportProjectsDTO
                {
                    TasksCount = p.Tasks.Count,
                    ProjectName = p.Name,
                    HasEndDate = p.DueDate != null ? "Yes" : "No",
                    Tasks = p.Tasks
                    .Select(t => new ExportProjectsTasksDTO
                    {
                        Name = t.Name,
                        Label = t.LabelType.ToString(),
                    })
                    .OrderBy(p => p.Name)
                    .ToArray()
                })
                .OrderByDescending(c => c.TasksCount)
                .ThenBy(p => p.ProjectName)
                .ToArray();

            serializer.Serialize(writer, projectsAndTasks, xns);
            writer.Close();

            return sb.ToString();
        }

        public static string ExportMostBusiestEmployees(TeisterMaskContext context, DateTime date)
        {
            var employeesAndTasks = context.Employees
                .Where(s => s.EmployeesTasks.Any(bgs => bgs.Task.OpenDate >= date))
                .OrderByDescending(s => s.EmployeesTasks.Count)
                .ThenBy(s => s.Username)
                .Select(s => new
                {
                    Username = s.Username,
                    Tasks = s.EmployeesTasks
                    .Where(bgs => bgs.Task.OpenDate >= date)
                    .OrderByDescending(bg => bg.Task.DueDate)
                    .ThenBy(bg => bg.Task.Name)
                    .Select(bg => new
                    {
                        TaskName = bg.Task.Name,
                        OpenDate = bg.Task.OpenDate.ToString("d", CultureInfo.InvariantCulture),
                        DueDate = bg.Task.DueDate.ToString("d", CultureInfo.InvariantCulture),
                        LabelType = bg.Task.LabelType.ToString(),
                        ExecutionType = bg.Task.ExecutionType.ToString(),
                    })
                    .ToArray()
                })
                .Take(10)
                .ToArray();

            return JsonConvert.SerializeObject(employeesAndTasks, Formatting.Indented);
        }
    }
}