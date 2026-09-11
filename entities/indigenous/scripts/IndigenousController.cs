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

	[ExportGroup("Occupy")]
	[Export(PropertyHint.Range, "0.1, 4, .05")] public float occupyArriveDistance = 2.2f;

	[ExportGroup("Follow")]
	[Export(PropertyHint.Range, "0.2, 2, .05")] public float followStopDistance = 0.7f;
	[Export(PropertyHint.Range, "0.4, 3, .05")] public float followResumeDistance = 1.2f;

	[ExportGroup("Actions")]
	[Export(PropertyHint.Layers3DPhysics)] public uint trampolineDetectMask = 8u;

	public CharacterBlackboard Board { get; } = new();
	public NavigationAgent3D agent;
	public CollisionShape3D standingShape;
	public Area3D trampolineArea;
	public IndigenousAnimManager animManager;

	private ICharacterState<IndigenousController> _state = new IndigenousAir();
	private Vector3 _pathDir;
	private Vector3 _settledCommandPoint;
	private bool _followSettled;
	private float _avoidanceMaxSpeed = -1f;

	public override void NodeSetup()
	{
		base.NodeSetup();

		standingShape = GetNode<CollisionShape3D>("StandingShape");
		trampolineArea = GetNode<Area3D>("TrampolineArea");
		trampolineArea.CollisionLayer = 0;
		trampolineArea.CollisionMask = trampolineDetectMask;
		trampolineArea.Monitorable = false;
		trampolineArea.Monitoring = false;
		trampolineArea.BodyEntered += OnTrampolineBodyEntered;

		agent = GetNode<NavigationAgent3D>("NavAgent");
		agent.VelocityComputed += OnVelocityComputed;

		if (animTree is IndigenousAnimManager manager)
			animManager = manager;
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
		FaceMoveDirection(GetHorizontalVelocity(), delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		TryArriveOccupy();
		UpdateWishDirection(delta);
		_state?.Physics(this, delta);
		base._PhysicsProcess(delta);
	}

	public void SetTrampolineActive(bool isActive)
	{
		if (trampolineArea == null)
			return;

		trampolineArea.Monitoring = isActive;
		if (isActive)
			BounceOverlapping();
	}

	private void OnTrampolineBodyEntered(Node3D body)
	{
		TryBounce(body);
	}

	private void BounceOverlapping()
	{
		if (trampolineArea == null || !trampolineArea.Monitoring)
			return;

		foreach (Node3D body in trampolineArea.GetOverlappingBodies())
			TryBounce(body);
	}

	private static void TryBounce(Node3D body)
	{
		if (body is PlayerController player)
			player.BounceFromTrampoline();
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

	public void ConfirmOccupyArrive()
	{
		if (Board.goal != CharacterGoal.Occupy || Board.occupying)
			return;
		if (Board.interactable == null)
			return;

		Board.ArriveOccupy();
		Board.interactable.NotifyArrived(this);
	}

	private void TryArriveOccupy()
	{
		if (Board.goal != CharacterGoal.Occupy || Board.occupying)
			return;

		float arriveSq = occupyArriveDistance * occupyArriveDistance;
		if (HorizontalDistanceSq(Board.slot?.GlobalPosition) <= arriveSq
			|| HorizontalDistanceSq(Board.interactable?.GlobalPosition) <= arriveSq)
		{
			ConfirmOccupyArrive();
		}
	}

	private bool UpdateFollowSettle(bool allowSettle)
	{
		if (Board.hasMoveTarget
			&& HorizontalDistanceSq(_settledCommandPoint, Board.moveTarget) > 0.04f)
		{
			_followSettled = false;
			_settledCommandPoint = Board.moveTarget;
		}

		if (!allowSettle)
		{
			_followSettled = false;
			return false;
		}

		float distSq = HorizontalDistanceSq(Board.moveTarget);
		float resume = Mathf.Max(followResumeDistance, followStopDistance);
		if (_followSettled)
		{
			if (distSq <= resume * resume)
				return true;

			_followSettled = false;
			return false;
		}

		if (distSq <= followStopDistance * followStopDistance)
		{
			_followSettled = true;
			_settledCommandPoint = Board.moveTarget;
			return true;
		}

		return false;
	}

	private float HorizontalDistanceSq(Vector3? point)
	{
		if (point == null)
			return float.MaxValue;

		return HorizontalDistanceSq(GlobalPosition, point.Value);
	}

	private static float HorizontalDistanceSq(Vector3 a, Vector3 b)
	{
		Vector3 to = b - a;
		to.Y = 0f;
		return to.LengthSquared();
	}

	public MovementParams GetGroundParamsForMove()
	{
		MovementParams p = GetGroundParams();
		if (_avoidanceMaxSpeed >= 0f)
			p.MaxSpeed = _avoidanceMaxSpeed;
		return p;
	}
	public MovementParams GetAirParamsForMove()
	{
		MovementParams p = GetAirParams();
		if (_avoidanceMaxSpeed >= 0f)
			p.MaxSpeed = Mathf.Min(_avoidanceMaxSpeed, airMaxSpeed);
		return p;
	}

	private void UpdateWishDirection(double delta)
	{
		if (Board.goal != CharacterGoal.Follow && Board.goal != CharacterGoal.MoveTo)
			_followSettled = false;

		if (Board.occupying)
		{
			StopWish();
			return;
		}

		if (Board.goal == CharacterGoal.Follow && Board.hasMoveTarget)
		{
			if (UpdateFollowSettle(Board.allowFollowSettle))
			{
				if (agent.AvoidanceEnabled)
					agent.AvoidanceEnabled = false;
				StopWish();
				return;
			}

			bool wantAvoidance = true;
			if (agent.AvoidanceEnabled != wantAvoidance)
				agent.AvoidanceEnabled = wantAvoidance;

			if (HorizontalDistanceSq(Board.moveTarget) <= 16f)
			{
				SteerToward(Board.moveTarget);
				return;
			}
		}
		else if (Board.goal == CharacterGoal.MoveTo && Board.hasMoveTarget)
		{
			if (UpdateFollowSettle(true))
			{
				StopWish();
				return;
			}

			if (agent.AvoidanceEnabled != true)
				agent.AvoidanceEnabled = true;

			if (HorizontalDistanceSq(Board.moveTarget) <= 16f)
			{
				SteerToward(Board.moveTarget);
				return;
			}
		}
		else
		{
			bool wantAvoidance = Board.goal != CharacterGoal.Occupy;
			if (agent.AvoidanceEnabled != wantAvoidance)
				agent.AvoidanceEnabled = wantAvoidance;
		}

		if (Board.goal == CharacterGoal.Occupy && Board.interactable != null)
		{
			if (HorizontalDistanceSq(Board.interactable.GlobalPosition) <= 36f)
			{
				SteerToward(Board.interactable.GlobalPosition);
				return;
			}
		}

		if (Board.goal == CharacterGoal.Occupy && Board.slot != null)
			Board.moveTarget = Board.slot.GlobalPosition;

		if (!Board.hasMoveTarget)
		{
			StopWish();
			return;
		}

		if (agent.TargetPosition.DistanceSquaredTo(Board.moveTarget) > 0.25f)
			agent.TargetPosition = Board.moveTarget;

		if (agent.IsNavigationFinished())
		{
			if (Board.goal == CharacterGoal.Occupy || Board.goal == CharacterGoal.Follow
				|| Board.goal == CharacterGoal.MoveTo)
				SteerToward(Board.moveTarget);
			else
				StopWish();
			return;
		}

		Vector3 toNext = agent.GetNextPathPosition() - GlobalPosition;
		toNext.Y = 0f;
		_pathDir = toNext.LengthSquared() > .0001f ? toNext.Normalized() : Vector3.Zero;
		ApplyWish(_pathDir);
	}

	private void SteerToward(Vector3 worldPoint)
	{
		Vector3 to = worldPoint - GlobalPosition;
		to.Y = 0f;
		if (to.LengthSquared() < 0.0001f)
		{
			StopWish();
			return;
		}

		ApplyWish(to.Normalized());
	}

	private void ApplyWish(Vector3 dir)
	{
		_pathDir = dir;
		if (agent.AvoidanceEnabled) agent.Velocity = dir.LengthSquared() > .0001f ? dir * groundMaxSpeed : Vector3.Zero;
		else
		{
			_avoidanceMaxSpeed = -1f;
			wishDir = dir;
		}
	}

	private void StopWish()
	{
		_pathDir = Vector3.Zero;
		wishDir = Vector3.Zero;
		_avoidanceMaxSpeed = -1f;
		if (agent.AvoidanceEnabled) agent.Velocity = Vector3.Zero;
	}

	private void OnVelocityComputed(Vector3 safeVelocity)
	{
		if (Board.occupying || !Board.hasMoveTarget || _followSettled)
		{
			wishDir = Vector3.Zero;
			_avoidanceMaxSpeed = 0f;
			return;
		}

		safeVelocity.Y = 0f;
		float speed = safeVelocity.Length();
		if (speed > 0.0001f)
		{
			wishDir = safeVelocity / speed;
			_avoidanceMaxSpeed = Mathf.Min(speed, groundMaxSpeed);
		}
		else
		{
			wishDir = Vector3.Zero;
			_avoidanceMaxSpeed = 0f;
		}
	}
}