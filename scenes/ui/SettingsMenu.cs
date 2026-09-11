using Godot;

public partial class SettingsMenu : Control
{
	[Signal]
	public delegate void ClosedEventHandler();

	private HSlider _masterSlider;
	private HSlider _musicSlider;
	private HSlider _sfxSlider;
	private HSlider _sensitivitySlider;
	private CheckButton _fullscreenToggle;
	private Button _backButton;

	public override void _Ready()
	{
		base._Ready();
		_masterSlider = GetNode<HSlider>("Center/Panel/Margin/Options/Master/Slider");
		_musicSlider = GetNode<HSlider>("Center/Panel/Margin/Options/Music/Slider");
		_sfxSlider = GetNode<HSlider>("Center/Panel/Margin/Options/Sfx/Slider");
		_sensitivitySlider = GetNode<HSlider>("Center/Panel/Margin/Options/Sensitivity/Slider");
		_fullscreenToggle = GetNode<CheckButton>("Center/Panel/Margin/Options/Fullscreen");
		_backButton = GetNode<Button>("Center/Panel/Margin/Options/Back");

		_masterSlider.Value = Settings.masterVolume;
		_musicSlider.Value = Settings.musicVolume;
		_sfxSlider.Value = Settings.sfxVolume;
		_sensitivitySlider.Value = Settings.mouseSensitivityScale;
		_fullscreenToggle.ButtonPressed = Settings.fullscreen;

		_masterSlider.ValueChanged += OnMasterChanged;
		_musicSlider.ValueChanged += OnMusicChanged;
		_sfxSlider.ValueChanged += OnSfxChanged;
		_sensitivitySlider.ValueChanged += OnSensitivityChanged;
		_fullscreenToggle.Toggled += OnFullscreenToggled;
		_backButton.Pressed += OnBackPressed;
	}

	private void OnMasterChanged(double value)
	{
		Settings.masterVolume = (float)value;
		Settings.Apply();
	}

	private void OnMusicChanged(double value)
	{
		Settings.musicVolume = (float)value;
		Settings.Apply();
	}

	private void OnSfxChanged(double value)
	{
		Settings.sfxVolume = (float)value;
		Settings.Apply();
	}

	private void OnSensitivityChanged(double value)
	{
		Settings.mouseSensitivityScale = (float)value;
	}

	private void OnFullscreenToggled(bool toggled)
	{
		Settings.fullscreen = toggled;
		Settings.Apply();
	}

	private void OnBackPressed()
	{
		Settings.Save();
		Hide();
		EmitSignal(SignalName.Closed);
	}
}
