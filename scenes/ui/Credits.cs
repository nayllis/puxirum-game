using Godot;

public partial class Credits : Control
{
	public override void _Ready()
	{
		base._Ready();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetNode<Button>("Back").Pressed += OnBackPressed;
	}

	private void OnBackPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
	}
}
