namespace WitnessBackendEngineerTask.Common.Models;

/// <summary>
/// Per-title processing state persisted in Redis.
/// </summary>
/// <param name="TitleNumber">Normalized lease title number key (upper-case).</param>
/// <param name="Status">Current state value (Pending/Processing/Completed/Failed).</param>
/// <param name="Error">Failure details when <see cref="FailedState"/> is set.</param>
public sealed record LeaseProcessingStatus(string TitleNumber, string Status, string? Error)
{
    /// <summary>The API has queued work and is waiting for processing.</summary>
    public const string PendingState = "Pending";
    /// <summary>The Function has accepted the trigger and is actively parsing.</summary>
    public const string ProcessingState = "Processing";
    /// <summary>Parsing for this title is complete and result is available.</summary>
    public const string CompletedState = "Completed";
    /// <summary>Processing failed or the requested title was not found.</summary>
    public const string FailedState = "Failed";
}
