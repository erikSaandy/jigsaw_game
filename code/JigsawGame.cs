﻿using Microsoft.VisualBasic;
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
	[Net] public BaseGameState GameState { get; set; }

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
			new RootHud();
			//new VotingTimer();
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

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( cl, reason );
		GameState?.ClientDisconnect( cl, reason );
	}

	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		GameState?.Simulate( cl );

	}

}

public partial class BaseGameState : BaseNetworkable
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

	public virtual void Simulate( IClient cl ) 
	{
		//base.Simulate( cl );
	}

	public virtual void ClientJoined( IClient client )
	{
	}

	public virtual void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{

	}

	public BaseGameState()
	{

	}

}

public partial class VotingGameState : BaseGameState
{

	[Net] public TimeSince Timer { get; set; } = new TimeSince();
	private const int TimeLimit = 90;
	public float GetTimer(){ return TimeLimit - Timer; }

	public bool paused = false;

	[ClientRpc]
	public static void UpdateTimer()
	{
		VotingTimer.Current?.SetTimer( TimeLimit - (JigsawGame.Current.GameState as VotingGameState).Timer );
	}

	public VotingGameState() : base()
	{
		WriteConsole( "Voting" );

		if ( Game.IsServer )
		{

			Timer = 0;
			if ( Game.Clients.Count > 0 )
			{
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
				UpdateTimer();
			}
		}

	}

	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );
	}
	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		base.ClientDisconnect( cl, reason );

		if ( Game.IsServer )
		{
			// If current leader left game, restart with new leader.
			if ( cl.Pawn == JigsawGame.Current.Leader )
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
			// TODO: TIMER! But only if there are more than one player in the game.

			Log.Info( "Loading client meshes..." );
			JigsawGame.Current.PuzzleLoaderInit();
		}

		VotingTimer.Current.Visible = false;
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

public partial class PuzzlingGameState : BaseGameState
{
	public PuzzlingGameState() : base()
	{
		WriteConsole( "Puzzling" );

		if ( Game.IsClient )
		{
			VotingTimer.Current.Visible = false;
			JigsawGame.Current.LoadClientPieces();
		}

		if ( Game.IsServer )
		{
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

}

public partial class EndingGameState : BaseGameState
{

	public EndingGameState() : base()
	{
		WriteConsole( "Ending" );
		Log.Warning( "Puzzle is finished! wooooo! Congratulations." );

		if ( Game.IsServer )
		{
			RestartGame();
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

	private async void RestartGame()
	{
		int waitSeconds = 5;
		await Task.Delay( waitSeconds * 1000 );
		JigsawGame.Current.GameState = new VotingGameState();
	}

}
