using Sandbox.UI.Construct;
using Sandbox;
using Sandbox.UI;

namespace NetBolt.Client.UI;

public class ClientChatBox : Panel
{
	public static ClientChatBox? Current;

	public Panel Canvas { get; protected set; }
	public TextEntry Input { get; protected set; }

	public ClientChatBox()
	{
		Current = this;

		StyleSheet.Load( "/UI/ChatBox/ClientChatBox.scss" );

		Canvas = Add.Panel( "chat_canvas" );

		Input = Add.TextEntry( "" );
		Input.AddEventListener( "onsubmit", Submit );
		Input.AddEventListener( "onblur", Close );
		Input.AcceptsFocus = true;
		Input.AllowEmojiReplace = true;
	}

	private void Open()
	{
		AddClass( "open" );
		Input.Focus();
	}

	private void Close()
	{
		RemoveClass( "open" );
		Input.Blur();
	}

	private void Submit()
	{
		Close();

		var msg = Input.Text.Trim();
		Input.Text = "";

		if ( string.IsNullOrWhiteSpace( msg ) )
			return;

		Say( msg );
	}

	[Event.BuildInput]
	private static void BuildInput( InputBuilder inputBuilder )
	{
		if ( inputBuilder.Pressed( InputButton.Chat ) )
			OpenChat();
	}

	public void AddEntry( string? name, string message, string? avatar, string? lobbyState = null )
	{
		var entry = Canvas.AddChild<ChatEntry>();

		entry.Message.Text = message;
		entry.NameLabel.Text = name;
		entry.Avatar.SetTexture( avatar );

		entry.SetClass( "noname", string.IsNullOrEmpty( name ) );
		entry.SetClass( "noavatar", string.IsNullOrEmpty( avatar ) );

		if ( lobbyState is "ready" or "staging" )
			entry.SetClass( "is-lobby", true );
	}
	
	public static void OpenChat()
	{
		Current?.Open();
	}
	
	public static void AddChatEntry( string name, string message, string? avatar = null, string? lobbyState = null )
	{
		Current?.AddEntry( name, message, avatar, lobbyState );
	}
	
	public static void AddInformation( string message, string? avatar = null )
	{
		Current?.AddEntry( null, message, avatar );
	}
	
	private static void Say( string message )
	{
		// TODO: Reject more stuff
		if ( message.Contains( '\n' ) || message.Contains( '\r' ) )
			return;

		//Current?.Task.RunInThreadAsync( () => NetworkManager.Instance?.SendToServer( new SayMessage( message ) ) );
		AddChatEntry( Local.Client.Name, message, $"avatar:{Local.PlayerId}" );
	}
}
