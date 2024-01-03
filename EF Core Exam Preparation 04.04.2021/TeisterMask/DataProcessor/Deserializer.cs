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
            //using Data Transfer Object Class to map it with Projects
            var serializer = new XmlSerializer(typeof(ImportProjectsDTO[]), new XmlRootAttribute("Projects"));

            //Deserialize method needs TextReader object to convert/map 
            using StringReader inputReader = new StringReader(xmlString);
            var projectsArrayDTOs = (ImportProjectsDTO[])serializer.Deserialize(inputReader);

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            //creating List where all valid projects can be kept
            List<Project> projectsXML = new List<Project>();

            foreach (ImportProjectsDTO projectDTO in projectsArrayDTOs)
            {
                //validating info for project from data
                if (!IsValid(projectDTO))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                //validating dates
                DateTime openDate;
                if (!DateTime.TryParseExact(projectDTO.OpenDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out openDate)) //culture-independent format
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

                //creating a valid project
                Project projectToAdd = new Project
                {
                    //using identical properties in order to map successfully
                    Name = projectDTO.Name,
                    OpenDate = openDate,
                    DueDate = dueDate
                };

                foreach (var task in projectDTO.Tasks)
                {
                    //validating tasks
                    if (!IsValid(task))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    //validating task dates
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

                    //adding valid task
                    projectToAdd.Tasks.Add(new Task()
                    {
                        //using identical properties in order to map successfully
                        Name = task.Name,
                        OpenDate = taskOpenDate,
                        DueDate = taskDueDate,
                        ExecutionType = (ExecutionType)task.ExecutionType, //casting from "int"
                        LabelType = (LabelType)task.LabelType //casting from "int"
                    });
                }

                projectsXML.Add(projectToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedProject, projectToAdd.Name,
                    projectToAdd.Tasks.Count));
            }

            context.Projects.AddRange(projectsXML);

            //actual importing info from data
            context.SaveChanges();

            //using TrimEnd() to get rid of white spaces
            return sb.ToString().TrimEnd();
        }

        public static string ImportEmployees(TeisterMaskContext context, string jsonString)
        {
            //using Data Transfer Object Class to map it with employees
            var employeesArray = JsonConvert.DeserializeObject<ImportEmployeesDTO[]>(jsonString);

            //using StringBuilder to gather all info in one string
            StringBuilder sb = new StringBuilder();

            //creating List where all valid emoloyees can be kept
            List<Employee> employeesList = new List<Employee>();

            //taking unique tasks
            var existingTasksIds = context.Tasks
                .Select(e => e.Id)
                .ToArray();

            foreach (ImportEmployeesDTO employeeDTO in employeesArray)
            {
                //validating info for employee from data
                if (!IsValid(employeeDTO))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                //creating a valid employee
                Employee employeeToAdd = new Employee()
                {
                    //using identical properties in order to map successfully
                    Username = employeeDTO.Username,
                    Email = employeeDTO.Email,
                    Phone = employeeDTO.Phone
                };



                foreach (int taskId in employeeDTO.Tasks.Distinct())
                {
                    //validating unique employees
                    if (!existingTasksIds.Contains(taskId))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }

                    //adding valid EmployeeTask
                    employeeToAdd.EmployeesTasks.Add(new EmployeeTask()
                    {
                        //Employee = employeeToAdd,
                        TaskId = taskId
                    });

                }

                employeesList.Add(employeeToAdd);
                sb.AppendLine(string.Format(SuccessfullyImportedEmployee, employeeToAdd.Username, employeeToAdd.EmployeesTasks.Count));
            }

            context.Employees.AddRange(employeesList);

            //actual importing info from data
            context.SaveChanges();

            //using TrimEnd() to get rid of white spaces
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
