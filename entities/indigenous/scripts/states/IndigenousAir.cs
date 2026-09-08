using Godot;

public partial class IndigenousAir : ICharacterState<IndigenousController>
{
	public void Enter(IndigenousController indigenous)
	{
		indigenous.animManager?.movesetPlayback.Travel("jumping");
	}
	public void Exit(IndigenousController indigenous){}
	public void HandleInput(IndigenousController indigenous, InputEvent @event){}
	public void Update(IndigenousController indigenous, double delta){}
	public void Physics(IndigenousController indigenous, double delta)
	{
		CharacterController.MovementParams p = indigenous.GetAirParamsForMove();
		float dt = (float)delta;

		indigenous.IntegrateHorizontal(indigenous.wishDir, dt, p, grounded: false);
		indigenous.IntegrateVertical(dt, p, applyGravity: true);

		if (indigenous.IsOnFloor())
		{
			indigenous.ChangeState(new IndigenousFloor());
		}
	}
}