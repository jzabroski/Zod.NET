// Usage example
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }
}

public class Example(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public static async Task RunExample()
    {
        // Define schema (similar to Zod)
        var personSchema = Z.Object<Person>()
            .Property(p => p.Name, Z.String().NotEmpty().Min(2).Max(50))
            .Property(p => p.Age, Z.Number().Positive().Min(1).Max(120))
            .Property(p => p.Email, Z.String().Email());
        
        // Test with valid data
        var validPerson = new Person 
        { 
            Name = "John Doe", 
            Age = 30, 
            Email = "john@example.com" 
        };
        
        var result1 = personSchema.Parse(validPerson);
        testOutputHelper.WriteLine($"Valid person: {result1.IsValid}");
        Assert.True(result1.IsValid);

        // Test with invalid data
        var invalidPerson = new Person 
        { 
            Name = "J", // Too short
            Age = -5,   // Negative
            Email = "invalid-email" 
        };
        
        var result2 = personSchema.Parse(invalidPerson);
        testOutputHelper.WriteLine($"Invalid person: {result2.IsValid}");
        Assert.False(result2.IsValid);
        testOutputHelper.WriteLine($"Error: {result2.Error}");
        
        // String validation example
        var emailSchema = Z.String().Email().NotEmpty();
        var emailResult = emailSchema.Parse("test@example.com");
        Assert.True(emailResult.IsValid);
        testOutputHelper.WriteLine($"Email valid: {emailResult.IsValid}");
        
        // Number validation example
        var ageSchema = Z.Number().Min(0).Max(150);
        var ageResult = ageSchema.Parse(25);
        Assert.True(ageResult.IsValid);
        testOutputHelper.WriteLine($"Age valid: {ageResult.IsValid}");
    }
}
