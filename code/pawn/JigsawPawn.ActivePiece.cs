using Saandy;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jigsaw;

public partial class JigsawPawn : AnimatedEntity
{
	public readonly string[] CollisionTags = { "default", "solid", "player" };
	public static readonly int MaxHeldDistance = 98;

	[Net, Predicted] public PuzzlePiece ActivePiece { get; set; } = null;

	[Predicted] public Particles HoldSplashParticle { get; set; }

	public readonly float SmoothTime = 0.5f;
	public Vector3 DampVelocity = Vector3.Zero;
	[Net] public Vector3 PositionOld { get; set; } = Vector3.Zero;
	[Net] public Vector3 PositionNew { get; set; } = Vector3.Zero;
	[Net, Predicted] public Vector3 PieceVelocity { get; set; } = Vector3.Zero;
	[Net] public Vector3 HeldOffset { get; set; } = Vector3.Zero;

	public readonly float YawSmoothTime = 0.5f;
	public float YawDampVelocity = 0;
	[Net] public Angles WantedAngleOffset { get; set; } = Angles.Zero;
	[Net] public float YawOld { get; set; } = 0;

}
