﻿using Sandbox;
using Sandbox.UI;
using System;

namespace Jigsaw;

partial class ViewModel : BaseViewModel
{
	public bool ShouldBob = true;
	float TargetRoll = 0f;
	float TargetFOV = 0f;
	Vector3 TargetPos = 0f;
	float WalkBob = 0f;
	float MyRoll = 0f;

	[Event.Client.PostCamera]
	private void PostCameraSetup()
	{
		if ( Game.LocalPawn is JigsawPawn pl && pl.MovementController is MovementController ctrl )
		{
			//ShouldBob = !ctrl.IsSliding && !ctrl.IsDashing;
			//TargetRoll = ctrl.IsSliding ? -45f : 0f;
			//TargetFOV = ctrl.IsSliding ? 80f : 75f;

			TargetRoll = 0f;
			TargetFOV = 75f;

			//TargetPos = TargetPos.LerpTo( Vector3.Up * (ctrl.IsSliding ? -15f : 0f), 2f * Time.Delta );
			Position += TargetPos;

			//switch ( ClientSettings.Current.WeaponPosition )
			//{
			//	case WeaponPositionSetting.Left:
			//		Position += Rotation.Left * 10;
			//		break;
			//	case WeaponPositionSetting.Center:
			//		break;
			//	case WeaponPositionSetting.Right:
			//		Position += Rotation.Right * 10;
			//		break;
			//}
		}

		AddCameraEffects();
	}

	private void AddCameraEffects()
	{
		if ( Game.LocalPawn.LifeState == LifeState.Dead ) return;
		//if ( !ClientSettings.Current.WalkBob ) return;

		// Slide Tilt
		//MyRoll = MyRoll.LerpTo( TargetRoll, Time.Delta * 10f );
		//Rotation *= Rotation.From( 0, 0, MyRoll );

		//
		// Bob up and down based on our walk movement
		//
		var speed = Owner.Velocity.Length.LerpInverse( 0, 400 );
		var left = Camera.Rotation.Left;
		var up = Camera.Rotation.Up;

		if ( ShouldBob && Owner.GroundEntity != null )
		{
			WalkBob += Time.Delta * 25.0f * speed;
		}

		Position += up * MathF.Sin( WalkBob ) * speed * -1;
		Position += left * MathF.Sin( WalkBob * 0.5f ) * speed * -0.5f;

		var uitx = new Sandbox.UI.PanelTransform();
		uitx.AddTranslateY( MathF.Sin( WalkBob * 1.0f ) * speed * -4.0f );
		uitx.AddTranslateX( MathF.Sin( WalkBob * 0.5f ) * speed * -3.0f );

		RootHud.Current.Style.Transform = uitx;
	}
}
