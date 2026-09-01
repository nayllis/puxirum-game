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

	protected void TryStepUp()
	{
		if (!IsOnFloor() || wishDir.LengthSquared() < .0001f)
			return;

		Vector3 forward = wishDir;
		forward.Y = 0f;
		if (forward.LengthSquared() < .0001f)
			return;
		forward = forward.Normalized();

		float maxHitNormal = .3f;
		float radius = GetStepRadius();
		float checkDist = Mathf.Max(stepCheckDistance, radius + .1f);
		Vector3 probe = forward * checkDist;
		uint mask = CollisionMask;
		Godot.Collections.Array<Rid> exceptions = [GetRid()];
		Vector3 feet = GlobalPosition;

		GameManager.RayCastResult low = GameManager.TestRayCollisionPoint(
			this,
			feet + Vector3.Up * stepLowHeight,
			feet + probe + Vector3.Up * stepLowHeight,
			mask, exceptions);
		if (!low.hasHit || low.normal.Y > maxHitNormal)
			return;

		Vector3 wallNormal = low.normal;
		wallNormal.Y = 0f;
		if (wallNormal.LengthSquared() < .0001f)
			wallNormal = -forward;
		else
			wallNormal = wallNormal.Normalized();

		Vector3 highFrom = low.hit + Vector3.Up * (maxStepHeight - stepLowHeight) + wallNormal * .02f;
		GameManager.RayCastResult high = GameManager.TestRayCollisionPoint(
			this,
			highFrom,
			highFrom - wallNormal * .15f,
			mask, exceptions);
		if (high.hasHit && high.normal.Y < maxHitNormal)
			return;

		Vector3 downFrom = low.hit - wallNormal * .1f + Vector3.Up * maxStepHeight;
		GameManager.RayCastResult down = GameManager.TestRayCollisionPoint(
			this,
			downFrom,
			downFrom - Vector3.Up * maxStepHeight,
			mask, exceptions);
		if (!down.hasHit || down.normal.Y < maxHitNormal)
			return;

		float lift = down.hit.Y - feet.Y + .02f;
		if (lift < .03f || lift > maxStepHeight + .05f)
			return;

		Vector3 ontoStep = -wallNormal * .15f;
		Transform3D lifted = GlobalTransform;
		lifted.Origin += Vector3.Up * lift;
		if (TestMove(lifted, ontoStep))
			return;

		GlobalPosition += Vector3.Up * lift;
	}

	private float GetStepRadius()
	{
		if (colShape?.Shape is CylinderShape3D cylinder)
			return cylinder.Radius;

		return .3f;
	}

	public void IntegrateHorizontal(Vector3 direction, float delta, MovementParams p, bool grounded)
	{
		Vector3 horiz = GetHorizontalVelocity();

		if (grounded && IsOnFloor())
		{
			direction = direction.Slide(GetFloorNormal());
		}

		if (direction.LengthSquared() > 0.0001f)
		{
			direction = direction.Normalized();

			if (p.UseSlide)
			{
				float speed = horiz.Length();
				if (speed > .0001f)
				{
					Vector3 newDir = horiz.Normalized().MoveToward(direction, p.Acceleration * delta);
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
					AccelerateAirStrafe(ref horiz, direction, p.MaxSpeed, p.Acceleration, delta);
				}
				else
				{
					Vector3 target = direction * p.MaxSpeed;
					horiz = horiz.MoveToward(target, p.Acceleration * delta);
				}
			}
		}
		else
		{
			horiz = ApplyDeceleration(horiz, p.Deceleration, delta);
		}

		Velocity = new Vector3(horiz.X, Velocity.Y, horiz.Z);
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
