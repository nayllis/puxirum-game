using Godot;
using System;
using System.Collections.Generic;

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

	public struct ShapeResult
	{
		public Node3D collider;
		public ulong colliderId;
		public Rid rid;
		public int shapeId;
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

	public static uint JoinPhysicsLayers(params uint[] layers)
	{
		uint mask = 0;
		if (layers == null) return mask;

		for (int i = 0; i < layers.Length; i++)
		{
			mask |= layers[i];
		}

		return mask;
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
		collider = result["collider"].As<Node3D>()
        };
	}

	public static ShapeResult[] TestShapeCollision(Node3D source, Vector3 position, Shape3D shape, uint colMask = 4u,
	Godot.Collections.Array<Rid> exceptions = default, bool areaColliding = false, bool bodyColliding = true, int maxResults = 32)
	{
		maxResults = Mathf.Max(maxResults, 1);
		PhysicsDirectSpaceState3D spaceState = source.GetWorld3D().DirectSpaceState;
		using PhysicsShapeQueryParameters3D query = new()
		{
			Shape = shape,
			CollisionMask = colMask,
			CollideWithAreas = areaColliding,
			CollideWithBodies = bodyColliding,
			Transform = new(Basis.Identity, position),
			Exclude = exceptions ?? []
		};

		Godot.Collections.Array<Godot.Collections.Dictionary> result = spaceState.IntersectShape(query, maxResults);
		ShapeResult[] hits = new ShapeResult[result.Count];
		for (int i = 0; i < result.Count; i++)
		{
			Godot.Collections.Dictionary hit = result[i];
			hits[i] = new ShapeResult
			{
				collider = hit["collider"].As<Node3D>(),
				colliderId = (ulong)hit["collider_id"],
				rid = (Rid)hit["rid"],
				shapeId = (int)hit["shape"]
			};
		}
		return hits;
	}
}
