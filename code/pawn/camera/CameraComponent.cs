using Jigsaw;
using Sandbox;

namespace Jigsaw;

/// <summary>
/// Component designed for camera stuff, only 1 per pawn.
/// </summary>
public class CameraComponent : EntityComponent<JigsawPawn>, ISingletonComponent
{

	public virtual void Simulate( IClient cl )
	{

	}
	public virtual void FrameSimulate( IClient cl )
	{

	}
	public virtual void BuildInput()
	{

	}
}
