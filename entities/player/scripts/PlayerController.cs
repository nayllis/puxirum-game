using Godot;

public partial class PlayerController : CharacterController
{
	private ICharacterState<PlayerController> _state = new PlayerAir();

	[ExportGroup("Ground Movement")]
	[Export(PropertyHint.Range, "1, 30, .5")] public float groundMaxSpeed = 10f;
	[Export(PropertyHint.Range, "1, 120, 1")] public float groundAcceleration = 90f;
	[Export(PropertyHint.Range, "1, 200, 1")] public float groundDeceleration = 140f;
	[Export(PropertyHint.Range, "0, 2, .1")] public double coyoteTime = .2;
	[Export(PropertyHint.Range, "0, 1, .01")] public double jumpBufferTime = .12;

	[ExportGroup("Air Movement")]
	[Export(PropertyHint.Range, "1, 30, .5")] public float airMaxSpeed = 10f;
	[Export(PropertyHint.Range, "1, 80, 1")] public float airAcceleration = 20f;
	[Export(PropertyHint.Range, "0, 40, 1")] public float airDeceleration = 2f;
	[Export(PropertyHint.Range, "0.1, 3, .1")] public float airGravityMultiplier = 1f;
	[Export(PropertyHint.Range, "0, 30, .5")] public float airInputSmoothing = 8f;
	[Export] public bool useAirStrafe = true;

	[ExportGroup("Dive Movement")]
	[Export(PropertyHint.Range, "0, 30, .5")] public float diveBoost = 5f;
	[Export(PropertyHint.Range, "1, 30, .5")] public float diveMaxSpeed = 10f;
	[Export(PropertyHint.Range, "1, 80, 1")] public float diveAcceleration = 20f;
	[Export(PropertyHint.Range, "0, 40, 1")] public float diveDeceleration = 2f;
	[Export(PropertyHint.Range, "0.1, 3, .1")] public float diveGravityMultiplier = 1f;
	[Export(PropertyHint.Range, "0, 30, .5")] public float diveInputSmoothing = 8f;

	[ExportGroup("Slide Movement")]
	[Export(PropertyHint.Range, "1, 30, .5")] public float slideMaxSpeed = 40f;
	[Export(PropertyHint.Range, "1, 80, 1")] public float slideAcceleration = 1f;
	[Export(PropertyHint.Range, "0, 40, 1")] public float slideDeceleration = 4f;
	[Export(PropertyHint.Range, "0.1, 3, .1")] public float slideGravityMultiplier = 1f;
	[Export(PropertyHint.Range, "0, 30, .5")] public float slideInputSmoothing = 16f;
	[Export(PropertyHint.Range, "0, 5, .1")] public float slideExitJump = .8f;

	[ExportGroup("WallRun Movement")]
	[Export(PropertyHint.Range, "0, 1, .01")] public float maxWallApproachDot = .65f;
	[Export(PropertyHint.Range, "0, 30, .5")] public float wallRunMinSpeed = 5f;
	[Export(PropertyHint.Range, "1, 30, .5")] public float wallRunMaxSpeed = 10f;
	[Export(PropertyHint.Range, "1, 80, 1")] public float wallRunAcceleration = 4f;
	[Export(PropertyHint.Range, "0, 40, 1")] public float wallRunDeceleration = 10f;
	[Export(PropertyHint.Range, "0, 30, .5")] public float wallJumpForce = 3f;
	[Export(PropertyHint.Range, "0, 30, .1")] public float wallExitForce = 6f;
	[Export(PropertyHint.Range, "0.1, 3, .1")] public float wallStickForce = 10f;
	[Export(PropertyHint.Range, "0, 1, .01, or_greater")] public float wallGravityMultiplier = .6f;

	[ExportGroup("Trampoline")]
	[Export(PropertyHint.Range, "1, 40, .5")] public float trampolineBoostSpeed = 5f;
	[Export(PropertyHint.Range, "1, 40, .5")] public float trampolineMaxSpeed = 50f;
	[Export(PropertyHint.Range, "0.05, 1, .05")] public float trampolineBounceCooldown = 0.15f;

	[ExportGroup("Camera")]
	[Export(PropertyHint.Range, ".1, 100, .1")] public float mouseSensitivity = 10f;
	[Export(PropertyHint.Range, "-90, 0, .5")] public float minPitch = -80f;
	[Export(PropertyHint.Range, "0, 90, .5")] public float maxPitch = 90f;
	[Export(PropertyHint.Range, "0, 10, .1")] public float wallRunCamOffset = .8f;

