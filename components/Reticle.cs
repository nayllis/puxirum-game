using Godot;

public partial class Reticle : Control
{
	[Export] public float radius = 3.5f;
	[Export] public Color fill = new(1f, 1f, 1f, 0.92f);
	[Export] public Color outline = new(0f, 0f, 0f, 0.55f);
	[Export] public float outlineWidth = 1.5f;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
		GrowHorizontal = GrowDirection.Both;
		GrowVertical = GrowDirection.Both;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized)
			QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 center = Size * 0.5f;
		DrawCircle(center, radius + outlineWidth, outline);
		DrawCircle(center, radius, fill);
	}
}
