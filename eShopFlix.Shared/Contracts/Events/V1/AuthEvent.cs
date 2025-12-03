using Contracts.DTOs;

namespace Contracts.Events.V1
{
    public record UserRegisteredV1(long UserId, UserDto User, DateTime OccurredAt);
    public record UserUpdatedV1(long UserId, UserDto User, DateTime OccurredAt);
    public record UserDeletedV1(long UserId, DateTime OccurredAt);
}
