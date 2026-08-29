using Godot;

public partial class MeshSquash : Node3D
{
	[Export] public Node3D visual;
	[Export] public CharacterBody3D body;

	[Export(PropertyHint.Range, "0,1,0.001")] public float landAmount = 0.04f;
	[Export(PropertyHint.Range, "0,1,0.001")] public float jumpAmount = 0.03f;
	[Export(PropertyHint.Range, "0,0.2,0.001")] public float accelAmount = 0.03f;
	[Export(PropertyHint.Range, "1,40,0.1")] public float stiffness = 18f;
	[Export(PropertyHint.Range, "1,40,0.1")] public float damping = 12f;
	[Export(PropertyHint.Range, "0.05,0.5,0.01")] public float maxDeform = 0.3f;
	[Export(PropertyHint.Range, "0,8,0.1")] public float accelDeadzone = 1.5f;

	[ExportGroup("Tilt")]
	[Export(PropertyHint.Range, "0,0.3,0.005")] public float turnTilt = 0.08f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] public float maxTilt = 0.25f;
	[Export(PropertyHint.Range, "1,40,0.1")] public float tiltStiffness = 14f;
	[Export(PropertyHint.Range, "1,40,0.1")] public float tiltDamping = 10f;

	private Node3D _visual;
	private Node3D _visualRoot;
	private CharacterBody3D _body;
	private Vector3 _restPos;
	private float _footY;
	private float _prevYaw;
	private float _roll;
	private float _rollVel;
	private Vector3 _prevVelocity;
	private bool _wasOnFloor;
	private bool _ready;
	private float _scaleY = 1f;
	private float _scaleYVel;
	private float _scaleZ = 1f;
	private float _scaleZVel;

	public override void _Ready()
	{
		_visual = visual ?? GetNodeOrNull<Node3D>("../Character/Skeleton3D/CharacterMesh");
		_visualRoot = GetParent<Node3D>();
		_body = body ?? GameManager.FindBody(this);
		if (_visual == null || _visualRoot == null || _body == null)
		{
			GD.PushError("MeshSquash needs a visual mesh and a CharacterBody3D ancestor.");
			SetProcess(false);
			SetPhysicsProcess(false);
			return;
		}

		_restPos = _visual.Position;
		if (_visual is MeshInstance3D mesh && mesh.Mesh != null)
		{
			_footY = mesh.Mesh.GetAabb().Position.Y;
		}

		_prevYaw = _visualRoot.Rotation.Y;

		_prevVelocity = _body.Velocity;
		_wasOnFloor = _body.IsOnFloor();
	}

	public void Squash(float amount)
	{
		_scaleYVel -= Mathf.Abs(amount);
	}

	public void Stretch(float amount)
	{
		_scaleZVel += amount;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_ready)
		{
			_prevVelocity = _body.Velocity;
			_wasOnFloor = _body.IsOnFloor();
			_ready = true;
			return;
		}

		Vector3 velocity = _body.Velocity;
		bool onFloor = _body.IsOnFloor();

		if (onFloor && !_wasOnFloor)
		{
			Squash(Mathf.Max(0f, -_prevVelocity.Y) * landAmount);
		}
		else if (!onFloor && _wasOnFloor && velocity.Y > 0.5f)
		{
			_scaleYVel += velocity.Y * jumpAmount;
		}

		float speed = new Vector3(velocity.X, 0f, velocity.Z).Length();
		float prevSpeed = new Vector3(_prevVelocity.X, 0f, _prevVelocity.Z).Length();
		float speedDelta = speed - prevSpeed;
		if (Mathf.Abs(speedDelta) >= accelDeadzone)
		{
			Stretch(speedDelta * accelAmount);
		}

		_wasOnFloor = onFloor;
		_prevVelocity = velocity;
	}

	public override void _Process(double delta)
	{
		float dt = Mathf.Min((float)delta, 0.05f);
		Spring(ref _scaleY, ref _scaleYVel, dt);
		Spring(ref _scaleZ, ref _scaleZVel, dt);

		float scaleX = 1f / Mathf.Max(_scaleY * _scaleZ, 0.0001f);
		_visual.Scale = new Vector3(scaleX, _scaleY, _scaleZ);
		_visual.Position = _restPos + new Vector3(0f, -_footY * (1f - _scaleY), 0f);

		float yaw = _visualRoot.Rotation.Y;
		float yawRate = Mathf.Wrap(yaw - _prevYaw, -Mathf.Pi, Mathf.Pi) / dt;
		_prevYaw = yaw;

		float targetRoll = Mathf.Clamp(yawRate * turnTilt, -maxTilt, maxTilt);
		SpringToward(ref _roll, ref _rollVel, targetRoll, tiltStiffness, tiltDamping, dt);
		_visualRoot.Rotation = new Vector3(_visualRoot.Rotation.X, yaw, _roll);
	}

	private void Spring(ref float value, ref float vel, float dt)
	{
		SpringToward(ref value, ref vel, 1f, stiffness, damping, dt);
		value = Mathf.Clamp(value, 1f - maxDeform, 1f + maxDeform);
	}

	private static void SpringToward(ref float value, ref float vel, float target, float spring, float damp, float dt)
	{
		vel += (target - value) * spring * dt;
		vel *= Mathf.Exp(-damp * dt);
		value += vel * dt;
	}
}
