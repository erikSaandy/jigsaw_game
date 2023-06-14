using Sandbox;
using System.ComponentModel;
using System.Numerics;

namespace Jigsaw;

public partial class JigsawPawn : AnimatedEntity
{
	[Net, Predicted]
	public Weapon ActiveWeapon { get; set; }

	[ClientInput]
	public Vector3 InputDirection { get; set; }

	[ClientInput]
	public Angles ViewAngles { get; set; }

	[Net, Predicted]
	public PuzzlePiece ActivePiece { get; set; } = null;

	/// <summary>
	/// Position a player should be looking from in world space.
	/// </summary>
	[Browsable( false )]
	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	/// <summary>
	/// Position a player should be looking from in local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Vector3 EyeLocalPosition { get; set; }

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity.
	/// </summary>
	[Browsable( false )]
	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity. In local to the entity coordinates.
	/// </summary>
	[Net, Predicted, Browsable( false )]
	public Rotation EyeLocalRotation { get; set; }

	public BBox Hull
	{
		get => new
		(
			new Vector3( -16, -16, 0 ),
			new Vector3( 16, 16, 64 )
		);
	}

	[BindComponent] public JigsawPawnController Controller { get; }
	[BindComponent] public JigsawPawnAnimator Animator { get; }

	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );

	/// <summary>
	/// Called when the entity is first created 
	/// </summary>
	public override void Spawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );
		Tags.Add( "player" );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	public void SetActiveWeapon( Weapon weapon )
	{
		ActiveWeapon?.OnHolster();
		ActiveWeapon = weapon;
		ActiveWeapon.OnEquip( this );
	}

	public void Respawn()
	{
		Components.Create<JigsawPawnController>();
		Components.Create<JigsawPawnAnimator>();

		SetActiveWeapon( new Fists() );
	}

	public void DressFromClient( IClient cl )
	{
		var c = new ClothingContainer();
		c.LoadFromClient( cl );
		c.DressEntity( this );
	}

	public override void Simulate( IClient cl )
	{
		SimulateRotation();
		Controller?.Simulate( cl );
		Animator?.Simulate();
		ActiveWeapon?.Simulate( cl );

		EyeLocalPosition = Vector3.Up * (64f * Scale);

		if ( Game.IsServer )
		{
			PuzzlePieceInput();
		}

		if ( Game.IsServer )
		{

			SimulateActivePiece();
		}

	}

	public override void BuildInput()
	{
		// Rotate active piece
		if ( !Input.Down( "attack2" ) )
		{
			InputDirection = Input.AnalogMove;
		}
		else if( Input.Pressed("attack2") )
		{
			InputDirection = Vector2.Zero;
		}

		if ( Input.StopProcessing )
			return;

		var look = Input.AnalogLook;

		if ( ViewAngles.pitch > 90f || ViewAngles.pitch < -90f )
		{
			look = look.WithYaw( look.yaw * -1f );
		}

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89f, 89f );
		viewAngles.roll = 0f;
		ViewAngles = viewAngles.Normal;

	}

	private void PuzzlePieceInput()
	{
		float rayMag = 256;

		if ( Input.Pressed( "use" ))
		{
			if ( ActivePiece != null )
			{
				ClearActivePiece();
				return;
			}

			TraceResult tr = Trace.Ray(EyePosition, EyePosition + (EyeRotation.Forward * rayMag) )
				.UseHitboxes()
				.WithTag("puzzlepiece")
				.Ignore(this)
				.Run();

			DebugOverlay.Line( EyePosition, EyePosition + EyeRotation.Forward * rayMag, 1, true);

			if (tr.Hit)
			{
				SetActivePiece( tr.Entity as PuzzlePiece );
			}

		}
	}


	private void SetActivePiece(PuzzlePiece piece)
	{
		ActivePiece = piece;
		ActivePiece.PhysicsEnabled = false;
		ActivePiece.Parent = this;
		Log.Error( "AP: " + ActivePiece.Name );
		ActivePiece.Rotation = new Rotation(
			ActivePiece.LocalRotation.x - (ActivePiece.Rotation.x % 15),
			ActivePiece.LocalRotation.y - (ActivePiece.Rotation.y % 15),
			ActivePiece.LocalRotation.z - (ActivePiece.Rotation.z % 15),
			ActivePiece.LocalRotation.w - (ActivePiece.Rotation.w % 15)
		);

	}

	private void ClearActivePiece()
	{
		ActivePiece.PhysicsEnabled = true;
		ActivePiece.Parent = null;
		ActivePiece = null;
	}
	private void SimulateActivePiece()
	{
		if ( ActivePiece == null ) return;
		ActivePiece.Position = EyePosition + (EyeRotation.Forward * 48) + (EyeRotation.Right * 32);

		// Rotate active piece
		if ( Input.Down( "attack2" ) && ActivePiece != null )
		{
			Vector2 deltaRot = new Vector2();
			if ( Input.Pressed( "Forward" ) ) deltaRot.y += 15;
			if ( Input.Pressed( "Backward" ) ) deltaRot.y -= 15;
			if ( Input.Pressed( "Right" ) ) deltaRot.y += 15;
			if ( Input.Pressed( "Left" ) ) deltaRot.x -= 15;
			//ActivePiece.Rotation = new Rotation( ActivePiece.Rotation.y + deltaRot.y, ActivePiece.Rotation.x + deltaRot.x, ActivePiece.Rotation.z, ActivePiece.Rotation.w );
		}
	}

	bool IsThirdPerson { get; set; } = false;

	public override void FrameSimulate( IClient cl )
	{
		SimulateRotation();

		Camera.Rotation = ViewAngles.ToRotation();
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

		Camera.FirstPersonViewer = this;
		Camera.Position = EyePosition;
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f )
	{
		return TraceBBox( start, end, Hull.Mins, Hull.Maxs, liftFeet );
	}

	public TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f )
	{
		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		var tr = Trace.Ray( start, end )
					.Size( mins, maxs )
					.WithAnyTags( "solid", "playerclip", "passbullets" )
					.Ignore( this )
					.Run();

		return tr;
	}

	protected void SimulateRotation()
	{
		EyeRotation = ViewAngles.ToRotation();
		Rotation = ViewAngles.WithPitch( 0f ).ToRotation();
	}
}
