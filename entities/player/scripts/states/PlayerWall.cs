using Godot;

public partial class PlayerWall : ICharacterState<PlayerController>
{    
    private bool _onCooldown = false;
    private double _wallRunCooldown = .2;
    private Vector3 _lastValidNormal = Vector3.Zero;

	public void Enter(PlayerController player)
    {
        _onCooldown = false;
        _wallRunCooldown = .2;
        SampleWall(player, out _, out _);
        player.animManager.movesetPlayback.Travel("wall_run");
    }

	public void Exit(PlayerController player) { }

	public void HandleInput(PlayerController player, InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            player.CameraLook(mouseMotion);
        }
        if (@event.IsActionPressed("action_jump", false) && !_onCooldown)
        {
            _onCooldown = true;
            Vector3 wallJump = _lastValidNormal * player.wallExitForce + Vector3.Up * player.wallJumpForce;
            player.Velocity += wallJump;
            player.alreadyJumped = true;
        }
    }

	public void Update(PlayerController player, double delta)
    {
        if (_onCooldown)
        {
            _wallRunCooldown -= delta;
            if (_wallRunCooldown <= 0)
            {
                _onCooldown = false;
                _wallRunCooldown = 0.2;
            }
        }
    }

	public void Physics(PlayerController player, double delta)
	{
        SampleWall(player, out Vector3 wNormal, out bool canWallRun);
		CharacterController.MovementParams p = player.GetWallRunParams();
		float dt = (float)delta;

		player.IntegrateHorizontal(player.wishDir, dt, p, grounded: false);
		player.IntegrateVertical(dt, p, applyGravity: true);

        if (!canWallRun)
        {
            player.ChangeState(new PlayerAir());
            return;
        }
        else
        {
            Vector3 localCamOffset = player.camBoom.GlobalBasis.Inverse() * (wNormal * player.wallRunCamOffset);
            player.camBoom.SetCameraOffset(localCamOffset);
            player.Velocity -= wNormal * player.wallStickForce * dt;
        }
	}

    public void SampleWall(PlayerController player, out Vector3 wNormal, out bool canWallRun)
    {
        Vector3 forward = player.GetHorizontalVelocity();
        forward.Y = 0;
        if (forward.Length() < .0001f) forward = player.GlobalTransform.Basis.Z;

        canWallRun = player.CanWallRun(out Vector3 wallNormal, out _);
        wNormal = wallNormal;
        Vector3 sideVector = Vector3.Up.Cross(forward.Normalized());
        float sideDot = sideVector.Dot(wallNormal);

        _lastValidNormal = wNormal != Vector3.Zero ? wNormal : _lastValidNormal;

        if (!canWallRun) return;

        player.animManager.UpdateWallRunSide(sideDot);
    }
}
