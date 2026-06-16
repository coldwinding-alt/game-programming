// The player’s next choice after the tutorial ends

// After the tutorial is completed, players can choose to replay the tutorial, enter free training, or start a quick match.

namespace mlp
{
    /// <summary>The player's next choice after the tutorial is: None, replay the tutorial, enter training, and start quick matching. </summary>
    public enum mlpTutorialNextAction
    {
        None,
        ReplayTutorial,
        StartTraining,
        StartQuickMatch
    }
}
