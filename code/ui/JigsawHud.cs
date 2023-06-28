using Sandbox;

public partial class JigsawHud : HudEntity<HudRootPanel>
{
	public JigsawHud()
	{
		Log.Error( "hud" );
	}
	//[ClientRpc]
	//public void OnPlayerDied( DeathmatchPlayer player )
	//{
	//	Game.AssertClient();
	//}

	//[ClientRpc]
	//public void ShowDeathScreen( string attackerName )
	//{
	//	Game.AssertClient();
	//}
}
