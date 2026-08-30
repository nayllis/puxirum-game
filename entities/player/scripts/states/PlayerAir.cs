using Godot;

public partial class PlayerAir : ICharacterState<PlayerController>
{
	public void Enter(PlayerController player)
	{
		player.animManager.movesetPlayback.Travel("jumping");
	}

	public void Exit(PlayerController player) { }

	public void HandleInput(PlayerController player, InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			player.CameraLook(mouseMotion);
		}
		if (@event.IsActionPressed("action_jump"))
		{
			if (player.CanCoyoteJump)
			{
				player.Jump(player.jumpSpeed, stackVelocity: false);
			}
			else if (player.wishDir.Length() > .001f)
			{
				GameManager.RayCastResult rr = GameManager.TestRayCollisionPoint(player,
				player.GlobalPosition, player.GlobalPosition - new Vector3(0, 1f - player.PlayerHeight/2f, 0),
				exceptions: [player.GetRid()]);

				if (rr.hasHit) return;

				player.Velocity += player.wishDir * player.diveBoost;
				player.ChangeState(new PlayerDive());
			}
		}
	}

	public void Update(PlayerController player, double delta) { }

	public void Physics(PlayerController player, double delta)
	{
		CharacterController.MovementParams p = player.GetAirParams();
		float dt = (float)delta;

		player.IntegrateHorizontal(player.wishDir, dt, p, grounded: false);
		player.IntegrateVertical(dt, p, applyGravity: true);

		if (player.IsOnFloor())
		{
			player.ChangeState(new PlayerFloor());
		}

        if (player.CanWallRun(out _, out _))
		{
			player.ChangeState(new PlayerWall());
		}
	}
}
