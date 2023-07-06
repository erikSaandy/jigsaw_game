﻿using Sandbox;

namespace Jigsaw;

/// <summary>
/// Component designed for animation stuff, only 1 per pawn.
/// </summary>
public class AnimationComponent : EntityComponent<JigsawPawn>, ISingletonComponent
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
