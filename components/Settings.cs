using Godot;

public static class Settings
{
	private const string ConfigPath = "user://settings.cfg";

	public static float masterVolume = 1f;
	public static float musicVolume = 1f;
	public static float sfxVolume = 1f;
	public static float mouseSensitivityScale = 1f;
	public static bool fullscreen = false;

	private static bool _loaded = false;

	public static void Load()
	{
		if (_loaded)
		{
			Apply();
			return;
		}

		ConfigFile config = new();
		if (config.Load(ConfigPath) == Error.Ok)
		{
			masterVolume = (float)config.GetValue("audio", "master", masterVolume);
			musicVolume = (float)config.GetValue("audio", "music", musicVolume);
			sfxVolume = (float)config.GetValue("audio", "sfx", sfxVolume);
			mouseSensitivityScale = (float)config.GetValue("input", "mouse_sensitivity", mouseSensitivityScale);
			fullscreen = (bool)config.GetValue("display", "fullscreen", fullscreen);
		}

		_loaded = true;
		Apply();
	}

	public static void Save()
	{
		ConfigFile config = new();
		config.SetValue("audio", "master", masterVolume);
		config.SetValue("audio", "music", musicVolume);
		config.SetValue("audio", "sfx", sfxVolume);
		config.SetValue("input", "mouse_sensitivity", mouseSensitivityScale);
		config.SetValue("display", "fullscreen", fullscreen);
		config.Save(ConfigPath);
	}

	public static void Apply()
	{
		SetBusVolume("Master", masterVolume);
		SetBusVolume("Music", musicVolume);
		SetBusVolume("SFX", sfxVolume);
		DisplayServer.WindowSetMode(fullscreen
			? DisplayServer.WindowMode.Fullscreen
			: DisplayServer.WindowMode.Windowed);
	}

	private static void SetBusVolume(string busName, float volume)
	{
		int index = AudioServer.GetBusIndex(busName);
		if (index < 0) return;
		AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Mathf.Max(volume, 0.0001f)));
	}
}
