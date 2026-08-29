using Godot;

public partial class PlayerAnimManager : AnimationTree
{
	[Export] public PlayerController body;
	[Export] public float walkBlendSpeed = 6f;
	[Export(PropertyHint.Range, "0, 20, 0.1")] public float blendSmoothing = 12f;
	[Export(PropertyHint.Range, "0.1, 3, 0.05")] public float minLocomotionPlaybackSpeed = 0.85f;
	[Export(PropertyHint.Range, "0.1, 3, 0.05")] public float maxLocomotionPlaybackSpeed = 1.25f;
	[Export(PropertyHint.Range, "0.1, 3, 0.05")] public float airPlaybackSpeed = 1f;

	private AnimationPlayer _animPlayer;
	public AnimationNodeStateMachinePlayback movesetPlayback;
	private float _blend;
	private bool _usingAirState;
	private float Speed => new Vector3(body.Velocity.X, 0f, body.Velocity.Z).Length();

	public override void _Ready()
	{
		Active = true;
		body ??= FindBody();
		_animPlayer = GetNodeOrNull<AnimationPlayer>("../AnimationPlayer");
		movesetPlayback = Get("parameters/moveset/playback").As<AnimationNodeStateMachinePlayback>();

		if (body == null || _animPlayer == null || movesetPlayback == null)
		{
			GD.PushError($"{Name} needs a CharacterBody3D, AnimationPlayer, and parameters/moveset/playback.");
			SetProcess(false);
		}
	}

	public override void _Process(double delta)
	{
		if (body == null)
		{
			return;
		}

		UpdateWalkState();
	}

	public void UpdateWallRunSide(float side)
	{
		float anim = side > 0 ? 1 : -1;
		Set("parameters/moveset/wall_run/blend_position", anim);
	}

	public void UpdateWalkState()
	{
		float currBlend = Get("parameters/moveset/walking/blend_position").As<float>();
		float blend = Mathf.Lerp(currBlend, GetWalkBlend(), GameManager.LerpDelta(blendSmoothing, GetProcessDeltaTime()));
		Set("parameters/moveset/walking/blend_position", blend);

		if (movesetPlayback.GetCurrentNode() != "walking") return;

		_animPlayer.SpeedScale = Mathf.Lerp(
			minLocomotionPlaybackSpeed,
			maxLocomotionPlaybackSpeed,
			GetSpeedRatio()
		);
	}

	private float GetSpeedRatio()
	{
		if (body == null) return 1;

		return Mathf.Clamp(Speed / GetCurrentMaxSpeed(), 0f, 1f);
	}

	private float GetWalkBlend()
	{
		return Mathf.Clamp(Speed / walkBlendSpeed, 0f, 1f);
	}

	private float GetCurrentMaxSpeed()
	{
		if (body is PlayerController player)
		{
			return player.IsOnFloor() ? player.groundMaxSpeed : player.airMaxSpeed;
		}

		return walkBlendSpeed;
	}

	private PlayerController FindBody()
	{
		Node node = GetParent();
		while (node != null)
		{
			if (node is PlayerController player)
			{
				return player;
			}

			node = node.GetParent();
		}

		return null;
	}
}
