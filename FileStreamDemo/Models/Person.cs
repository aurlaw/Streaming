namespace FileStreamDemo.Models;

public class Person
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly BirthDate { get; set; }
    public int Age => DateTime.Now.Year - BirthDate.Year;    
}