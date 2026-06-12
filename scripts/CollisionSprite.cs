using Godot;

[Tool]
public partial class CollisionSprite : Sprite2D
{
    [Export]
    public float Epsilon = 4f;

    [Export]
    public float AlphaThreshold = 0.1f;

    [ExportToolButton("Bake Collision")]
    public Callable BakeButton => Callable.From(Bake);

    private const string BodyName = "BakedCollision";

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
            return;
        if (GetNodeOrNull<StaticBody2D>(BodyName) is { } body)
        {
            body.MouseEntered += OnMouseEnter;
            body.MouseExited += OnMouseExit;
        }
    }

    public void OnMouseExit()
    {
        GameManager.instance.MouseExit = true;
    }

    public void OnMouseEnter()
    {
        GD.Print("ENTERED");
        GameManager.instance.MouseEnter = true;
    }

    public void Bake()
    {
        GetNodeOrNull(BodyName)?.Free();
        if (Texture is null)
        {
            GD.PushWarning("CollisionSprite: no texture.");
            return;
        }

        var img = Texture.GetImage();
        if (img.IsCompressed())
            img.Decompress();

        var bmp = new Bitmap();
        bmp.CreateFromImageAlpha(img, AlphaThreshold);

        var root = GetTree().EditedSceneRoot;
        var body = new StaticBody2D
        {
            Name = BodyName,
            InputPickable = true,
            CollisionLayer = 2,
        };
        AddChild(body);
        body.Owner = root;

        var origin = Offset - (Centered ? (Vector2)img.GetSize() / 2f : Vector2.Zero);
        var rect = new Rect2I(Vector2I.Zero, img.GetSize());
        foreach (Vector2[] poly in bmp.OpaqueToPolygons(rect, Epsilon))
        {
            for (int i = 0; i < poly.Length; i++)
                poly[i] += origin;
            var occNode = new LightOccluder2D
            {
                Occluder = new OccluderPolygon2D { Polygon = poly },
            };
            var polyNode = new CollisionPolygon2D
            {
                Polygon = poly,
                BuildMode = CollisionPolygon2D.BuildModeEnum.Solids,
            };
            body.AddChild(polyNode);
            body.AddChild(occNode);
            polyNode.Owner = root;
            occNode.Owner = root;
        }
    }
}
