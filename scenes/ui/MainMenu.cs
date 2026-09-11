using Godot;

public partial class MainMenu : Control
{
	private VBoxContainer _buttons;
	private SettingsMenu _settingsMenu;

	public override void _Ready()
	{
		base._Ready();
		Settings.Load();
		Input.MouseMode = Input.MouseModeEnum.Visible;

		_buttons = GetNode<VBoxContainer>("Buttons");
		_settingsMenu = GetNode<SettingsMenu>("SettingsMenu");

		_buttons.GetNode<Button>("Play").Pressed += OnPlayPressed;
		_buttons.GetNode<Button>("Settings").Pressed += OnSettingsPressed;
		_buttons.GetNode<Button>("Credits").Pressed += OnCreditsPressed;
		_buttons.GetNode<Button>("Quit").Pressed += OnQuitPressed;
		_settingsMenu.Closed += OnSettingsClosed;
	}

	private void OnPlayPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/demo_level/demo_level.tscn");
	}

	private void OnSettingsPressed()
	{
		_buttons.Hide();
		_settingsMenu.Show();
	}

	private void OnSettingsClosed()
	{
		_buttons.Show();
	}

	private void OnCreditsPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/ui/credits.tscn");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
