using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

// Zod-like schema validation library for C#
public static class Z
{
    public static StringSchema String() => new StringSchema();
    public static NumberSchema Number() => new NumberSchema();
    public static ObjectSchema<T> Object<T>() where T : new() => new ObjectSchema<T>();
}

public abstract class Schema<T>
{
    protected List<Func<T, ValidationResult>> validators = new();
    
    public ValidationResult Parse(T value)
    {
        foreach (var validator in validators)
        {
            var result = validator(value);
            if (!result.IsValid)
                return result;
        }
        return ValidationResult.Success();
    }
    
    public bool TryParse(T value, out ValidationResult result)
    {
        result = Parse(value);
        return result.IsValid;
    }
}

public class StringSchema : Schema<string>
{
    public StringSchema Min(int length)
    {
        validators.Add(value => 
            value?.Length >= length 
                ? ValidationResult.Success() 
                : ValidationResult.Error($"String must be at least {length} characters"));
        return this;
    }
    
    public StringSchema Max(int length)
    {
        validators.Add(value => 
            value?.Length <= length 
                ? ValidationResult.Success() 
                : ValidationResult.Error($"String must be at most {length} characters"));
        return this;
    }
    
    public StringSchema Email()
    {
        validators.Add(value => 
            !string.IsNullOrEmpty(value) && value.Contains("@") && value.Contains(".") 
                ? ValidationResult.Success() 
                : ValidationResult.Error("Invalid email format"));
        return this;
    }
    
    public StringSchema NotEmpty()
    {
        validators.Add(value => 
            !string.IsNullOrEmpty(value) 
                ? ValidationResult.Success() 
                : ValidationResult.Error("String cannot be empty"));
        return this;
    }
}

public class NumberSchema : Schema<int>
{
    public NumberSchema Min(int min)
    {
        validators.Add(value => 
            value >= min 
                ? ValidationResult.Success() 
                : ValidationResult.Error($"Number must be at least {min}"));
        return this;
    }
    
    public NumberSchema Max(int max)
    {
        validators.Add(value => 
            value <= max 
                ? ValidationResult.Success() 
                : ValidationResult.Error($"Number must be at most {max}"));
        return this;
    }
    
    public NumberSchema Positive()
    {
        validators.Add(value => 
            value > 0 
                ? ValidationResult.Success() 
                : ValidationResult.Error("Number must be positive"));
        return this;
    }
}

public class ObjectSchema<T> : Schema<T> where T : new()
{
    private Dictionary<string, Func<T, ValidationResult>> propertyValidators = new();
    
    public ObjectSchema<T> Property<TProp>(Expression<Func<T, TProp>> propertyExpression, Schema<TProp> schema)
    {
        var propertyName = GetPropertyName(propertyExpression);
        var compiledExpression = propertyExpression.Compile();
        
        propertyValidators[propertyName] = obj =>
        {
            var propValue = compiledExpression(obj);
            var result = schema.Parse(propValue);
            if (!result.IsValid)
            {
                return ValidationResult.Error($"{propertyName}: {result.Error}");
            }
            return ValidationResult.Success();
        };
        
        return this;
    }
    
    public new ValidationResult Parse(T value)
    {
        // First run base validators
        var baseResult = base.Parse(value);
        if (!baseResult.IsValid)
            return baseResult;
            
        // Then run property validators
        foreach (var validator in propertyValidators.Values)
        {
            var result = validator(value);
            if (!result.IsValid)
                return result;
        }
        
        return ValidationResult.Success();
    }
    
    private string GetPropertyName<TProp>(Expression<Func<T, TProp>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        throw new ArgumentException("Expression must be a property access");
    }
}

public class ValidationResult
{
    public bool IsValid { get; private set; }
    public string Error { get; private set; }
    
    private ValidationResult(bool isValid, string error = null)
    {
        IsValid = isValid;
        Error = error;
    }
    
    public static ValidationResult Success() => new ValidationResult(true);
    public static ValidationResult Error(string error) => new ValidationResult(false, error);
}
