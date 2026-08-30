using Godot;

public partial class Pushable : RigidBody3D, IInteractable
{
	[Export(PropertyHint.Range, "1, 8, 1")] public int requiredCount = 2;
	[Export] public Godot.Collections.Array<Marker3D> slots = [];
	[Export(PropertyHint.Range, ".1, 10, .1")] public float zoneRadius = 1.5f;
	[Export] public Marker3D tip;
	[Export(PropertyHint.Range, "0.5, 8, .1")] public float tipHeight = 2.8f;
	[Export(PropertyHint.Range, "1, 200, 1")] public float impulsePerPerson = 15f;
	[Export(PropertyHint.Range, "0.1, 0.9, .05")] public float toppleDot = 0.4f;

	[Signal]
	public delegate void SatisfiedChangedEventHandler(bool satisfied);
	[Signal]
	public delegate void ToppledEventHandler();

	public Godot.Collections.Array<IndigenousController> Occupants { get; private set; } = [];
	public virtual int Remaining => _toppled ? 0 : Mathf.Max(0, requiredCount - Occupants.Count);
	public bool IsSatisfied => ComputeSatisfied();

	private bool _wasSatisfied;
	private bool _hasPushed;
	private bool _toppled;

	public override void _Ready()
	{
		base._Ready();
		AddToGroup("interactable");
		if (slots.Count == 0)
			CollectSlots(this);

		if (slots.Count < requiredCount)
			GD.PrintErr($"{Name}: requiredCount={requiredCount} but only {slots.Count} slots");

		tip ??= GetNodeOrNull<Marker3D>("Tip");
		SetupUseZone();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		if (_toppled || !_hasPushed)
			return;

		if (GlobalTransform.Basis.Y.Dot(Vector3.Up) < toppleDot)
		{
			_toppled = true;
			EmitSignal(SignalName.Toppled);
			GD.Print($"{Name} toppled");
			ReleaseAll();
		}
	}

	public bool TryAssign(IndigenousController npc)
	{
		if (_toppled || npc == null || Occupants.Contains(npc) || Remaining <= 0)
			return false;

		Marker3D slot = GetFreeSlot();
		if (slot == null)
			return false;

		Occupants.Add(npc);
		npc.Board.SetOccupy(slot, this);
		return true;
	}

	public void Release(IndigenousController npc)
	{
		if (npc == null || !Occupants.Contains(npc))
			return;

		Occupants.Remove(npc);
		if (npc.Board.interactable == this)
			npc.Board.ClearOccupy();

		if (!_toppled)
			_hasPushed = false;

		RefreshSatisfied();
	}

	public bool OnCommandWhenFull()
	{
		if (_toppled)
			return true;

		ReleaseAll();
		return true;
	}

	public void NotifyArrived(IndigenousController npc)
	{
		if (!Occupants.Contains(npc))
			return;

		RefreshSatisfied();
		if (IsSatisfied)
			ApplyToppleImpulse();
	}

	public void ReleaseAll()
	{
		while (Occupants.Count > 0)
			Release(Occupants[0]);
	}

	private void ApplyToppleImpulse()
	{
		if (_toppled || _hasPushed)
			return;

		int pushing = 0;
		for (int i = 0; i < Occupants.Count; i++)
		{
			if (Occupants[i].Board.occupying)
				pushing++;
		}

		if (pushing <= 0)
			return;

		Vector3 dir = GetPushDir();
		if (dir.LengthSquared() < 0.0001f)
			return;

		_hasPushed = true;
		Freeze = false;
		Sleeping = false;
		Vector3 offset = GlobalTransform.Basis * GetTipOffset();
		ApplyImpulse(dir * impulsePerPerson * pushing, offset);
		GD.Print($"{Name} impulse x{pushing}");
	}

	private Vector3 GetPushDir()
	{
		Vector3 dir;
		if (slots.Count > 0 && slots[0] != null)
		{
			dir = GlobalPosition - slots[0].GlobalPosition;
		}
		else
		{
			dir = -GlobalTransform.Basis.Z;
		}

		dir.Y = 0f;
		return dir.LengthSquared() > 0.0001f ? dir.Normalized() : -GlobalTransform.Basis.Z;
	}

	private Vector3 GetTipOffset()
	{
		if (tip != null)
			return tip.Position;

		return Vector3.Up * tipHeight;
	}

	private void SetupUseZone()
	{
		Area3D zone = new()
		{
			Name = "UseZone",
			CollisionLayer = 0,
			CollisionMask = 8,
			Monitoring = true,
			Monitorable = false
		};
		CollisionShape3D shape = new()
		{
			Shape = new SphereShape3D { Radius = zoneRadius }
		};
		zone.AddChild(shape);
		AddChild(zone);
		zone.BodyEntered += OnUseZoneEntered;
	}

	private void OnUseZoneEntered(Node3D body)
	{
		if (body is IndigenousController npc)
			npc.ConfirmOccupyArrive();
	}

	private bool ComputeSatisfied()
	{
		if (Occupants.Count < requiredCount)
			return false;

		for (int i = 0; i < Occupants.Count; i++)
		{
			if (!Occupants[i].Board.occupying)
				return false;
		}

		return true;
	}

	private void RefreshSatisfied()
	{
		bool now = ComputeSatisfied();
		if (now == _wasSatisfied)
			return;

		_wasSatisfied = now;
		EmitSignal(SignalName.SatisfiedChanged, now);
		GD.Print($"{Name} satisfied={now}");
	}

	private Marker3D GetFreeSlot()
	{
		for (int i = 0; i < slots.Count; i++)
		{
			Marker3D slot = slots[i];
			if (slot == null)
				continue;

			bool used = false;
			for (int j = 0; j < Occupants.Count; j++)
			{
				if (Occupants[j].Board.slot == slot)
				{
					used = true;
					break;
				}
			}

			if (!used)
				return slot;
		}

		return null;
	}

	private void CollectSlots(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is Marker3D marker && marker.Name != "Tip")
				slots.Add(marker);
			CollectSlots(child);
		}
	}
}
