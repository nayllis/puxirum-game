using Godot;

public partial class PlayerFloor : IPlayerState
{
	public void Enter(PlayerController player)
	{
		player.alreadyJumped = false;
		player.animManager.movesetPlayback.Travel("walking");
		player.SnapVerticalToFloor();
	}

	public void Exit(PlayerController player) { }

	public void HandleInput(PlayerController player, InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			player.CameraLook(mouseMotion);
		}

		if (@event.IsActionPressed("action_jump") && !player.alreadyJumped)
		{
			player.Jump(player.jumpSpeed, stackVelocity: false);
			player.ChangeState(new PlayerAir());
		}
	}

	public void Update(PlayerController player, double delta) { }

	public void Physics(PlayerController player, double delta)
	{
		if (player.HasBufferedJump && !player.alreadyJumped)
		{
			player.ConsumeJumpBuffer();
			player.Jump(player.jumpSpeed, stackVelocity: false);
			player.ChangeState(new PlayerAir());
			return;
		}

		CharacterController.MovementParams p = player.GetGroundParams();
		float dt = (float)delta;

		player.IntegrateHorizontal(player.wishDir, dt, p, grounded: true);
		player.SnapVerticalToFloor();

		if (!player.IsOnFloor())
		{
			player.ChangeState(new PlayerAir());
		}
	}
}
