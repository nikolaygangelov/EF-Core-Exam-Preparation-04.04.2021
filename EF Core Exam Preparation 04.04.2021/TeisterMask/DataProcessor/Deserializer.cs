// ReSharper disable InconsistentNaming

namespace TeisterMask.DataProcessor
{
    using System.ComponentModel.DataAnnotations;
    using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

    using Data;
    using System.Text;
    using System.Xml.Serialization;
    using TeisterMask.DataProcessor.ImportDto;
    using TeisterMask.Data.Models;
    using System.Globalization;
    using TeisterMask.Data.Models.Enums;
    using Newtonsoft.Json;

    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";

        private const string SuccessfullyImportedProject
            = "Successfully imported project - {0} with {1} tasks.";

        private const string SuccessfullyImportedEmployee
            = "Successfully imported employee - {0} with {1} tasks.";

        public static string ImportProjects(TeisterMaskContext context, string xmlString)
        {
            var serializer = new XmlSerializer(typeof(ImportProjectsDTO[]), new XmlRootAttribute("Projects"));
            using StringReader inputReader = new StringReader(xmlString);
            var projectsArrayDTOs = (ImportProjectsDTO[])serializer.Deserialize(inputReader);

            StringBuilder sb = new StringBuilder();
            List<Project> projectsXML = new List<Project>();

            foreach (ImportProjectsDTO projectDTO in projectsArrayDTOs)
            {

                if (!IsValid(projectDTO))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                DateTime openDate;
                if (!DateTime.TryParseExact(projectDTO.OpenDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out openDate))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                DateTime? dueDate = null;
                if (!String.IsNullOrWhiteSpace(projectDTO.DueDate))
                {
                    DateTime dueDate2;
                    if (!DateTime.TryParseExact(projectDTO.DueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dueDate2))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    dueDate = dueDate2;
                }

                Project projectToAdd = new Project
                {
                    Name = projectDTO.Name,
                    OpenDate = openDate,
                    DueDate = dueDate
                };

                foreach (var task in projectDTO.Tasks)
                {
                    if (!IsValid(task))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    DateTime taskOpenDate;
                    DateTime taskDueDate;
                    if (!DateTime.TryParseExact(task.OpenDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out taskOpenDate)
                        || !DateTime.TryParseExact(task.DueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out taskDueDate))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    if (taskOpenDate < openDate)
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    if (dueDate.HasValue && taskDueDate > dueDate.Value)
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    projectToAdd.Tasks.Add(new Task()
                    {
                        Name = task.Name,
                        OpenDate = taskOpenDate,
                        DueDate = taskDueDate,
                        ExecutionType = (ExecutionType)task.ExecutionType,
                        LabelType = (LabelType)task.LabelType
                    });
                }

                projectsXML.Add(projectToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedProject, projectToAdd.Name,
                    projectToAdd.Tasks.Count));
            }

            context.Projects.AddRange(projectsXML);

            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        public static string ImportEmployees(TeisterMaskContext context, string jsonString)
        {
            var employeesArray = JsonConvert.DeserializeObject<ImportEmployeesDTO[]>(jsonString);

            StringBuilder sb = new StringBuilder();
            List<Employee> employeesList = new List<Employee>();

            var existingTasksIds = context.Tasks
                .Select(e => e.Id)
                .ToArray();

            foreach (ImportEmployeesDTO employeeDTO in employeesArray)
            {

                if (!IsValid(employeeDTO))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                Employee employeeToAdd = new Employee()
                {
                    Username = employeeDTO.Username,
                    Email = employeeDTO.Email,
                    Phone = employeeDTO.Phone
                };



                foreach (int taskId in employeeDTO.Tasks.Distinct())
                {
                    if (!existingTasksIds.Contains(taskId))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    employeeToAdd.EmployeesTasks.Add(new EmployeeTask()
                    {
                        //Employee = employeeToAdd,// !!!!!!!!!!!
                        TaskId = taskId
                    });

                }

                employeesList.Add(employeeToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedEmployee, employeeToAdd.Username, employeeToAdd.EmployeesTasks.Count));
            }

            context.Employees.AddRange(employeesList);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}