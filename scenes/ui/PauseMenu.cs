using Godot;

public partial class PauseMenu : CanvasLayer
{
	[Export] public GameManager gameManager;

	private VBoxContainer _buttons;
	private SettingsMenu _settingsMenu;

	public override void _Ready()
	{
		base._Ready();
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;

		_buttons = GetNode<VBoxContainer>("Root/Buttons");
		_settingsMenu = GetNode<SettingsMenu>("Root/SettingsMenu");

		_buttons.GetNode<Button>("Continue").Pressed += OnContinuePressed;
		_buttons.GetNode<Button>("Settings").Pressed += OnSettingsPressed;
		_buttons.GetNode<Button>("Restart").Pressed += OnRestartPressed;
		_buttons.GetNode<Button>("MainMenu").Pressed += OnMainMenuPressed;
		_settingsMenu.Closed += OnSettingsClosed;
	}

	public void SetOpen(bool open)
	{
		Visible = open;
		if (!open)
		{
			_settingsMenu.Hide();
			_buttons.Show();
		}
	}

	private void OnContinuePressed()
	{
		gameManager.Pause();
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

	private void OnRestartPressed()
	{
		gameManager.Pause();
		gameManager.Reload();
	}

	private void OnMainMenuPressed()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
	}
}
