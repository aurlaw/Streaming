namespace DataGenetator;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    
    public User(Guid id, string name, string email, bool isActive)
    {
        Id = id;
        Name = name;
        Email = email;
        IsActive = isActive;
    }
}