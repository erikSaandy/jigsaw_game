using Sandbox;

namespace Jigsaw;

public partial class JigsawPawn
{
	[ConCmd.Admin( "noclip" )]
	static void DoPlayerNoclip()
	{
		if ( ConsoleSystem.Caller.Pawn is JigsawPawn basePlayer )
		{
			if ( basePlayer.MovementController is NoclipController )
			{
				basePlayer.Components.Add( new MovementController() );
			}
			else
			{
				basePlayer.Components.Add( new NoclipController() );
			}
		}
	}

	[ConCmd.Admin( "kill" )]
	static void DoPlayerSuicide()
	{
		if ( ConsoleSystem.Caller.Pawn is JigsawPawn basePlayer )
		{
			basePlayer.OnKilled();
		}
	}

	[ConCmd.Admin( "respawn" )]
	static void DoPlayerRespawn()
	{
		if ( ConsoleSystem.Caller.Pawn is JigsawPawn basePlayer )
		{
			basePlayer.Respawn();
		}
	}

	[ConCmd.Admin( "reset" )]
	static void DoGameReset()
	{
		JigsawGame.Current.GameState = new EndingGameState();
	}

	[ConCmd.Admin( "debug_overlay" )]
	static void ShowDebugInformation()
	{
		JigsawGame.Current.Debug = !JigsawGame.Current.Debug;
	}

	/// <summary>
	/// Enables the devcam. Input to the player will stop and you'll be able to freefly around.
	/// </summary>
	[ConCmd.Server( "devcam" )]
	static void DevcamCommand()
	{
		Log.Error( "hum" );
		if ( ConsoleSystem.Caller == null ) return;

		JigsawManager.Current?.DoPlayerDevCam( ConsoleSystem.Caller );
	}

}
