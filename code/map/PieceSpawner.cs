using Sandbox;
using Editor;
using System.Collections.Immutable;
using System;
using Saandy;

namespace Jigsaw;

[Model]
[Library( "piece_spawner" ), HammerEntity]
[Title( "Piece Spawner" ), Category( "Jigsaw" ), Icon( "place" )]
[EditorModel( "models/jigsaw_spawn/jigsaw_spawn.vmdl" )]

/// <summary>
/// This entity deines a custom spawn point for puzzle pieces.
/// Learn more about supporting Jigsaw Game by going to saandy.net/jigsaw
/// </summary>
public class PieceSpawner : Entity
{

	[Property( Title = "Spawn Radius" )]
	[Range(0, 128, 1)]
	public int Radius { get; set; } = 32;

	public static void DrawGizmos( EditorContext context )
	{
		var pos = context.Target.GetProperty( "Position" ).GetValue<Vector3>();
		var radius = context.Target.GetProperty( "Radius" ).GetValue<float>();
		var inner_radius = radius - 16f;


		Gizmo.Draw.Color = Color.Green;
		Gizmo.Draw.LineThickness = 1;
		Math2d.DrawCircleGizmo( pos, radius );
		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.LineThickness = 0.5f;
		Math2d.DrawCircleGizmo( pos, inner_radius );

	}

	public override void Spawn()
	{
		Name = "PieceSpawner";
		base.Spawn();

	}

}
