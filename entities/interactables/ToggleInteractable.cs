using Godot;

public partial class ToggleInteractable : Interactable
{
	[Export] public bool lockAfterUse = true;

	[Signal]
	public delegate void ToggledEventHandler(bool isOn);

	public bool IsOn { get; private set; }

	public override int Remaining => (IsOn && lockAfterUse) ? 0 : base.Remaining;

	public override void NotifyArrived(IndigenousController npc)
	{
		base.NotifyArrived(npc);

		if (!IsSatisfied || IsOn)
			return;

		IsOn = true;
		EmitSignal((StringName)"Toggled", true);
		GD.Print($"{Name} toggled on");
		ReleaseAll();
	}
}
