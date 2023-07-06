using Sandbox;

namespace Jigsaw;
public partial class JigsawPawn
{
	public ClothingContainer Clothing { get; protected set; } = new();

	/// <summary>
	/// Set the clothes to whatever the player is wearing
	/// </summary>
	public void UpdateClothes( IClient cl )
	{
		Clothing ??= new();
		Clothing.LoadFromClient( cl );
		Clothing.DressEntity( this, true, true );
	}
}
