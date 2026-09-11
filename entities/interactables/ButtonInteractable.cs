using Godot;

public partial class ButtonInteractable : Interactable
{
	[Export(PropertyHint.Layers3DPhysics)] public uint pressMask = 8u;
	[Export] public Vector3 pressZoneSize = new(1.6f, 0.8f, 1.6f);

	public bool IsPressed => IsSatisfied;

	private Godot.Collections.Array<Node3D> _pressers = [];

	public override int Remaining => Mathf.Max(0, requiredCount - Mathf.Max(_pressers.Count, Occupants.Count));

	public override void _Ready()
	{
		base._Ready();
		SetupPressZone();
	}

	public override bool OnCommandWhenFull()
	{
		ReleaseAll();
		return true;
	}

	protected override bool ComputeSatisfied()
	{
		return _pressers.Count >= requiredCount;
	}

	private void SetupPressZone()
	{
		Area3D zone = new()
		{
			Name = "PressZone",
			CollisionLayer = 0,
			CollisionMask = pressMask,
			Monitoring = true,
			Monitorable = false,
			Position = new Vector3(0f, pressZoneSize.Y * 0.5f, 0f)
		};
		CollisionShape3D shape = new()
		{
			Shape = new BoxShape3D { Size = pressZoneSize }
		};
		zone.AddChild(shape);
		AddChild(zone);
		zone.BodyEntered += OnPressEntered;
		zone.BodyExited += OnPressExited;
	}

	private void OnPressEntered(Node3D body)
	{
		if (!IsPresser(body) || _pressers.Contains(body))
			return;

		_pressers.Add(body);
		if (body is IndigenousController npc)
			npc.ConfirmOccupyArrive();
		RefreshSatisfied();
	}

	private void OnPressExited(Node3D body)
	{
		if (!_pressers.Remove(body))
			return;

		if (body is IndigenousController npc && Occupants.Contains(npc))
			Release(npc);
		RefreshSatisfied();
	}

	private static bool IsPresser(Node3D body)
	{
		return body is PlayerController or IndigenousController;
	}
}