	public CollisionShape3D standingShape;
	public CollisionShape3D proneShape;
	public CameraRig camBoom;
	public Node3D camYaw;
	public PlayerAnimManager animManager;

	public float PlayerHeight => ((CylinderShape3D)standingShape.Shape).Height;
	public ICharacterState<PlayerController> GetState => _state;

	public Vector2 smoothInputDir;
	
	public bool HasBufferedJump => _jumpBufferTimer >= 0 && _jumpBufferTimer < jumpBufferTime;
	public void BufferJump() => _jumpBufferTimer = .0;
	public void ConsumeJumpBuffer() => _jumpBufferTimer = -1;

	public bool CanCoyoteJump { get; private set; } = false;
	public bool alreadyJumped = false;
	private double _coyoteTimer = 0;

	private float ScaledMouseSens => mouseSensitivity * .0001f;
	private double _jumpBufferTimer = -1;
	private float _trampolineBounceLock;

	public override void NodeSetup()
	{
		base.NodeSetup();

		standingShape = GetNode<CollisionShape3D>("StandingShape");
		standingShape.Disabled = false;
		proneShape = GetNode<CollisionShape3D>("ProneShape");
		proneShape.Disabled = true;

		camYaw = GetNode<Node3D>("CameraYaw");
		camBoom = camYaw.GetNode<CameraRig>("CameraBoom");

		if (animTree is not null) if (animTree is PlayerAnimManager manager) animManager = manager;
		else GD.PrintErr("AnimationTree is not a PlayerAnimManager");
		else GD.PrintErr("AnimationTree is null");
	}

	public void ChangeState(ICharacterState<PlayerController> state)
	{
		_state?.Exit(this);
		_state = state;
		_state.Enter(this);
	}

	public override void _Ready()
	{
		base._Ready();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("action_jump")) BufferJump();

