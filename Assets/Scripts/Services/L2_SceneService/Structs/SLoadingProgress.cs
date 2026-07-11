namespace RedDust.Services.Scene
{
    /// <summary>
    /// Loading progress payload. Published via LoadingProgressEvent.
    /// </summary>
    public readonly struct SLoadingProgress
    {
        public readonly string PhaseName;
        public readonly float Progress;

        public SLoadingProgress(string phaseName, float progress)
        {
            PhaseName = phaseName;
            Progress = progress;
        }
    }
}
