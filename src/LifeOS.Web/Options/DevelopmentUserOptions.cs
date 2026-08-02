namespace LifeOS.Web.Options;

public sealed class DevelopmentUserOptions
{
    public const string SectionName = "DevelopmentUser";

    public Guid UserId { get; set; }
    public bool IsAuthenticated => true;
}