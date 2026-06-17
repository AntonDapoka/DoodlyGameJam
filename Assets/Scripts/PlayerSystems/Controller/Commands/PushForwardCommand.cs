public class PushForwardCommand : Command
{
    private readonly ISkateboardActor _actor;

    public PushForwardCommand(ISkateboardActor actor)
    {
        _actor = actor;
    }

    public override void Execute()
    {
        _actor.Push();
    }
}
