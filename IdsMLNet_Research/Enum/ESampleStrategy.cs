namespace IdsMLNet_Research.Enum
{
    /// <summary>
    /// Enumerates the Sampling strategies for data.
    /// </summary>
    public enum ESampleStrategy
    {
        All, // All data, i.e. no sampling
        CompleteOnly, // Only data that is complete - See AuthEvent.IsComplete()
        Demo, // Testing only.
        Day8And9, // Data from days 8 and 9 of the study
        RedOnly, // Red Team Events only
        RandomSample, // Naive sampler
        SMOTE,
        RandomUpSample,
        RandomDownSample
    }
}
