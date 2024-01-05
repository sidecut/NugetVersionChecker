using System.ComponentModel.DataAnnotations;

namespace NugetVersionChecker;

class PathExistsAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string path && File.Exists(path))
        {
#pragma warning disable CS8603 // Possible null reference return.
            return ValidationResult.Success;
#pragma warning restore CS8603 // Possible null reference return.
        }
        return new ValidationResult($"The path '{value}' is not found.");
    }
}
