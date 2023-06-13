using Sandbox;
using Editor;
using System.Collections.Immutable;
using System;
using Saandy;

/// <summary>
/// This entity defines the spawn point of the player in first person shooter gamemodes.
/// </summary>

namespace Jigsaw;

[Model]
[Library( "piece_spawner" ), HammerEntity]
[Title( "Piece Spawner" ), Category( "Jigsaw" ), Icon( "place" )]
[EditorModel( "models/jigsaw_spawn/jigsaw_spawn.vmdl" )]
public class PieceSpawner : Entity
{

	[Property( Title = "Spawn Radius" )]
	[Range(0, 128, 1)]
	public int Radius { get; set; } = 32;

	/// <summary>
	/// Can multiple puzzle pieces spawn here?
	/// </summary>
	[Property( Title = "Multiple Spawns" )]
	public bool MultipleSpawns { get; set; } = true;
		

	public static void DrawGizmos( EditorContext context )
	{
		var pos = context.Target.GetProperty( "Position" ).GetValue<Vector3>();
		var radius = context.Target.GetProperty( "Radius" ).GetValue<float>();
		var multipleSpawns = context.Target.GetProperty( "MultipleSpawns" ).GetValue<bool>();
		var inner_radius = radius - 16f;


		Gizmo.Draw.Color = multipleSpawns? Color.Green : Color.Blue;
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
