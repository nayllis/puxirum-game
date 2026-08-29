using Godot;

public partial class IndigenousFloor : ICharacterState<IndigenousController>
{
	public void Enter(IndigenousController indigenous)
	{
		indigenous.SnapVerticalToFloor();
	}
	public void Exit(IndigenousController indigenous){}
	public void HandleInput(IndigenousController indigenous, InputEvent @event){}
	public void Update(IndigenousController indigenous, double delta){}
	public void Physics(IndigenousController indigenous, double delta)
	{
		CharacterController.MovementParams p = indigenous.GetGroundParams();
		float dt = (float)delta;

		indigenous.IntegrateHorizontal(indigenous.wishDir, dt, p, grounded: true);
		indigenous.SnapVerticalToFloor();

		if (!indigenous.IsOnFloor())
		{
			indigenous.ChangeState(new IndigenousAir());
		}
	}
}