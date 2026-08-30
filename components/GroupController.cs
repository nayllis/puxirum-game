using Godot;

public partial class GroupController : Node
{
	[Export] public PlayerController player;
	[Export(PropertyHint.Range, ".1, 50, .1")] public float defaultRecruitRadius = 10f;
	[Export(PropertyHint.Layers3DPhysics)] public uint recruitCollisionMask = 4u;
	[Export] public Godot.Collections.Array<IndigenousController> Puxirum { get; private set; } = [];
	public Godot.Collections.Array<Rid> PuxirumRid { get; private set; } = [];

	private SphereShape3D _recruitShape = new();
	private Godot.Collections.Array<Rid> exceptions = [];

	public override void _Ready()
	{
		base._Ready();
		if (Puxirum.Count > 0)
		{
			foreach (IndigenousController i in Puxirum)
			{
				AddToPuxirum(i);
			}
		}

		CallDeferred(MethodName.AddPlayerToExceptions);
	}

	private void AddPlayerToExceptions()
	{
		exceptions.Add(player.GetRid());
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		PuxirumCheck();
	}

	public void PuxirumCheck()
	{
		_recruitShape.Radius = defaultRecruitRadius;
		GameManager.ShapeResult[] results = GameManager.TestShapeCollision(player, player.GlobalPosition, _recruitShape, recruitCollisionMask, exceptions);

		for (int i = 0; i < results.Length; i++)
		{
			if (results[i].collider is IndigenousController indigenous)
			{
				AddToPuxirum(indigenous);
				GD.Print($"added {indigenous.Name} to puxirum");
			}
		}
	}

	public void AddToPuxirum(IndigenousController indigenous)
	{
		if (!Puxirum.Contains(indigenous))
		{
			Puxirum.Add(indigenous);
		}
		if (!PuxirumRid.Contains(indigenous.GetRid()))
		{
			PuxirumRid.Add(indigenous.GetRid());
			exceptions.Add(indigenous.GetRid());
		}
	}

	public void RemoveFromPuxirum(IndigenousController indigenous)
	{
		Puxirum.Remove(indigenous);
		PuxirumRid.Remove(indigenous.GetRid());
		exceptions.Remove(indigenous.GetRid());
	}

	public void SetTarget(Vector3 target)
	{
		foreach (IndigenousController p in Puxirum)
		{
			p.agent.TargetPosition = target;
		}
	}
}