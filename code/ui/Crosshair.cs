using Sandbox.UI;

public partial class Crosshair : Panel
{
	public static Crosshair Current;

	public Crosshair()
	{
		Current = this;
		StyleSheet.Load( "Resource/styles/crosshair.scss" );
	}
}
