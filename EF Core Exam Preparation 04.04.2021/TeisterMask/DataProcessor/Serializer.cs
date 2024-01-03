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
            //using Data Transfer Object Class to map it with projects
            XmlSerializer serializer = new XmlSerializer(typeof(ExportProjectsDTO[]), new XmlRootAttribute("Projects"));

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            //"using" automatically closes opened connections
            using var writer = new StringWriter(sb);

            var xns = new XmlSerializerNamespaces();

            //one way to display empty namespace in resulted file
            xns.Add(string.Empty, string.Empty);

            var projectsAndTasks = context.Projects
                .Where(p => p.Tasks.Any())
                .Select(p => new ExportProjectsDTO
                {
                    //using identical properties in order to map successfully
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

            //Serialize method needs file, TextReader object and namespace to convert/map
            serializer.Serialize(writer, projectsAndTasks, xns);

            //explicitly closing connection in terms of reaching edge cases
            writer.Close();

            //using TrimEnd() to get rid of white spaces
            return sb.ToString().TrimEnd();
        }

        public static string ExportMostBusiestEmployees(TeisterMaskContext context, DateTime date)
        {
            //turning needed info about employees into a collection using anonymous object
            //using less data
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
                        OpenDate = bg.Task.OpenDate.ToString("d", CultureInfo.InvariantCulture), //using culture-independent format
                        DueDate = bg.Task.DueDate.ToString("d", CultureInfo.InvariantCulture), //using culture-independent format
                        LabelType = bg.Task.LabelType.ToString(),
                        ExecutionType = bg.Task.ExecutionType.ToString(),
                    })
                    .ToArray()
                })
                .Take(10)
                .ToArray();

            //Serialize method needs object to convert/map
	        //adding Formatting for better reading 
            return JsonConvert.SerializeObject(employeesAndTasks, Formatting.Indented);
        }
    }
}
