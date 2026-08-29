using Godot;
using System;

public partial class GameManager : Node
{
	public struct RayCastResult
	{
		public bool hasHit;
		public Vector3 normal;
		public Vector3 hit;
		public Node3D collider;

		public RayCastResult()
		{
			hasHit = false;
			normal = Vector3.Zero;
			hit = Vector3.Zero;
			collider = null;
		}
	}

	private bool _isPaused = false;

	public override void _Ready()
	{
		base._Ready();
		ProcessMode = ProcessModeEnum.Always;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
		if (@event.IsActionPressed("game_pause")) Pause();
		if (@event.IsActionPressed("game_reload")) Reload();
    }


	public static float LerpDelta(float speed, double delta)
	{
		return 1f - Mathf.Exp(-speed * (float)delta);
	}

	public static CharacterBody3D FindBody(Node self)
	{
		Node node = self.GetParent();
		while (node != null)
		{
			if (node is CharacterBody3D body3D)
			{
				return body3D;
			}
			node = node.GetParent();
		}
		return null;
	}

	public void Reload()
	{
		GetTree().ReloadCurrentScene();
	}

	public void Pause()
	{
		_isPaused = !_isPaused;
		GetTree().Paused = _isPaused;
		Input.MouseMode = _isPaused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
	}

	public static Vector3 CalculateInstantVelocity(Vector3 previousPosition, Vector3 currentPosition, double delta)
	{
		return (currentPosition - previousPosition) / (float)delta;
	}

	public static RayCastResult TestRayCollisionPoint(Node3D source, Vector3 origin, Vector3 end,
	uint colMask = 1, Godot.Collections.Array<Rid> exceptions = default, bool areaColliding = false,
	bool bodyColliding = true)
	{
		PhysicsDirectSpaceState3D spaceState = source.GetWorld3D().DirectSpaceState;
		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(origin, end, colMask, exceptions);
		query.CollideWithAreas = areaColliding;
		query.CollideWithBodies = bodyColliding;

		Godot.Collections.Dictionary result = spaceState.IntersectRay(query);
		bool didHit = result.Count > 0;
		if (!didHit) return new RayCastResult();
		else return new RayCastResult
		{
		hasHit = didHit,
		normal = (Vector3)result["normal"],
		hit = (Vector3)result["position"],
		collider = (Node3D)(GodotObject)result["collider"]
        };
	}
}
