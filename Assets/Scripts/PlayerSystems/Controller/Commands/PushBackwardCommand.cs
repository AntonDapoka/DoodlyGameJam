public class PushBackwardCommand : Command
{
    private readonly ISkateboardActor _actor;

    public PushBackwardCommand(ISkateboardActor actor)
    {
        _actor = actor;
    }

    public override void Execute()
    {
        _actor.PushBackward();
    }
}
