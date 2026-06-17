public interface ISkateboardActor
{
    void Push();
    void Turn(float direction);
    void Jump();

    bool IsGrounded { get; }
    bool IsGrinding { get; }
    float CurrentSpeed { get; }
}
