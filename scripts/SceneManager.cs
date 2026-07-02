using Godot;

public partial class SceneManager : Node
{
    private static SceneManager _instance;
    public static SceneManager Instance => _instance;

    public override void _Ready()
    {
        _instance = this;
    }

    public void GotoMain()
    {
        GetTree().ChangeSceneToFile("res://scenes/main.tscn");
    }

    public void GotoMenu()
    {
        GetTree().ChangeSceneToFile("res://scenes/menu.tscn");
    }
}
