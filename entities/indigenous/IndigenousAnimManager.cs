using Godot;

public partial class IndigenousAnimManager : AnimationTree
{
	[Export] public IndigenousController body;
	[Export] public float walkBlendSpeed = 6f;
	[Export(PropertyHint.Range, "0, 20, 0.1")] public float blendSmoothing = 12f;
	[Export(PropertyHint.Range, "0.1, 3, 0.05")] public float minLocomotionPlaybackSpeed = 0.85f;
	[Export(PropertyHint.Range, "0.1, 3, 0.05")] public float maxLocomotionPlaybackSpeed = 1.25f;

	private AnimationPlayer _animPlayer;
	public AnimationNodeStateMachinePlayback movesetPlayback;
	public AnimationNodeStateMachinePlayback actionsPlayback;
	private string _actionNode = "";
	private float Speed => new Vector3(body.Velocity.X, 0f, body.Velocity.Z).Length();

	public override void _Ready()
	{
		Active = true;
		body ??= FindBody();
		_animPlayer = GetNodeOrNull<AnimationPlayer>("../AnimationPlayer");
		movesetPlayback = Get("parameters/moveset/playback").As<AnimationNodeStateMachinePlayback>();
		actionsPlayback = Get("parameters/actions/playback").As<AnimationNodeStateMachinePlayback>();

		if (body == null || _animPlayer == null || movesetPlayback == null)
		{
			GD.PushError($"{Name} needs an IndigenousController, AnimationPlayer, and parameters/moveset/playback.");
			SetProcess(false);
		}
	}

	public override void _Process(double delta)
	{
		if (body == null)
			return;

		UpdateWalkState();
		UpdateActionState();
	}

	public void UpdateWalkState()
	{
		float currBlend = Get("parameters/moveset/walking/blend_position").As<float>();
		float blend = Mathf.Lerp(currBlend, GetWalkBlend(), GameManager.LerpDelta(blendSmoothing, GetProcessDeltaTime()));
		Set("parameters/moveset/walking/blend_position", blend);

		if (movesetPlayback.GetCurrentNode() != "walking")
			return;

		_animPlayer.SpeedScale = Mathf.Lerp(
			minLocomotionPlaybackSpeed,
			maxLocomotionPlaybackSpeed,
			GetSpeedRatio()
		);
	}

	private void UpdateActionState()
	{
		string want = "idle";
		float blendTarget = 0f;
		if (body.Board.occupying)
		{
			want = "interact";
			blendTarget = 1f;
		}
		else if (body.trampolineArea != null && body.trampolineArea.Monitoring)
		{
			want = "trampoline";
			blendTarget = 1f;
		}

		float curr = Get("parameters/Blend2/blend_amount").As<float>();
		Set("parameters/Blend2/blend_amount", Mathf.Lerp(curr, blendTarget, GameManager.LerpDelta(blendSmoothing, GetProcessDeltaTime())));

		if (actionsPlayback == null || want == _actionNode)
			return;

		_actionNode = want;
		actionsPlayback.Travel(want);
	}

	private float GetSpeedRatio()
	{
		if (body == null)
			return 1f;

		float max = body.IsOnFloor() ? body.groundMaxSpeed : body.airMaxSpeed;
		return Mathf.Clamp(Speed / max, 0f, 1f);
	}

	private float GetWalkBlend()
	{
		return Mathf.Clamp(Speed / walkBlendSpeed, 0f, 1f);
	}

	private IndigenousController FindBody()
	{
		Node node = GetParent();
		while (node != null)
		{
			if (node is IndigenousController indigenous)
				return indigenous;

			node = node.GetParent();
		}

		return null;
	}
}
