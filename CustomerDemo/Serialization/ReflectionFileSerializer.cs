using System.Reflection;
using System.Text;
using CustomerDemo.Attributes;
using CustomerDemo.Models;

namespace CustomerDemo.Serialization;

public class ReflectionFileSerializer
{
    public string Serialize(List<Customer> customers)
    {
        var sb = new StringBuilder();

        foreach (var customer in customers)
        {
            // Write customer line
            sb.AppendLine(SerializeObject(customer, "C"));

            // Write orders for this customer
            foreach (var order in customer.Orders)
            {
                sb.AppendLine(SerializeObject(order, "O"));

                // Write order lines for this order
                foreach (var orderLine in order.OrderLines)
                {
                    sb.AppendLine(SerializeObject(orderLine, "L"));
                }
            }
        }

        return sb.ToString();
    }

    private string SerializeObject<T>(T obj, string recordType)
    {
        var sb = new StringBuilder();
        sb.Append(recordType);

        // Get all properties with ReportItem attribute
        var properties = typeof(T)
            .GetProperties()
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<ReportItemAttribute>()
            })
            .Where(x => x.Attribute != null)
            .OrderBy(x => x.Attribute!.Order)
            .ToList();

        foreach (var prop in properties)
        {
            var value = prop.Property.GetValue(obj);
            var attr = prop.Attribute!;

            var formattedValue = FormatValue(value, attr);
            sb.Append(formattedValue);
        }

        return sb.ToString();
    }

    private string FormatValue(object? value, ReportItemAttribute attr)
    {
        // Convert value to string
        string stringValue = value switch
        {
            null => string.Empty,
            int intValue => intValue.ToString(),
            decimal decimalValue => decimalValue.ToString("F2").Replace(".", ""), // Format as cents
            string strValue => strValue,
            _ => value.ToString() ?? string.Empty
        };

        // Truncate if too long
        if (stringValue.Length > attr.MaxLength)
        {
            stringValue = stringValue.Substring(0, attr.MaxLength);
        }

        // Apply padding
        if (attr.PaddingDirection == PaddingDirection.Left)
        {
            return stringValue.PadLeft(attr.MaxLength, attr.PaddingChar);
        }
        else
        {
            return stringValue.PadRight(attr.MaxLength, attr.PaddingChar);
        }
    }    
}