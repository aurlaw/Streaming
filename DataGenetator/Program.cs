using System;
using System.IO;
using System.Text;

const int lineCount = 150_000;
const string outputFile = "/Users/michaellawrence/Documents/dev/src/github.com/aurlaw/Benchmarks/Streaming/Shared/people.txt";

var firstNames = new[]
{
    "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda",
    "William", "Barbara", "David", "Elizabeth", "Richard", "Susan", "Joseph", "Jessica",
    "Thomas", "Sarah", "Charles", "Karen", "Christopher", "Nancy", "Daniel", "Lisa",
    "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra", "Donald", "Ashley",
    "Steven", "Kimberly", "Paul", "Emily", "Andrew", "Donna", "Joshua", "Michelle",
    "Kenneth", "Dorothy", "Kevin", "Carol", "Brian", "Amanda", "George", "Melissa",
    "Edward", "Deborah", "Ronald", "Stephanie", "Timothy", "Rebecca", "Jason", "Sharon",
    "Jeffrey", "Laura", "Ryan", "Cynthia", "Jacob", "Kathleen", "Gary", "Amy",
    "Nicholas", "Shirley", "Eric", "Angela", "Jonathan", "Helen", "Stephen", "Anna"
};

var lastNames = new[]
{
    "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
    "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas",
    "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White",
    "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young",
    "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
    "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell",
    "Carter", "Roberts", "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker",
    "Cruz", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales", "Murphy",
    "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper", "Peterson", "Bailey"
};

var random = new Random(42); // Seed for reproducibility

Console.WriteLine($"Generating {lineCount:N0} lines...");

using (var writer = new StreamWriter(outputFile, false, Encoding.UTF8))
{
    for (int i = 0; i < lineCount; i++)
    {
        var firstName = firstNames[random.Next(firstNames.Length)];
        var lastName = lastNames[random.Next(lastNames.Length)];
        
        // Generate random birth date between 1940 and 2005
        int year = random.Next(1940, 2006);
        int month = random.Next(1, 13);
        int day = random.Next(1, 29); // Keep it simple to avoid invalid dates
        
        writer.WriteLine($"{firstName},{lastName},{year:D4}-{month:D2}-{day:D2}");
        
        // Progress indicator
        if ((i + 1) % 10000 == 0)
        {
            Console.WriteLine($"  Generated {i + 1:N0} lines...");
        }
    }
}
//
// var fileInfo = new FileInfo(outputFile);
// Console.WriteLine($"\nFile created: {outputFile}");
// Console.WriteLine($"Size: {fileInfo.Length / 1024:N0} KB ({fileInfo.Length:N0} bytes)");
// Console.WriteLine($"Lines: {lineCount:N0}");