using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
namespace Jigsaw;

public partial class LeaderInfo : Panel
{

	[ConCmd.Client("leader_ui_display", CanBeCalledFromServer = true)]
	public static void Enable(bool enable)
	{
		if ( Current == null ) return;

		Current.Visible = enable;
	}

}
