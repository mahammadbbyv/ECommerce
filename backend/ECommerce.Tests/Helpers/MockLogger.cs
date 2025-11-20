using Microsoft.Extensions.Logging;
using Moq;

namespace ECommerce.Tests.Helpers;

public static class MockLogger
{
    public static ILogger<T> Create<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }
}
