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
public class JigsawController : Entity
{

	
	[Property( Name = "piece_scale", Title = "Puzzle Piece Scale" )]
	public int PieceScale { get; set; } = 32;

	public override void Spawn()
	{
		Name = "Jigsaw Controller";
		base.Spawn();

	}

}
