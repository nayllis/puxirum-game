using Godot;

public partial class CommandController : Node
{
	[Export] public PlayerController player;
	[Export] public GroupController groupController;
	[Export(PropertyHint.Range, "1, 300, .5")] public float maxCommandDistance = 150f;
	[Export(PropertyHint.Layers3DPhysics)] public uint interactableRayMask = 16u;
	[Export(PropertyHint.Range, "0.5, 8, .1")] public float interactablePickRadius = 3.5f;

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);
		if (@event.IsActionPressed("action_command")) GiveCommand();
		if (@event.IsActionPressed("action_clear_all_commands")) groupController.ResumeFollowAll();
	}

	private void GiveCommand()
	{
		Camera3D camera = player.camBoom.camera;
		Vector2 center = camera.GetViewport().GetVisibleRect().Size * 0.5f;
		Vector3 origin = camera.ProjectRayOrigin(center);
		Vector3 end = origin + camera.ProjectRayNormal(center) * maxCommandDistance;

		Godot.Collections.Array<Rid> exceptions = [player.GetRid()];
		exceptions.AddRange(groupController.PuxirumRid);

		GameManager.RayCastResult interactableRay = GameManager.TestRayCollisionPoint(
			player, origin, end, interactableRayMask, exceptions);

		Interactable interactable = FindInteractable(interactableRay.collider);
		if (interactable == null)
		{
			GameManager.RayCastResult worldRay = GameManager.TestRayCollisionPoint(
				player, origin, end, 1u, exceptions);
			if (!worldRay.hasHit)
				return;

			interactable = FindNearbyInteractable(worldRay.hit);
			if (interactable == null)
			{
				groupController.SetTarget(worldRay.hit);
				return;
			}
		}

		groupController.AssignTo(interactable);
	}

	private static Interactable FindInteractable(Node node)
	{
		while (node != null)
		{
			if (node is Interactable interactable)
				return interactable;
			node = node.GetParent();
		}

		return null;
	}

	private Interactable FindNearbyInteractable(Vector3 point)
	{
		float maxSq = interactablePickRadius * interactablePickRadius;
		Interactable best = null;
		float bestSq = maxSq;

		foreach (Node node in GetTree().GetNodesInGroup("interactable"))
		{
			if (node is not Interactable candidate)
				continue;

			Vector3 a = candidate.GlobalPosition;
			Vector3 b = point;
			a.Y = 0f;
			b.Y = 0f;
			float sq = a.DistanceSquaredTo(b);
			if (sq < bestSq)
			{
				bestSq = sq;
				best = candidate;
			}
		}

		return best;
	}
}
