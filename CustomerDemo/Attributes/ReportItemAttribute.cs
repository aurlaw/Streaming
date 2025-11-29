namespace CustomerDemo.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class ReportItemAttribute : Attribute
{
    public int Order { get; set; }
    public int MaxLength { get; set; }
    public char PaddingChar { get; set; } = ' ';
    public PaddingDirection PaddingDirection { get; set; } = PaddingDirection.Right;
    
    public ReportItemAttribute(int order, int maxLength)
    {
        Order = order;
        MaxLength = maxLength;
    }

    // Full constructor - specify everything
    public ReportItemAttribute(int order, int maxLength, char paddingChar, PaddingDirection paddingDirection)
    {
        Order = order;
        MaxLength = maxLength;
        PaddingChar = paddingChar;
        PaddingDirection = paddingDirection;
    }    
}
public enum PaddingDirection
{
    Left,
    Right
}