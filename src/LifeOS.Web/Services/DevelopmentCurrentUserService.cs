using LifeOS.Core.Abstractions;
using LifeOS.Web.Options;
using Microsoft.Extensions.Options;

namespace LifeOS.Web.Services;

public sealed class DevelopmentCurrentUserService
    : ICurrentUserService
{
    public DevelopmentCurrentUserService(
        IOptions<DevelopmentUserOptions> options)
    {
        if (options.Value.UserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Development user id has not been configured.");
        }

        UserId = options.Value.UserId;
    }

    public Guid UserId { get; }
    public bool IsAuthenticated => true;
}