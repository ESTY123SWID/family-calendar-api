using my_project.Core.Interfaces.Infrastructure;

namespace my_project.Service.Infrastructure;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
