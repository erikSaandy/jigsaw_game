using System;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Jigsaw;

/// <summary>
/// This is the main base game
/// </summary>
[Title( "Game" ), Icon( "sports_esports" )]
public partial class JigsawManager
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
	/// The player wants to enable the devcam. Probably shouldn't allow this
	/// unless you're in a sandbox mode or they're a dev.
	/// </summary>
	public virtual void DoPlayerDevCam( IClient client )
	{
		Game.AssertServer();
		Log.Error( "yo" );
		var camera = client.Components.Get<DevCamera>( true );

		if ( camera == null )
		{
			camera = new DevCamera();
			client.Components.Add( camera );
			return;
		}

		camera.Enabled = !camera.Enabled;
	}

	[ConCmd.Server]
	public static void CheckPuzzleCompletionRelative( PuzzlePiece piece )
	{

		PuzzlePiece root = piece.GetRoot();
		int c = root.Children.Count + 1;

		if( c == JigsawGame.Current.PieceCountX * JigsawGame.Current.PieceCountY)
		{
			if ( JigsawGame.Current.GameState.GetType() != typeof( PuzzlingGameState ) )
			{
				// This should only happen during debugging.
				Log.Error( "Completed puzzle during wrong gamestate." );
				return;
			}

			JigsawGame.Current.GameState = new EndingGameState();
			OnPuzzleCompletedClient( To.Everyone );
		}
	}

	[ConCmd.Client("on_puzzle_completed", CanBeCalledFromServer = true)]
	public static void OnPuzzleCompletedClient()
	{
		//Sandbox.Services.Stats.Increment( "completed_puzzle", 1 );
	}


	/// <summary>
	/// Find a new client pawn that isn't the current leader.
	/// </summary>
	public static void GetNewGameLeader()
	{
		LeaderInfo.Enable( To.Everyone, false );

		//LeaderInfo.Current.Visible = false;

		// Single player in lobby
		if ( Game.Clients.Count == 1 ) 
		{ 
			JigsawGame.Current.Leader = (JigsawPawn)Game.Clients.FirstOrDefault().Pawn;
			LeaderInfo.Enable( To.Single( JigsawGame.Current.Leader.Client ), true );
		}
		else
		{
			// If more players, find new leader that isn't current leader.
			JigsawGame.Current.Leader = (JigsawPawn)Game.Clients.Where( (x => x.Pawn != JigsawGame.Current.Leader) ).OrderBy( x => Guid.NewGuid() ).First().Pawn;
			LeaderInfo.Enable( To.Single(JigsawGame.Current.Leader.Client), true );
		}

		ChatBox.SayInformation( JigsawGame.Current.Leader.Client.Name + " is now leader! \rSubmit an image before the time runs out." );

	}

}
