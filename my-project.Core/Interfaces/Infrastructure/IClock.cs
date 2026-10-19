namespace my_project.Core.Interfaces.Infrastructure;

// Abstraction over the current time, so services can be tested with a fixed clock
public interface IClock
{
    DateTime UtcNow { get; }
}
