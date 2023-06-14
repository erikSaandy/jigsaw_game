using Sandbox;

namespace Jigsaw;

public partial class ViewModel : BaseViewModel
{
	protected Weapon Weapon { get; init; }

	public ViewModel( Weapon weapon )
	{
		Weapon = weapon;
		EnableShadowCasting = false;
		EnableViewmodelRendering = true;
	}

	public override void PlaceViewmodel()
	{
		base.PlaceViewmodel();

		Camera.Main.SetViewModelCamera( 80f, 1, 500 );
	}
}
