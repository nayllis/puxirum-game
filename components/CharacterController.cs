using Godot;

public partial class CharacterController : CharacterBody3D
{
	[ExportGroup("General")]
	[Export] public float health = 100;
	[Export] public float jumpSpeed = 7f;
	[Export] public float turnSpeed = 12f;
	[Export] public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	[Export] public Node3D meshRoot;
	[Export] public AnimationTree animTree;

	[ExportGroup("Step")]
	[Export(PropertyHint.Range, "0, 1, .01")] public float maxStepHeight = .5f;
	[Export(PropertyHint.Range, "0.1, 1, .01")] public float stepCheckDistance = .4f;
	[Export(PropertyHint.Range, "0.05, 0.3, .01")] public float stepLowHeight = .12f;

	public CollisionShape3D colShape;
	public bool stepEnabled = true;

	public Vector2 inputDir;
	public Vector3 wishDir;
	public Vector3 horizontalVelocity;


	public struct MovementParams
	{
		public float MaxSpeed;
		public float Acceleration;
		public float Deceleration;
		public float GravityMultiplier;

		public bool UseAirStrafe;
		public bool UseSlide;
	}

	public virtual void NodeSetup()
	{
		animTree ??= FindChild("AnimationTree") as AnimationTree;
		if (animTree == null) GD.PrintErr("AnimationTree not found in CharacterController");

		colShape = GetNodeOrNull<CollisionShape3D>("StandingShape")
			?? GetNodeOrNull<CollisionShape3D>("CollisionShape3D");

		FloorBlockOnWall = false;
		FloorConstantSpeed = true;
		FloorMaxAngle = Mathf.DegToRad(60f);
		FloorSnapLength = Mathf.Max(FloorSnapLength, .25f);
	}

	public override void _Ready()
	{
		NodeSetup();
		base._Ready();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		horizontalVelocity.X = Velocity.X;
		horizontalVelocity.Y = 0;
		horizontalVelocity.Z = Velocity.Z;

		if (stepEnabled)
			TryStepUp();
		MoveAndSlide();
	}

	public void SnapVerticalToFloor()
	{
		if (Velocity.Y < 0f)
		{
			Velocity = new Vector3(Velocity.X, 0f, Velocity.Z);
		}
	}

	public void SetStepEnabled(bool enabled)
	{
		stepEnabled = enabled;
		FloorBlockOnWall = !enabled;
		FloorConstantSpeed = enabled;
		FloorSnapLength = enabled ? .25f : 0f;
	}

	protected void TryStepUp()
	{
		if (!stepEnabled || !IsOnFloor())
			return;

		Vector3 horiz = new(Velocity.X, 0f, Velocity.Z);
		if (horiz.LengthSquared() < .0001f)
			return;

		float dt = (float)GetPhysicsProcessDeltaTime();
		Vector3 motion = horiz * dt;
		if (motion.Length() < stepCheckDistance)
			motion = horiz.Normalized() * stepCheckDistance;

		KinematicCollision3D hit = new();
		if (!TestMove(GlobalTransform, motion, hit))
			return;

		if (hit.GetNormal().Y >= Mathf.Cos(FloorMaxAngle))
			return;

		float step = Mathf.Max(stepLowHeight, .05f);
		for (float h = step; h <= maxStepHeight + .001f; h += step)
		{
			Transform3D lifted = GlobalTransform;
			lifted.Origin += Vector3.Up * h;
			if (TestMove(lifted, motion))
				continue;

			GlobalPosition += Vector3.Up * h;
			return;
		}
	}

	private float GetStepRadius()
	{
		if (colShape?.Shape is CapsuleShape3D capsule)
			return capsule.Radius;
		if (colShape?.Shape is CylinderShape3D cylinder)
			return cylinder.Radius;

		return .3f;
	}

	public void IntegrateHorizontal(Vector3 direction, float delta, MovementParams p, bool grounded)
	{
		Vector3 horiz = GetHorizontalVelocity();
		Vector3 flatDir = new(direction.X, 0f, direction.Z);
		Vector3 target = flatDir * p.MaxSpeed;

		bool reversing = horiz.Length() > .0001 && horiz.Dot(flatDir) < 0f;

		if (flatDir.LengthSquared() > 0.0001f && !reversing)
		{
			flatDir = flatDir.Normalized();

			if (p.UseSlide)
			{
				float speed = horiz.Length();
				if (speed > .0001f)
				{
					Vector3 newDir = horiz.Normalized().MoveToward(flatDir, p.Acceleration * delta);
					if (newDir.LengthSquared() > .0001f)
					{
						horiz = newDir.Normalized() * speed;
					}
				}
				horiz = ApplyDeceleration(horiz, p.Deceleration, delta);
			}
			else
			{
				if (!grounded && p.UseAirStrafe)
				{
					AccelerateAirStrafe(ref horiz, flatDir, p.MaxSpeed, p.Acceleration, delta);
				}
				else
				{
					horiz = horiz.MoveToward(target, p.Acceleration * delta);
				}
			}
		}
		else
		{
			horiz = ApplyDeceleration(horiz, p.Deceleration, delta);
		}

		Velocity = new Vector3(horiz.X, grounded && IsOnFloor() ? 0f : Velocity.Y, horiz.Z);
	}

	public void IntegrateVertical(float delta, MovementParams p, bool applyGravity)
	{
		if (!applyGravity)
		{
			return;
		}

		Velocity = new Vector3(
			Velocity.X,
			Velocity.Y - gravity * p.GravityMultiplier * delta,
			Velocity.Z
		);
	}

	public virtual void Jump(float speed, Vector3 direction = default, bool normalize = true, bool stackVelocity = true)
	{
		if (direction == default) direction = Vector3.Up;
		Vector3 impulse = (normalize ? direction.Normalized() : direction) * speed;
		Velocity = stackVelocity ? Velocity + impulse : new Vector3(Velocity.X + impulse.X, impulse.Y, Velocity.Z + impulse.Z);
	}

	public Vector3 GetHorizontalVelocity()
	{
		return new Vector3(Velocity.X, 0f, Velocity.Z);
	}

	public void FaceMoveDirection(Vector3 direction, double delta)
	{
		direction.Y = 0;

		if (direction.LengthSquared() < .0001f)
		{
			return;
		}

		float targetYaw = Mathf.Atan2(-direction.X, -direction.Z);
		meshRoot.Rotation = new Vector3(
			meshRoot.Rotation.X,
			Mathf.LerpAngle(meshRoot.Rotation.Y, targetYaw, 1f - Mathf.Exp(-turnSpeed * (float)delta)),
			meshRoot.Rotation.Z
		);
	}

	private static void AccelerateAirStrafe(
		ref Vector3 horiz,
		Vector3 wishDir,
		float maxSpeed,
		float acceleration,
		float delta)
	{
		float wishSpeed = maxSpeed;
		float currentAlongWish = horiz.Dot(wishDir);
		float addSpeed = wishSpeed - currentAlongWish;

		if (addSpeed <= 0f)
		{
			return;
		}

		float accelSpeed = Mathf.Min(acceleration * delta, addSpeed);
		horiz += wishDir * accelSpeed;
	}

	private static Vector3 ApplyDeceleration(Vector3 horiz, float deceleration, float delta)
	{
		float speed = horiz.Length();
		if (speed <= 0f)
		{
			return horiz;
		}

		speed = Mathf.Max(speed - deceleration * delta, 0f);
		return horiz.Normalized() * speed;
	}
}
