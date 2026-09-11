using Godot;

public interface IInteractable
{
	int Remaining { get; }
	Vector3 GlobalPosition { get; }
	bool TryAssign(IndigenousController npc);
	void Release(IndigenousController npc);
	void ReleaseAll();
	bool OnCommandWhenFull();
	void NotifyArrived(IndigenousController npc);
}

public partial class Interactable : StaticBody3D, IInteractable
{
	[Export(PropertyHint.Range, "1, 8, 1")] public int requiredCount = 1;
	[Export] public Godot.Collections.Array<Marker3D> slots = [];
	[Export(PropertyHint.Range, ".1, 10, .1")] public float zoneRadius = 1.5f;

	public Godot.Collections.Array<IndigenousController> Occupants { get; private set; } = [];

	[Signal]
	public delegate void SatisfiedChangedEventHandler(bool satisfied);

	public virtual int Remaining => Mathf.Max(0, requiredCount - Occupants.Count);
	public bool IsSatisfied => ComputeSatisfied();

	private bool _wasSatisfied;

	public override void _Ready()
	{
		base._Ready();
		AddToGroup("interactable");
		if (slots.Count == 0)
			CollectSlots(this);

		if (slots.Count < requiredCount)
			GD.PrintErr($"{Name}: requiredCount={requiredCount} but only {slots.Count} slots");

		SetupUseZone();
	}

	public bool TryAssign(IndigenousController npc)
	{
		if (npc == null || Occupants.Contains(npc) || Remaining <= 0)
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

		RefreshSatisfied();
	}

	public virtual bool OnCommandWhenFull()
	{
		return false;
	}

	public virtual void NotifyArrived(IndigenousController npc)
	{
		if (!Occupants.Contains(npc))
			return;

		RefreshSatisfied();
	}

	public void ReleaseAll()
	{
		while (Occupants.Count > 0)
			Release(Occupants[0]);
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

	protected virtual bool ComputeSatisfied()
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

	protected void RefreshSatisfied()
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
			if (child is Marker3D marker)
				slots.Add(marker);
			CollectSlots(child);
		}
	}
}
