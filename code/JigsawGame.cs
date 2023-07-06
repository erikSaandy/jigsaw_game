using Microsoft.VisualBasic;
using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

	// GameState takes care of the basic game loop.
	[Net, Change] private BaseGameState gameState { get; set; }

	// Call OnExit() on server.
	public BaseGameState GameState { get { return gameState; } set { gameState?.OnExit(); gameState = value; } }

	public void OngameStateChanged( BaseGameState old, BaseGameState _new )
	{
		// Call OnExit() on client.
		old?.OnExit();
	}

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
		}

		if ( Game.IsClient )
		{
			new RootHud();
		}

	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		ChatBox.SayInformation( client.Name + " has joined the game." );

		// Create a pawn for this client to play with
		var pawn = new JigsawPawn();
		client.Pawn = pawn;
		pawn.UpdateClothes( client );

		//pawn.DressFromClient( client );

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

		// init gamestate when first client joined.
		if(GameState == null)
		{
			GameState = new VotingGameState();
		}

		Current.GameState?.ClientJoined( client );

		Current.ClientSpawned( To.Single( client ) );
	}

	[ClientRpc]
	public void ClientSpawned( )
	{
		Current.gameState?.ClientSpawned();
	}

	//public override void Spawn()
	//{
	//	base.Spawn();
	//	Current.GameState?.ClientSpawn();
	//}

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( cl, reason );
		Current.GameState?.ClientDisconnect( cl, reason );
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		Current.GameState?.Simulate( cl );

	}

}

public partial class BaseGameState : BaseNetworkable
{
	public BaseGameState()
	{

	}

	protected void WriteConsole(string state)
	{
		if ( Game.IsServer )
		{
			Log.Info( "________________________________________" );
			Log.Info( "[GameState] " + state );
			Log.Info( "________________________________________" );
		}
	}

	public virtual void Simulate( IClient cl ) 
	{
		//base.Simulate( cl );
	}

	/// <summary>
	/// Server-side
	/// </summary>
	/// <param name="client"></param>
	public virtual void ClientJoined( IClient client )
	{
	}

	/// <summary>
	/// Only spawned client-side on the client that spawned.
	/// </summary>
	public virtual void ClientSpawned()
	{

	}

	public virtual void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{

	}

	public virtual void OnExit()
	{

	}

}

public partial class VotingGameState : BaseGameState
{

	[Net] public TimeSince Timer { get; set; } = new TimeSince();
	private const int TimeLimit = 120;
	public float GetTimer(){ return TimeLimit - Timer; }

	public bool paused = false;

	public VotingGameState() : base()
	{
		WriteConsole( "Voting" );

		if ( Game.IsServer )
		{
			Timer = 0;
			if ( Game.Clients.Count > 0 )
			{
				// Find new leader.
				JigsawManager.GetNewGameLeader();
			}
		}

		if ( Game.IsClient )
		{
			VotingTimer.Current.Visible = true;
		}
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		if ( Game.IsServer )
		{
			if ( paused ) return;

			if ( GetTimer() <= 0 )
			{
				JigsawGame.Current.Leader = null;
				ChatBox.SayInformation( cl.Name + " is too slow! \rLet's find a new leader." );
				RestartVoting();
			}
			else
			{
				VotingTimer.SetTimer( TimeLimit - (JigsawGame.Current.GameState as VotingGameState).Timer );
			}
		}

	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		if ( Game.IsClient ) return;

		// Make sure late clients know who is leader and a vote is going on.
		if ( JigsawGame.Current.Leader != null && client != JigsawGame.Current.Leader?.Client )
		{
			ChatBox.SayInformation( To.Single( client ), 
				"Welcome " + client.Name + "!\r"+
				JigsawGame.Current.Leader.Client.Name + " is currently chosing a puzzle image. Please hold on!"
				);
		}
	}

	public override void ClientSpawned()
	{
		base.ClientSpawned();

		// If someone joins during subsequent voting state, when old puzzle is still in the world...
		// Spawn puzzle!

		if ( Game.IsClient && JigsawGame.Current.PieceEntities.Count > 0 )
		{
			JigsawGame.Current.LoadClientPieces();
		}
	}

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( cl, reason );

		if ( Game.IsServer )
		{
			if ( cl == JigsawGame.Current.Leader.Client )
			{
				ChatBox.SayInformation( "The leader left the game! \rLet's find a new leader." );
				RestartVoting();
			}
		}

	}

	private async void RestartVoting()
	{
		paused = true;
		int waitSeconds = 3;
		await Task.Delay( waitSeconds * 1000 );
		JigsawGame.Current.GameState = new VotingGameState();
	}

}

public partial class LoadingGameState : BaseGameState
{
	public LoadingGameState() : base()
	{
		WriteConsole( "Loading" );

		// NOTE: Game.IsClient doesn't work here. I guess server takes priority, and clients doesn't have a chance.

		if ( Game.IsServer )
		{
			Log.Info( "Loading server puzzles pieces..." );
			JigsawGame.Current.LoadEntities();
		}

	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );
	}

	public override void OnExit()
	{
		base.OnExit();

		if ( Game.IsClient )
		{
			VotingTimer.Current.Visible = false;
			JigsawGame.Current.LoadClientPieces();
		}
	}

}

public partial class PuzzlingGameState : BaseGameState
{
	public PuzzlingGameState() : base()
	{
		WriteConsole( "Puzzling" );

	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );
	}

	public override void ClientSpawned( )
	{
		base.ClientSpawned( );

		if ( Game.IsClient )
		{
			JigsawGame.Current.LoadClientPieces();
		}
	}

}

public partial class EndingGameState : BaseGameState
{

	public EndingGameState() : base()
	{
		WriteConsole( "Ending" );

		if ( Game.IsServer )
		{
			RestartGame();

			string plural = "";
			if(Game.Clients.Count > 1)
			{	
				plural = "s";
			}

			ChatBox.SayInformation( "You crazy bastard" + plural + ", you actually did it... \rCongratulations!" );
			Log.Warning( "Puzzle is finished! wooooo! Congratulations." );
		}
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );
	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

	}

	public override void ClientSpawned()
	{
		base.ClientSpawned();

		if ( Game.IsClient )
		{
			JigsawGame.Current.LoadClientPieces();
		}
	}

	private async void RestartGame()
	{
		int waitSeconds = 5;
		await Task.Delay( waitSeconds * 1000 );
		JigsawGame.Current.GameState = new VotingGameState();
	}

}
