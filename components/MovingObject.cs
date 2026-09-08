using Godot;

public partial class MovingObject : AnimatableBody3D
{
	public enum MovementMode
	{
		Loop,
		PingPong,
		OneShot,
		Random
	}

	[Export] public bool isEnabled = true;
	[Export] public NodePath toggle;
	[Export] public MeshInstance3D mesh;

	[ExportGroup("Movement Settings")]
	[Export] public MovementMode mode = MovementMode.OneShot;
	[Export] public Marker3D[] targetMarkers;
	[Export] public double timePerSegment = 1.5;
	[Export] public double waitTimeAtTarget = 0.0;
	[Export] public double startDelay = 0.0;
	[Export] public bool startAutomatically = false;
	[Export(PropertyHint.Range, "0, 100, 1")] public int loops = 0;

	[ExportGroup("Tween Settings")]
	[Export] public Tween.EaseType easeType = Tween.EaseType.InOut;
	[Export] public Tween.TransitionType transitionType = Tween.TransitionType.Sine;
	[Export] public bool useGlobalSpace = true;

	private Tween _tween;
	private Transform3D[] _waypoints;
	private int _currentIndex = 0;
	private bool _isMoving = false;
	private bool _meshIsParent = false;

	public override void _Ready()
	{
		base._Ready();
		mesh ??= GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
		_meshIsParent = mesh != null && mesh.GetParent() == this;
		EnsureCollision();
		CacheWaypoints();

		Node source = GetNodeOrNull(toggle);
		if (source is ToggleInteractable toggleSource)
		{
			toggleSource.Toggled += OnToggled;
			if (toggleSource.IsOn)
				OnToggled(true);
		}
		else if (source is Interactable holdSource)
		{
			holdSource.SatisfiedChanged += OnToggled;
			if (holdSource.IsSatisfied)
				OnToggled(true);
		}
		else if (toggle != null && !toggle.IsEmpty)
			GD.PrintErr($"{Name}: Interactable not found at '{toggle}'");

		if (_waypoints == null || _waypoints.Length < 2)
		{
			GD.PushWarning($"{Name} needs at least 2 markers to move");
			return;
		}

		if (startAutomatically)
			StartMovement();
	}

	public void StartMovement()
	{
		if (!isEnabled) GD.Print($"{Name} is disabled. Not moving.");
		if (_isMoving || _waypoints == null || _waypoints.Length < 2 || !isEnabled)
			return;

		_isMoving = true;
		CreateTweenMovement(forward: true);
	}

	public void StopMovement()
	{
		_tween?.Kill();
		_isMoving = false;
	}

	public void GoToTarget(int index)
	{
		if (_waypoints == null || index < 0 || index >= _waypoints.Length)
			return;

		StopMovement();
		_currentIndex = index;
		if (useGlobalSpace)
			GlobalTransform = _waypoints[index];
		else
			Transform = _waypoints[index];
	}

	private void OnToggled(bool isOn)
	{
		if (_waypoints == null || _waypoints.Length < 2)
			return;

		if (mode == MovementMode.OneShot)
		{
			HandleOneShotSignal(isOn);
			return;
		}

		if (isOn)
		{
			if (!_isMoving)
				StartMovement();
			_tween?.Play();
		}
		else
			_tween?.Pause();
	}

	private void HandleOneShotSignal(bool shouldOpen)
	{
		_tween?.Kill();
		_isMoving = true;
		CreateTweenMovement(forward: shouldOpen);
	}

	private void EnsureCollision()
	{
		foreach (Node child in GetChildren())
		{
			if (child is CollisionShape3D)
				return;
		}

		if (mesh?.Mesh == null)
			return;

		AddChild(new CollisionShape3D
		{
			Shape = mesh.Mesh.CreateConvexShape(),
			Transform = mesh.Transform
		});
	}

	private void CacheWaypoints()
	{
		if (targetMarkers == null || targetMarkers.Length == 0)
			return;

		_waypoints = new Transform3D[targetMarkers.Length];
		for (int i = 0; i < targetMarkers.Length; i++)
		{
			Marker3D marker = targetMarkers[i];
			if (marker == null)
				continue;

			_waypoints[i] = useGlobalSpace ? marker.GlobalTransform : marker.Transform;
		}
	}

	private void CreateTweenMovement(bool forward)
	{
		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetEase(easeType).SetTrans(transitionType);

		if (startDelay > 0.0)
			_tween.TweenInterval(startDelay);

		switch (mode)
		{
			case MovementMode.Loop:
				CreateLoopTween();
				_tween.SetLoops(loops);
				break;
			case MovementMode.PingPong:
				CreatePingPongTween();
				_tween.SetLoops(loops);
				break;
			case MovementMode.OneShot:
				CreateOneShotTween(forward);
				break;
			case MovementMode.Random:
				CreateRandomTween();
				break;
		}
	}

	private void CreateLoopTween()
	{
		for (int i = 0; i < _waypoints.Length; i++)
		{
			AddMoveTo(_waypoints[i]);
			AddWait();
		}
	}

	private void CreatePingPongTween()
	{
		for (int i = 1; i < _waypoints.Length; i++)
		{
			AddMoveTo(_waypoints[i]);
			AddWait();
		}

		for (int i = _waypoints.Length - 2; i >= 0; i--)
		{
			AddMoveTo(_waypoints[i]);
			AddWait();
		}
	}

	private void CreateOneShotTween(bool forward)
	{
		int start = forward ? 0 : _waypoints.Length - 1;
		int end = forward ? _waypoints.Length : -1;
		int step = forward ? 1 : -1;
		for (int i = start; i != end; i += step)
		{
			if (i == start)
				continue;

			AddMoveTo(_waypoints[i]);
			AddWait();
		}
	}

	private void CreateRandomTween()
	{
		_tween.TweenCallback(Callable.From(MoveToRandomTarget));
	}

	private void AddMoveTo(Transform3D target)
	{
		NodePath property = useGlobalSpace ? "global_transform" : "transform";
		_tween.TweenProperty(this, property, target, timePerSegment);
	}

	private void AddWait()
	{
		if (waitTimeAtTarget > 0.0)
			_tween.TweenInterval(waitTimeAtTarget);
	}

	private void MoveToRandomTarget()
	{
		if (_waypoints == null || _waypoints.Length < 2)
			return;

		int nextIndex = _currentIndex;
		while (nextIndex == _currentIndex)
			nextIndex = GD.RandRange(0, _waypoints.Length - 1);

		_currentIndex = nextIndex;
		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetEase(easeType).SetTrans(transitionType);
		AddMoveTo(_waypoints[_currentIndex]);
		AddWait();
		_tween.TweenCallback(Callable.From(MoveToRandomTarget));
	}
}
