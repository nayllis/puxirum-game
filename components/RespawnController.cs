using Godot;

public partial class RespawnController : Node
{
	[Export] public PlayerController player;
	[Export] public GroupController groupController;
	[Export] public Marker3D defaultSpawn;
	[Export(PropertyHint.Range, ".5, 5, .1")] public float groupSpawnRadius = 1.5f;

	public Checkpoint ActiveCheckpoint { get; private set; }

	public override void _Ready()
	{
		base._Ready();
		AddToGroup("respawn");
	}

	public void SetCheckpoint(Checkpoint checkpoint)
	{
		ActiveCheckpoint = checkpoint;
	}

	public Vector3 GetRespawnPosition()
	{
		if (ActiveCheckpoint != null) return ActiveCheckpoint.GetSpawnPosition();
		if (defaultSpawn != null) return defaultSpawn.GlobalPosition;
		return player != null ? player.GlobalPosition : Vector3.Zero;
	}

	public void RespawnPlayer()
	{
		Vector3 spawn = GetRespawnPosition();
		if (player != null)
		{
			player.GlobalPosition = spawn;
			player.Velocity = Vector3.Zero;
		}

		if (groupController == null) return;
		int slot = 0;
		foreach (IndigenousController npc in groupController.Puxirum)
		{
			MoveToSpawn(npc, spawn + GetSlotOffset(slot));
			slot++;
		}
	}

	public void RespawnIndigenous(IndigenousController npc)
	{
		MoveToSpawn(npc, GetRespawnPosition());
	}

	private static void MoveToSpawn(IndigenousController npc, Vector3 position)
	{
		npc.GlobalPosition = position;
		npc.Velocity = Vector3.Zero;
	}

	private Vector3 GetSlotOffset(int slot)
	{
		float angle = slot * Mathf.Tau / 8f;
		return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * groupSpawnRadius;
	}
}
