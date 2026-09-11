using Godot;

public partial class Checkpoint : Area3D
{
	[Export] public RespawnController respawnController;
	[Export] public Marker3D spawnPoint;

	public override void _Ready()
	{
		base._Ready();
		BodyEntered += OnBodyEntered;
		if (respawnController == null)
			CallDeferred(MethodName.FindRespawnController);
	}

	public Vector3 GetSpawnPosition()
	{
		return spawnPoint != null ? spawnPoint.GlobalPosition : GlobalPosition;
	}

	private void FindRespawnController()
	{
		respawnController = GetTree().GetFirstNodeInGroup("respawn") as RespawnController;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is PlayerController)
			respawnController?.SetCheckpoint(this);
	}
}
