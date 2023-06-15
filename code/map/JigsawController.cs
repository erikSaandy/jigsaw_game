using Sandbox;
using Editor;
using System.Collections.Immutable;
using System;
using Saandy;

/// <summary>
/// This entity defines the spawn point of the player in first person shooter gamemodes.
/// </summary>

namespace Jigsaw;

[EditorSprite("icons/icon_jigsaw_controller.png")]
[Library( "jigsaw_controller" ), HammerEntity]
[Title( "Jigsaw Controller" ), Category( "Jigsaw" ), Icon( "place" )]
public class JigsawController : Entity
{

	[Property( Title = "Puzzle Piece Scale" )]
	[Range(0, 128, 1)]
	public int PieceScale { get; set; } = 32;

	public override void Spawn()
	{
		Name = "Jigsaw Controller";
		base.Spawn();

	}

}
