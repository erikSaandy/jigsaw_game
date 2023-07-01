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

		public void AddEntry( string name, string message, string avatar, string lobbyState = null )
		{
			var e = Canvas.AddChild<ChatEntry>();

			e.Message.Text = message;
			e.NameLabel.Text = name;
			e.Avatar.SetTexture( avatar );

			e.SetClass( "noname", string.IsNullOrEmpty( name ) );
			e.SetClass( "noavatar", string.IsNullOrEmpty( avatar ) );

			if ( lobbyState == "ready" || lobbyState == "staging" )
			{
				e.SetClass( "is-lobby", true );
			}
		}


		[ConCmd.Client( "add_chat", CanBeCalledFromServer = true )]
		public static void AddChatEntry( string name, string message, string avatar = null, string lobbyState = null )
		{

			Current?.AddEntry( name, message, avatar, lobbyState );

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
				Texture t = await JigsawGame.Current.Task.RunInThreadAsync( () => ImageLoader.LoadWebImage( message ) );

				avatar = "icons/server.png";


				if(!ImageLoader.TextureIsValid(t, out string error))
				{
					// Texture is not valid!
					message = "URL is not valid! " + "(" + error + ")" + "\rPlease try another URL";
				}
				else
				{
					JigsawGame.Current.PuzzleTextureURL = message;
					JigsawGame.Current.GameState = new LoadingGameState();

					message = "Found a valid image! \rLoading...";
				}
			}

			//// todo - reject more stuff
			//if ( message.Contains( '\n' ) || message.Contains( '\r' ) )
			//	return;

			Log.Info( $"{ConsoleSystem.Caller}: {message}" );

			if ( avatar == "" ) avatar = $"avatar:{ConsoleSystem.Caller?.SteamId}";

			AddChatEntry( To.Everyone, ConsoleSystem.Caller?.Name ?? "[Server]", message, avatar );
		}

		private static bool MessageIsPuzzleURL(string message)
		{
			// Client pawn is current lobby leader.
			if ( JigsawGame.Current.Leader == ConsoleSystem.Caller.Client.Pawn )
			{
				Type gameState = typeof( VotingGameState ); //JigsawGame.Current.GameState?.GetType();

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
