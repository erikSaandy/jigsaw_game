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
			basePlayer.TakeDamage( new DamageInfo { Damage = basePlayer.Health * 99 } );
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

}
