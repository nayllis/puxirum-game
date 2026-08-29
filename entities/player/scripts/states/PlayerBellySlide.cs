using Godot;

public partial class PlayerBellySlide : IPlayerState
{
    private float _velocityThreshold = .5f;
	private bool _exitingState = false;
	public void Enter(PlayerController player)
	{
		player.alreadyJumped = false;
        player.CollisionSwitch();
		player.animManager.movesetPlayback.Travel("diving");
		player.SnapVerticalToFloor();
	}

	public void Exit(PlayerController player)
    {
        player.CollisionSwitch();
    }

	public void HandleInput(PlayerController player, InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			player.CameraLook(mouseMotion);
		}
        if (@event.IsActionPressed("action_jump"))
        {
            player.Jump(player.jumpSpeed, stackVelocity: false);
        }
	}

	public void Update(PlayerController player, double delta) { }

	public void Physics(PlayerController player, double delta)
	{
		CharacterController.MovementParams p = player.GetSlideParams();
		float dt = (float)delta;

		player.IntegrateHorizontal(player.wishDir, dt, p, grounded: true);
        player.SnapVerticalToFloor();

        if (player.GetHorizontalVelocity().Length() < _velocityThreshold && !_exitingState)
        {
			_exitingState = true;
			player.Jump(player.slideExitJump, stackVelocity: false);
            player.ChangeState(new PlayerAir());
			return;
        }

		if (!player.IsOnFloor())
		{
			player.ChangeState(new PlayerAir());
			return;
		}
	}
}
