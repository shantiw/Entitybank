using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Entities
{
    public class University
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public virtual ICollection<College> Colleges { get; set; }
    }

    public class College
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public virtual University University { get; set; }

        [Required]
        public virtual ICollection<Department> Departments { get; set; }

        public virtual Location Location { get; set; }
    }

    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public virtual College College { get; set; }

        [Required]
        public virtual ICollection<Specialty> Specialties { get; set; }
    }

    public class Specialty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public virtual Department Department { get; set; }
    }

    public class Location
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
    }

    public class Example
    {
        public IEnumerable<University> Universities;

        public Example()
        {
            List<University> universities = new List<University>();
            University university = new University() { Id = 1, Name = "Sydney" };
            university.Colleges = new List<College>();
            universities.Add(university);
            university = new University() { Id = 2, Name = "Queensland" };            
            universities.Add(university);
            Universities = universities;
        }
    }
}
