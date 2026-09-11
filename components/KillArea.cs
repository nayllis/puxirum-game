using Godot;

public partial class KillArea : Area3D
{
	[Export] public RespawnController respawnController;

	public override void _Ready()
	{
		base._Ready();
		BodyEntered += OnBodyEntered;
		if (respawnController == null)
			CallDeferred(MethodName.FindRespawnController);
	}

	private void FindRespawnController()
	{
		respawnController = GetTree().GetFirstNodeInGroup("respawn") as RespawnController;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (respawnController == null) return;
		if (body is PlayerController)
			respawnController.RespawnPlayer();
		else if (body is IndigenousController npc)
			respawnController.RespawnIndigenous(npc);
	}
}
