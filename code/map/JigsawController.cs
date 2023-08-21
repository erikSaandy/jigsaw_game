using Sandbox;
using Editor;
using System.Collections.Immutable;
using System;
using Saandy;

/// <summary>
/// This entity defines the spawn point of the player in first person shooter gamemodes.
/// </summary>

namespace Jigsaw;

//[EditorSprite( "icons/jigsaw_controller/jigsaw_controller" )]
[Library( "jigsaw_controller" ), HammerEntity]
[Title( "Jigsaw Controller" ), Category( "Jigsaw" ), Icon( "place" )]
//[EditorSprite( "editor/icon_jigsaw_controller.vmat" )]
public class JigsawController : Entity
{

	
	[Property( Name = "piece_scale", Title = "Puzzle Piece Scale" )]
	public int PieceScale { get; set; } = 24;

	[Property( "Spawn puzzle pieces pround the center of the map?", Name = "spawn_around_center", Title = "Spawn Around Center" )]
	public bool SpawnAroundCenter { get; set; } = false;

	public override void Spawn()
	{
		Name = "Jigsaw Controller";
		base.Spawn();

	}

}
