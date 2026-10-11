public interface ISentenceAnimatable
{
    void Animate(string sentence);
    void CompleteImmediately();
}

public interface IStageAnimationStatus
{
    bool IsStageAnimationComplete { get; }
}