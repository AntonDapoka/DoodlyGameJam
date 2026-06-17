public class TurnCommand : Command
{
    private readonly ISkateboardActor _actor;
    private readonly float _direction;

    public TurnCommand(ISkateboardActor actor, float direction) // -1 for left, 1 for right.
    {
        _actor = actor;
        _direction = direction;
    }

    public override void Execute()
    {
        _actor.Turn(_direction);
    }
}