		_state?.HandleInput(this, @event);
		base._UnhandledInput(@event);
	}

	public override void _Process(double delta)
	{
		_state?.Update(this, delta);
		base._Process(delta);

		FaceMoveDirection(GetHorizontalVelocity(), delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		UpdateWishDirection(delta);
		JumpChecks(delta);
		_state?.Physics(this, delta);
		base._PhysicsProcess(delta);
		if (_trampolineBounceLock > 0f)
			_trampolineBounceLock -= (float)delta;
		if (GetHorizontalVelocity().Length() > .1f) VelocityFov();
	}

	public void CameraLook(InputEventMouseMotion mouseMotion)
	{
		camBoom.RotateX(-mouseMotion.Relative.Y * ScaledMouseSens);
		camBoom.RotationDegrees = new Vector3(
			Mathf.Clamp(camBoom.RotationDegrees.X, minPitch, maxPitch),
			camBoom.RotationDegrees.Y,
			camBoom.RotationDegrees.Z
		);
		camYaw.RotateY(-mouseMotion.Relative.X * ScaledMouseSens);
		camBoom.relativeMouseMotion += mouseMotion.Relative / (float)GetPhysicsProcessDeltaTime();
	}

	public void VelocityFov()
	{
		float factor = Mathf.InverseLerp(.001f, 10f, GetHorizontalVelocity().Length());
		factor = Mathf.Clamp(factor, 0, 1);
		camBoom.DynamicFov(factor);
	}

	private void UpdateWishDirection(double delta)
	{
		if (IsOnFloor())
		{
			smoothInputDir = inputDir;
			wishDir = inputDir.LengthSquared() > 0.0001f
				? camYaw.GlobalBasis * new Vector3(inputDir.X, 0f, inputDir.Y)
				: Vector3.Zero;
			return;
		}

		bool hasInput = inputDir.LengthSquared() > 0.0001f;
		if (hasInput)
		{
			smoothInputDir = smoothInputDir.Lerp(inputDir, GameManager.LerpDelta(airInputSmoothing, delta));
		}
		else
		{
			smoothInputDir = smoothInputDir.Lerp(Vector2.Zero, GameManager.LerpDelta(airInputSmoothing * 2f, delta));
		}

		wishDir = smoothInputDir.LengthSquared() > 0.0001f
			? camYaw.GlobalBasis * new Vector3(smoothInputDir.X, 0f, smoothInputDir.Y)
			: Vector3.Zero;
	}

	public bool CanWallRun(out Vector3 wallNormal, out Vector3 tangent)
	{
		wallNormal = Vector3.Zero;
		tangent = Vector3.Zero;

		if (IsOnFloor() || !IsOnWall()) return false;

		Vector3 horiz = GetHorizontalVelocity();
		if (horiz.LengthSquared() < wallRunMinSpeed * wallRunMinSpeed) return false;
		Vector3 bestNormal = Vector3.Zero;
		float bestAlong = -1f;

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision3D col = GetSlideCollision(i);
			Vector3 n = col.GetNormal();

			if (Mathf.Abs(n.Y) > .2f) continue;
			n.Y = 0f;
			if (n.LengthSquared() < .0001f) continue;
			n = n.Normalized();
			Vector3 along = horiz.Slide(n);
			float alongSpeed = along.Length();
			float intoDot = Mathf.Abs(horiz.Normalized().Dot(n));

			if (alongSpeed < wallRunMinSpeed) continue;
			if (intoDot > maxWallApproachDot) continue;
			if (alongSpeed > bestAlong)
			{
				bestAlong = alongSpeed;
				bestNormal = n;
			}
		}

		if (bestAlong < 0f) return false;

		wallNormal = bestNormal;
		tangent = Vector3.Up.Cross(wallNormal).Normalized();
		if (tangent.Dot(horiz) < 0f) tangent = -tangent;
		return true;
	}

	public override void Jump(float speed, Vector3 direction = default, bool normalize = true, bool stackVelocity = true)
	{
		base.Jump(speed, direction, normalize, stackVelocity);
		alreadyJumped = true;
		ConsumeJumpBuffer();
	}

	public void BounceFromTrampoline()
	{
		if (_trampolineBounceLock > 0f)
			return;
		if (Velocity.Y > 0.5f)
			return;

		float speed = Mathf.Abs(Velocity.Y) + (HasBufferedJump ? trampolineBoostSpeed : 0);
		speed = Mathf.Clamp(speed, 0, trampolineMaxSpeed);

		Jump(speed, stackVelocity: false);
		if (GetState is not PlayerAir)
			ChangeState(new PlayerAir());
		_trampolineBounceLock = trampolineBounceCooldown;
	}


	public void JumpChecks(double delta)
	{
		if (IsOnFloor())
		{
			_coyoteTimer = 0;

		}
		else
		{
			_coyoteTimer += delta;
		}
		CanCoyoteJump = !IsOnFloor() && _coyoteTimer < coyoteTime && !alreadyJumped;

		if (_jumpBufferTimer >= 0)
		{
			_jumpBufferTimer += delta;
			if (_jumpBufferTimer >= jumpBufferTime) _jumpBufferTimer = -1;
		}
	}

	public void CollisionSwitch()
	{
		standingShape.Disabled = !standingShape.Disabled;
		proneShape.Disabled = !proneShape.Disabled;
	}

	public MovementParams GetGroundParams()
	{
		return new MovementParams
		{
			MaxSpeed = groundMaxSpeed,
			Acceleration = groundAcceleration,
			Deceleration = groundDeceleration,
			GravityMultiplier = 0f,
			UseAirStrafe = false,
			UseSlide = false
		};
	}

	public MovementParams GetAirParams()
	{
		return new MovementParams
		{
			MaxSpeed = airMaxSpeed,
			Acceleration = airAcceleration,
			Deceleration = airDeceleration,
			GravityMultiplier = airGravityMultiplier,
			UseAirStrafe = useAirStrafe,
			UseSlide = false
		};
	}

	public MovementParams GetDiveParams()
	{
		return new MovementParams
		{
			MaxSpeed = diveMaxSpeed,
			Acceleration = diveAcceleration,
			Deceleration = diveDeceleration,
			GravityMultiplier = diveGravityMultiplier,
			UseAirStrafe = false,
			UseSlide = false
		};
	}

	public MovementParams GetSlideParams()
	{
		return new MovementParams
		{
			MaxSpeed = slideMaxSpeed,
			Acceleration = slideAcceleration,
			Deceleration = slideDeceleration,
			GravityMultiplier = slideGravityMultiplier,
			UseAirStrafe = false,
			UseSlide = true
		};
	}

	public MovementParams GetWallRunParams()
	{
		return new MovementParams
		{
			MaxSpeed = wallRunMaxSpeed,
			Acceleration = wallRunAcceleration,
			Deceleration = wallRunDeceleration,
			GravityMultiplier = wallGravityMultiplier,
			UseAirStrafe = true,
			UseSlide = false
		};
	}
}
