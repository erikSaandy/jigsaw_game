using System;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Jigsaw;

/// <summary>
/// This is the main base game
/// </summary>
[Title( "Game" ), Icon( "sports_esports" )]
public abstract class JigsawManager : BaseGameManager
{
	/// <summary>
	/// Currently active game entity.
	/// </summary>
	public static JigsawManager Current { get; protected set; }

	public JigsawManager()
	{
		Current = this;
	}

	/// <summary>
	/// Called when the game is shutting down.
	/// </summary>
	public override void Shutdown()
	{
		if ( Current == this )
			Current = null;
	}

	/// <summary>
	/// The player wants to enable the devcam. Probably shouldn't allow this
	/// unless you're in a sandbox mode or they're a dev.
	/// </summary>
	public virtual void DoPlayerDevCam( IClient client )
	{
		Game.AssertServer();

		var camera = client.Components.Get<DevCamera>( true );

		if ( camera == null )
		{
			camera = new DevCamera();
			client.Components.Add( camera );
			return;
		}

		camera.Enabled = !camera.Enabled;
	}

	/// <summary>
	/// Someone is speaking via voice chat. This might be someone in your game,
	/// or in your party, or in your lobby.
	/// </summary>
	public override void OnVoicePlayed( IClient cl )
	{
		cl.Voice.WantsStereo = true;
		VoiceList.Current?.OnVoicePlayed( cl.SteamId, cl.Voice.CurrentLevel );
	}

	[ConCmd.Server]
	public static async void CheckPuzzleCompletionRelative( PuzzlePiece piece )
	{
		PuzzlePiece root = piece.GetRoot();
		int c = root.Children.Count + 1;

		if( c == JigsawGame.Current.PieceCountX * JigsawGame.Current.PieceCountY)
		{
			JigsawGame.Current.GameState = new EndingGameState();
		}
	}

	/// <summary>
	/// Find a new client pawn that isn't the current leader.
	/// </summary>
	public static void GetNewGameLeader()
	{
		// Single player in lobby
		if ( Game.Clients.Count == 1 ) 
		{ 
			JigsawGame.Current.Leader = (JigsawPawn)Game.Clients.FirstOrDefault().Pawn;
		}
		else
		{
			// If more players, find new leader that isn't current leader.
			JigsawGame.Current.Leader = (JigsawPawn)Game.Clients.Where( (x => x.Pawn != JigsawGame.Current.Leader) ).OrderBy( x => Guid.NewGuid() ).First().Pawn;
		}

		ChatBox.SayInformation( JigsawGame.Current.Leader.Client.Name + " is now leader! \rSubmit an image before the time runs out." );

	}

}
