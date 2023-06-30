using Microsoft.VisualBasic;
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

//
// You don't need to put things in a namespace, but it doesn't hurt.
//

namespace Jigsaw;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class JigsawGame : GameManager
{

	public static new JigsawGame Current { get; protected set; }

	///// <summary>
	///// GameState lets the game and joining clients know what to do.
	///// </summary>
	[Net] private BaseGameState gameState { get; set; }
	public BaseGameState GameState { get { return gameState; } set { gameState = value; } }


	/// <summary>
	/// Server random.
	/// </summary>
	public static Random Rand { get; set; }

	[Net] public JigsawPawn Leader { get; set; }

	public JigsawGame()
	{
		Current = this;

		if ( Game.IsServer )
		{
			Leader = null;
			Rand = new Random();

			GameState = new VotingGameState();
		}


		if ( Game.IsClient )
		{
			// Create the HUD
			//Hud = new ExplorerHud();
			//Hud.HudInit();
			new RootHud();
		}

	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		var pawn = new JigsawPawn();
		client.Pawn = pawn;
		pawn.Respawn();
		pawn.DressFromClient( client );

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();
		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}

		// Make sure someone is leader.
		if ( Leader == null ) { Leader = pawn; }

		GameState?.ClientJoined( client );

	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		GameState?.Simulate( cl );

	}

}

public partial class BaseGameState : Entity
{

	protected void WriteConsole(string state)
	{
		if ( Game.IsServer )
		{
			Log.Info( "________________________________________" );
			Log.Info( "[GameState] " + state );
			Log.Info( "________________________________________" );
		}
	}

	public override void Simulate( IClient cl ) 
	{
		base.Simulate( cl );
	}

	public virtual void ClientJoined( IClient client )
	{

	}

	public BaseGameState()
	{

	}

}

public partial class VotingGameState : BaseGameState
{

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );
	}

	public VotingGameState() : base()
	{
		WriteConsole( "Voting" );

		if ( Game.IsServer )
		{
			//Log.Info( "waiting..." );

			//// Temporary...
			//Wait( 1 );
		}
	}
		
	//public async void Wait(int secs)
	//{
	//	await Task.Delay( secs * 1000 );
	//	JigsawGame.Current.GameState = new LoadingGameState();
	//}

}

public partial class LoadingGameState : BaseGameState
{

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );
	}
	public LoadingGameState() : base()
	{

		WriteConsole( "Loading" );

		if ( Game.IsServer )
		{
			// TODO: TIMER! But only if there are more than one player in the game.
			
			Log.Info( "Loading client meshes..." );
			JigsawGame.Current.PuzzleLoaderInit();
		}
	}

}

public partial class PuzzlingGameState : BaseGameState
{

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );
	}

	public PuzzlingGameState() : base()
	{
		WriteConsole( "Puzzling" );

		if ( Game.IsClient )
		{
			JigsawGame.Current.LoadClientPieces();
		}

		if(Game.IsServer)
		{
		}

	}

}

public partial class EndingGameState : BaseGameState
{

	public EndingGameState() : base()
	{

	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );
	}

}
