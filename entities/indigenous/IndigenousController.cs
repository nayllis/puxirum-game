using Godot;

public partial class IndigenousController : CharacterController
{
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

	public NavigationAgent3D agent;
	public CollisionShape3D standingShape;

	private ICharacterState<IndigenousController> _state = new IndigenousAir();
	private Vector3 _pathDir;

	public override void NodeSetup()
	{
		base.NodeSetup();

		standingShape = GetNode<CollisionShape3D>("StandingShape");

		agent = GetNode<NavigationAgent3D>("NavAgent");
		agent.VelocityComputed += OnVelocityComputed;
	}

	public override void _Ready()
	{
		base._Ready();
		ChangeState(new IndigenousFloor());
	}

	public override void _Process(double delta)
	{
		_state?.Update(this, delta);
		base._Process(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		UpdateWishDirection(delta);
		_state?.Physics(this, delta);
		base._PhysicsProcess(delta);
	}

	public void ChangeState(ICharacterState<IndigenousController> state)
	{
		_state?.Exit(this);
		_state = state;
		_state.Enter(this);
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

	private void UpdateWishDirection(double delta)
	{
		if (agent.IsNavigationFinished())
		{
			_pathDir = Vector3.Zero;
			if (agent.AvoidanceEnabled) agent.Velocity = Vector3.Zero;
			else wishDir = Vector3.Zero;
			return;
		}
		Vector3 toNext = agent.GetNextPathPosition() - GlobalPosition;
		toNext.Y = 0f;
		_pathDir = toNext.LengthSquared() > .0001f ? toNext.Normalized() : Vector3.Zero;
		if (agent.AvoidanceEnabled) agent.Velocity = _pathDir * groundMaxSpeed;
		else wishDir = _pathDir;
	}

	private void OnVelocityComputed(Vector3 safeVelocity)
	{
		safeVelocity.Y = 0f;
		wishDir = safeVelocity.LengthSquared() > .0001f ? safeVelocity.Normalized() : Vector3.Zero;
	}
}