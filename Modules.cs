using Discord.Commands;
using Nethereum.Web3;
using System.Numerics;
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
            sb.AppendLine($"!setFaucetDropAmount");

            sb.Append($"```"); // CODE FORMATTING END

            return Context.Channel.SendMessageAsync(sb.ToString());
        }

        [Command("r")]
        [Summary("Refresh.")]
        public Task Refresh()
        {
            return Program.Instance.Refresh();
        }



        [Command("setFaucetDropAmount")]
        [Summary("...")]
        public Task SetFaucetDropAmount([Remainder][Summary("Value in ETH units")] string eth)
        {
            BigInteger newFaucetDropAmount;

            try
            {
                newFaucetDropAmount = Web3.Convert.ToWei(eth);
                Program.Instance.PersistenData.DropAmount = newFaucetDropAmount;
                Program.Instance.SavePersistentData();
                return ReplyAsync("```Setting FaucetDropAmount to: " + newFaucetDropAmount + " wei = " + eth + " eth```");
            }
            catch
            {
                return ReplyAsync("```Wrong parameter!```");
            }
        }
    }
}
