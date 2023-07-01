using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace Jigsaw
{
	public partial class ChatBox : Panel
	{
		static ChatBox Current;

		public Panel Canvas { get; protected set; }
		public TextEntry Input { get; protected set; }

		public ChatBox()
		{
			Current = this;

			StyleSheet.Load( "/ui/chat/ChatBox.scss" );

			Canvas = Add.Panel( "chat_canvas" );

			Input = Add.TextEntry( "" );
			Input.AddEventListener( "onsubmit", () => Submit() );
			Input.AddEventListener( "onblur", () => Close() );
			Input.AcceptsFocus = true;
			Input.AllowEmojiReplace = true;
		}

		void Open()
		{
			AddClass( "open" );
			Input.Focus();
		}

		void Close()
		{
			RemoveClass( "open" );
			Input.Blur();
		}

		public override void Tick()
		{
			base.Tick();

			if ( Sandbox.Input.Pressed( "chat" ) )
			{
				Open();
			}
		}

		void Submit()
		{
			Close();

			var msg = Input.Text.Trim();
			Input.Text = "";

			if ( string.IsNullOrWhiteSpace( msg ) )
				return;


			SaySomething( msg );
		}

		public void AddEntry( string name, string message, string avatar, string lobbyState = null, bool isLeader = false )
		{

			var e = Canvas.AddChild<ChatEntry>();
			
			e.Message.Text = message;
			e.NameLabel.Text = name;
			e.Avatar.SetTexture( avatar );
			e.LeaderImage.SetTexture( "icons/leader.png" );

			e.SetClass( "noname", string.IsNullOrEmpty( name ) );
			e.SetClass( "noavatar", string.IsNullOrEmpty( avatar ) );
			e.SetClass( "noleader", !isLeader );
			e.SetClass( "leader", isLeader );


			if ( lobbyState == "ready" || lobbyState == "staging" )
			{
				e.SetClass( "is-lobby", true );
			}
		}


		[ConCmd.Client( "add_chat", CanBeCalledFromServer = true )]
		public static void AddChatEntry( string name, string message, string avatar = null, string lobbyState = null, bool isLeader = false )
		{

			Current?.AddEntry( name, message, avatar, lobbyState, isLeader );

			// Only log clientside if we're not the listen server host
			if ( !Game.IsListenServer )
			{
				Log.Info( $"{name}: {message}" );
			}
		}

		[ConCmd.Client( "addinfo_chat", CanBeCalledFromServer = true )]
		public static void AddInformation( string message, string avatar = null )
		{
			Current?.AddEntry( null, message, avatar );
		}

		[ConCmd.Server]	
		public static async void SaySomething( string message )
		{
			string avatar = "";
			if(MessageIsPuzzleURL( message ) )
			{
				(JigsawGame.Current.GameState as VotingGameState).paused = true;

				Texture t = await JigsawGame.Current.Task.RunInThreadAsync( () => ImageLoader.LoadWebImage( message ) );

				if(!ImageLoader.TextureIsValid(t, out string error))
				{
					// Texture is not valid!
					SayInformation( "URL is not valid! " + "(" + error + ")" + "\rPlease try another URL" );

					(JigsawGame.Current.GameState as VotingGameState).paused = false;
					return;
				}
				else
				{
					JigsawGame.Current.PuzzleTextureURL = message;
					JigsawGame.Current.GameState = new LoadingGameState();

					SayInformation( "Found a valid image! \rLet's get it up and running." );

					(JigsawGame.Current.GameState as VotingGameState).paused = false;
					JigsawGame.Current.Leader = null;
					return;
				}
			}

			//// todo - reject more stuff
			//if ( message.Contains( '\n' ) || message.Contains( '\r' ) )
			//	return;

			Log.Info( $"{ConsoleSystem.Caller}: {message}" );

			if ( avatar == "" ) avatar = $"avatar:{ConsoleSystem.Caller?.SteamId}";

			bool isLeader = ConsoleSystem.Caller == JigsawGame.Current.Leader?.Client;
			AddChatEntry( To.Everyone, ConsoleSystem.Caller?.Name ?? "[Server]", message, avatar, isLeader: isLeader);
		}

		[ConCmd.Server]
		public static void SayInformation( string message )
		{
			string avatar = "icons/info.png";
			AddChatEntry( To.Everyone, "", message, avatar );
		}

		private static bool MessageIsPuzzleURL(string message)
		{
			if ( ConsoleSystem.Caller == null ) return false;

			// Client pawn is current lobby leader.
			if ( JigsawGame.Current.Leader == ConsoleSystem.Caller.Client.Pawn )
			{
				// TODO: reset this. This is temporary for debugging purposes.
				//Type gameState = typeof( VotingGameState );
				Type gameState = JigsawGame.Current.GameState?.GetType();

				// Only accept URL during voting game state.
				if ( gameState == typeof( VotingGameState ) )
				{
					// Check if message is URL
					if ( ImageLoader.IsURL( message ) )
					{
						return true;
					}
				}
			}

			return false;

		}
	}
}
