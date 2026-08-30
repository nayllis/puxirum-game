using Godot;

public partial class PlayerDive : ICharacterState<PlayerController>
{
	public void Enter(PlayerController player)
	{
		player.animManager.movesetPlayback.Travel("diving");
	}

	public void Exit(PlayerController player) { }

	public void HandleInput(PlayerController player, InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			player.CameraLook(mouseMotion);
		}
	}

	public void Update(PlayerController player, double delta) { }

	public void Physics(PlayerController player, double delta)
	{
		CharacterController.MovementParams p = player.GetDiveParams();
		float dt = (float)delta;

		player.IntegrateHorizontal(player.wishDir, dt, p, grounded: true);
        player.IntegrateVertical(dt, p, applyGravity: true);

		if (player.IsOnFloor())
		{
			player.ChangeState(new PlayerBellySlide());
		}
	}
}
