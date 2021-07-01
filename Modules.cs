using Discord.Commands;
using System.Text;
using System.Threading.Tasks;

namespace FaucetHandler
{
    public class Modules : ModuleBase<SocketCommandContext>
	{
		[Command("info")]
		[Summary("Infos.")]
		public Task Info()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append($"```"); // CODE FORMATTING START

		    sb.AppendLine($"!r - refresh");
		//	sb.AppendLine($"!setlp - to set lp");

			sb.Append($"```"); // CODE FORMATTING END

			return Context.Channel.SendMessageAsync(sb.ToString());
		}

        //[Command("setlp")]
        //[Summary("Sets new LP values.")]
        //public Task SetLP([Remainder][Summary("New LP Value")] float newLP)
        //      {
        //	Program.Instance.SetNewLP(newLP);
        //	return Program.Instance.RefreshCalculations();
        //      }

        [Command("r")]
        [Summary("Refresh.")]
        public Task Refresh()
        {
            return Program.Instance.Refresh();
        }
    }
}
