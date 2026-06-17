public class JumpCommand : Command
{
    private readonly ISkateboardActor _actor;

    public JumpCommand(ISkateboardActor actor)
    {
        _actor = actor;
    }

    public override void Execute()
    {
        _actor.Jump();
    }
}
