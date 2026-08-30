using System.Collections.Generic;
using Godot;

public partial class GroupController : Node
{
	[Export] public PlayerController player;
	[Export(PropertyHint.Range, ".1, 50, .1")] public float defaultRecruitRadius = 10f;
	[Export(PropertyHint.Layers3DPhysics)] public uint recruitColLayer = 4u;
	[Export] public Godot.Collections.Array<IndigenousController> Puxirum { get; private set; } = [];
	public Godot.Collections.Array<Rid> PuxirumRid { get; private set; } = [];

	[ExportGroup("Follow")]
	[Export(PropertyHint.Range, "0.2, 2, .05")] public float trailSampleDistance = 0.35f;
	[Export(PropertyHint.Range, "0.6, 4, .05")] public float followSpacing = 1.5f;
	[Export(PropertyHint.Range, "0, 4, .05")] public float followSettlePlayerSpeed = 0.65f;

	private SphereShape3D _recruitShape = new();
	private Godot.Collections.Array<Rid> exceptions = [];
	private readonly List<Vector3> _trail = [];

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
		UpdateFollow();
		TrampolineForPlayer();
	}

	public void PuxirumCheck()
	{
		_recruitShape.Radius = defaultRecruitRadius;
		GameManager.ShapeResult[] results = GameManager.TestShapeCollision(player, player.GlobalPosition, _recruitShape, recruitColLayer, exceptions);

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
			indigenous.Board.SetFollow();
			if (_trail.Count == 0)
				SeedTrail();
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

	public void TrampolineForPlayer()
	{
		if (player.GetState is PlayerAir or PlayerDive) SetTrampoline();
		else SetTrampoline(false);
	}

	public void SetTrampoline(bool isActive = true)
	{
		foreach (IndigenousController i in Puxirum)
			i.SetTrampolineActive(isActive);
	}

	public void SetTarget(Vector3 target)
	{
		foreach (IndigenousController p in Puxirum)
		{
			if (p.Board.IsBusy) continue;
			p.Board.SetToGo(target);
		}
	}

	public bool AssignTo(IInteractable interactable)
	{
		if (interactable == null)
			return false;

		int need = interactable.Remaining;
		if (need <= 0)
		{
			interactable.OnCommandWhenFull();
			return true;
		}

		List<IndigenousController> idle = [];
		foreach (IndigenousController p in Puxirum)
		{
			if (!p.Board.IsBusy)
				idle.Add(p);
		}

		Node3D target = (Node3D)interactable;
		idle.Sort((a, b) =>
			a.GlobalPosition.DistanceSquaredTo(target.GlobalPosition)
				.CompareTo(b.GlobalPosition.DistanceSquaredTo(target.GlobalPosition)));

		if (idle.Count < need)
		{
			GD.Print($"{target.Name}: need {need}, idle {idle.Count}");
			return false;
		}

		List<IndigenousController> assigned = [];
		for (int i = 0; i < need; i++)
		{
			if (!interactable.TryAssign(idle[i]))
			{
				for (int j = 0; j < assigned.Count; j++)
					interactable.Release(assigned[j]);
				return false;
			}

			assigned.Add(idle[i]);
		}

		return true;
	}

	public void ResumeFollowAll()
	{
		foreach (IndigenousController p in Puxirum)
		{
			p.Board.interactable?.Release(p);
			p.Board.SetFollow();
		}
	}

	private void UpdateFollow()
	{
		if (Puxirum.Count == 0)
			return;

		SampleTrail();

		int slot = 0;
		int samplesPerSlot = Mathf.Max(1, Mathf.RoundToInt(followSpacing / trailSampleDistance));
		bool playerStill = player.GetHorizontalVelocity().LengthSquared()
			< followSettlePlayerSpeed * followSettlePlayerSpeed;

		for (int i = 0; i < Puxirum.Count; i++)
		{
			IndigenousController npc = Puxirum[i];
			if (npc.Board.IsBusy)
				continue;

			if (npc.Board.goal == CharacterGoal.MoveTo)
				continue;

			npc.Board.allowFollowSettle = playerStill;
			npc.Board.SetFollowTarget(GetTrailPoint(slot, samplesPerSlot));
			npc.agent.AvoidancePriority = Mathf.Clamp(0.85f - slot * 0.05f, 0.2f, 1f);
			slot++;
		}
	}

	private void SampleTrail()
	{
		if (_trail.Count == 0)
			SeedTrail();

		Vector3 pos = player.GlobalPosition;
		if (pos.DistanceSquaredTo(_trail[_trail.Count - 1]) < trailSampleDistance * trailSampleDistance)
			return;

		_trail.Add(pos);

		int followers = 0;
		foreach (IndigenousController p in Puxirum)
		{
			if (!p.Board.IsBusy)
				followers++;
		}

		int samplesPerSlot = Mathf.Max(1, Mathf.RoundToInt(followSpacing / trailSampleDistance));
		int max = Mathf.Max(12, followers * samplesPerSlot + 6);
		while (_trail.Count > max)
			_trail.RemoveAt(0);
	}

	private void SeedTrail()
	{
		_trail.Clear();
		Vector3 back = GetFollowBack();
		int samplesPerSlot = Mathf.Max(1, Mathf.RoundToInt(followSpacing / trailSampleDistance));
		int count = Mathf.Max(16, Puxirum.Count * samplesPerSlot + 8);
		for (int i = count - 1; i >= 0; i--)
			_trail.Add(player.GlobalPosition + back * trailSampleDistance * i);
	}

	private Vector3 GetTrailPoint(int followerIndex, int samplesPerSlot)
	{
		int stepsBack = (followerIndex + 1) * samplesPerSlot;
		int index = _trail.Count - 1 - stepsBack;
		if (index >= 0)
			return _trail[index];

		return player.GlobalPosition + GetFollowBack() * followSpacing * (followerIndex + 1);
	}

	private Vector3 GetFollowBack()
	{
		Vector3 horiz = player.GetHorizontalVelocity();
		if (horiz.LengthSquared() > 0.04f)
			return -horiz.Normalized();

		if (player.meshRoot != null)
		{
			Vector3 back = player.meshRoot.GlobalBasis.Z;
			back.Y = 0f;
			if (back.LengthSquared() > 0.0001f)
				return back.Normalized();
		}

		return Vector3.Back;
	}
}