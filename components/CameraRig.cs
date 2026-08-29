using Godot;
using System;

public partial class CameraRig : Node3D
{
    [Export] public PlayerController player;
    [Export(PropertyHint.Layers3DPhysics)] public uint cameraCollisionMask = 1;
    [Export] public float offsetLerpSpeed = 2f;
    [Export] public float cameraApproachLerpSpeed = 36f;
    [Export] public float cameraPushLerpSpeed = 4f;
    [Export] public float minDistanceCamera = .25f;
    [Export] public float horizontalTiltAmount = .1f;
    [Export] public float horizontalMaxTilt = 5f;
    [Export] public float horizontalTiltLerp = 4f;
    [Export] public float verticalTiltAmount = 1f;
    [Export] public float verticalMaxTilt = 5f;
    [Export] public float verticalTiltLerp = 1f;
    [Export] public float maxExtraFov = 15;
    [Export] public float fovLerp = 12f;
    public Node3D cameraPos;
    public Camera3D camera;

    public Vector3 Velocity { get; private set; }
    public Vector2 relativeMouseMotion = Vector2.Zero;

    private Vector3 _offsetTarget = Vector3.Zero;
    private Vector3 _currentOffset = Vector3.Zero;
    private bool _isOffsetting = false; 
    private float _currentDist = 0f;
    private SphereShape3D _springSphere = new();
    private Vector3 _previousCamPosition = Vector3.Zero;
    private float _tiltZ;
    private float _tiltX;
    private float _defaultFov;
    private float _fovTarget = 90f;
    private float _maxFov;
    private bool _isFoving = false;

    public void NodeSetup()
    {
        player ??= GameManager.FindBody(this) as PlayerController;
        cameraPos = GetNode<Node3D>("CameraPos");
        camera = GetNode<Camera3D>("Camera3D");

        _currentDist = cameraPos.Position.Length();
        camera.Position = cameraPos.Position.Normalized() * _currentDist;
        _previousCamPosition = camera.GlobalPosition;
        _defaultFov = camera.Fov;
        _maxFov = camera.Fov + maxExtraFov;
    }

	public override void _Ready()
	{
        NodeSetup();
		base._Ready();
	}

    public override void _PhysicsProcess(double delta)
    {
        camera.Fov = Mathf.Lerp(camera.Fov, _fovTarget, GameManager.LerpDelta(fovLerp, delta));
        if (!_isFoving) _fovTarget = _defaultFov;
        _isFoving = false;

        _currentOffset = _currentOffset.Lerp(_offsetTarget, GameManager.LerpDelta(offsetLerpSpeed, delta));
        if (!_isOffsetting) _offsetTarget = Vector3.Zero;
        _isOffsetting = false;
        base._PhysicsProcess(delta);
        CameraSpring(delta);
        UpdateVelocity(delta);
        CameraTilt(delta);
    }

    public void UpdateVelocity(double delta)
    {
        Velocity = GameManager.CalculateInstantVelocity(_previousCamPosition, camera.GlobalPosition, delta);
    }

    public void DynamicFov(float fovFactor)
    {
        fovFactor = Mathf.Clamp(fovFactor, 0, 1);
        float fov = Mathf.Lerp(_defaultFov, _maxFov, fovFactor);
        _fovTarget = fov;
        _isFoving = true;
    }

    public void CameraTilt(double delta)
    {
        float hTarget = Mathf.Clamp(relativeMouseMotion.X * Mathf.DegToRad(horizontalTiltAmount), -Mathf.DegToRad(horizontalMaxTilt), Mathf.DegToRad(horizontalMaxTilt));
        float vTarget = Mathf.Clamp(-relativeMouseMotion.Y * Mathf.DegToRad(verticalTiltAmount), -Mathf.DegToRad(verticalMaxTilt), Mathf.DegToRad(verticalMaxTilt));
        relativeMouseMotion = Vector2.Zero;
        _tiltZ = Mathf.Lerp(_tiltZ, hTarget, GameManager.LerpDelta(horizontalTiltLerp, delta));
        _tiltX = Mathf.Lerp(_tiltX, vTarget, GameManager.LerpDelta(verticalTiltLerp, delta));
        Vector3 rot = camera.Rotation;
        rot.Z = _tiltZ;
        rot.X = _tiltX;
        camera.Rotation = rot;
    }

    public void CameraSpring(double delta)
    {
        float aspect = camera.GetViewport().GetVisibleRect().Size.X / camera.GetViewport().GetVisibleRect().Size.Y;
        float halfH = camera.Near * Mathf.Tan(Mathf.DegToRad(camera.Fov) * .5f);
        float halfW = halfH * aspect;
        float padding = Mathf.Sqrt(halfW * halfW + halfH * halfH);

        Vector3 desiredLocal = cameraPos.Position + _currentOffset;
        Vector3 desiredGlobal = ToGlobal(desiredLocal);

        float maxDist = desiredLocal.Length();
        if (maxDist < .001f) return;
        Vector3 hit = SphereGetCollision(padding, GlobalPosition, desiredGlobal, out _);
        float targetDist = GlobalPosition.DistanceTo(hit);
        float targetSpeed = targetDist < _currentDist ? cameraApproachLerpSpeed : cameraPushLerpSpeed;
        _currentDist = Mathf.Lerp(_currentDist, targetDist, GameManager.LerpDelta(targetSpeed, delta));
        _currentDist = Mathf.Clamp(_currentDist, minDistanceCamera, maxDist);
        camera.Position = desiredLocal.Normalized() * _currentDist;
    }

    public void SetCameraOffset(Vector3 localOffset)
    {
        _offsetTarget = localOffset;
        _isOffsetting = true;
    }

    public Vector3 SphereGetCollision(float radius, Vector3 origin, Vector3 end, out bool hasHit)
    {
        _springSphere.Radius = radius;
        PhysicsShapeQueryParameters3D query = new()
        {
          ShapeRid = _springSphere.GetRid(),
          CollisionMask = cameraCollisionMask,
          Transform = new(Basis.Identity, origin),
          Motion = end - origin,
          Exclude = [player.GetRid()],
          CollideWithBodies = true,
          CollideWithAreas = false,
        };
        float[] fractions = GetWorld3D().DirectSpaceState.CastMotion(query);
        float safe = fractions[0];
        hasHit = safe < 1f;
        return origin + query.Motion * safe;
    }
}
