namespace SfmPackageBuilder.Core.Staging;

public sealed class StagingResult
{
    private StagingResult(StagingSession? session, StagingFailure? failure, StagingSession? cancelledSession = null)
    {
        Session = session;
        Failure = failure;
        CancelledSession = cancelledSession;
    }

    public bool Succeeded => Session is not null;

    public bool Cancelled => Failure?.Kind == StagingFailureKind.Cancelled;

    public StagingSession? Session { get; }

    public StagingFailure? Failure { get; }

    public StagingSession? CancelledSession { get; }

    public static StagingResult Success(StagingSession session) => new(session, null);

    public static StagingResult Failed(StagingFailure failure) => new(null, failure);

    public static StagingResult CancelledResult(StagingFailure failure, StagingSession session) =>
        new(null, failure, session);
}
