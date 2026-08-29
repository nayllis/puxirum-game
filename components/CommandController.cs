using Godot;
using System;

public partial class CommandController : Node
{
	[Export] public PlayerController player;
	[Export] public GroupController groupController;
	[Export(PropertyHint.Range, "1, 300, .5")] public float maxCommandDistance = 150f;

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);
		if (@event.IsActionPressed("action_command")) GiveCommand();
	}

	private void GiveCommand()
	{
		Node3D camera = player.camBoom.camera;

		Vector3 origin = camera.GlobalPosition;
		Vector3 end = origin - camera.GlobalBasis.Z * maxCommandDistance;

		Godot.Collections.Array<Rid> exceptions = [player.GetRid()];
		exceptions.AddRange(groupController.PuxirumRid);

		GameManager.RayCastResult rr = GameManager.TestRayCollisionPoint(player,
		origin, end, exceptions: exceptions);

		if (rr.hasHit)
		{
			groupController.SetTarget(rr.hit);
			GD.Print(rr.hit);
		}
	}
}
