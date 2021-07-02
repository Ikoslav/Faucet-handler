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
            sb.AppendLine($"!setFaucetDropAmount [ETH]");
            sb.AppendLine($"!setFaucetDropTreshold [ETH]");
            sb.AppendLine($"!setClaimRewardsDuration [Milliseconds]");


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
                Program.Instance.PersistenData.FaucetDropAmount = newFaucetDropAmount;
                Program.Instance.SavePersistentData();
                return ReplyAsync("```Setting FaucetDropAmount to: " + newFaucetDropAmount + " wei = " + eth + " eth```");
            }
            catch
            {
                return ReplyAsync("```Wrong parameter!```");
            }
        }

        [Command("setFaucetDropTreshold")]
        [Summary("...")]
        public Task SetFaucetDropTreshold([Remainder][Summary("Value in ETH units")] string eth)
        {
            BigInteger newFaucetDropTreshold;

            try
            {
                newFaucetDropTreshold = Web3.Convert.ToWei(eth);
                Program.Instance.PersistenData.FaucetDropTreshold = newFaucetDropTreshold;
                Program.Instance.SavePersistentData();
                return ReplyAsync("```Setting FaucetDropTreshold to: " + newFaucetDropTreshold + " wei = " + eth + " eth```");
            }
            catch
            {
                return ReplyAsync("```Wrong parameter!```");
            }
        }

        [Command("setClaimRewardsDuration")]
        [Summary("...")]
        public Task SetClaimRewardsDuration([Remainder][Summary("Milliseconds")] string ms)
        {
            try
            {
                int msParsed = int.Parse(ms);
                int hours = (((msParsed / 1000) / 60) / 60);
              
                Program.Instance.PersistenData.ClaimRewardsDurationInMS = msParsed;
                Program.Instance.SavePersistentData();
                return ReplyAsync("```Setting ClaimRewardsDuration to: " + msParsed + " ms = " + hours + " hours```");
            }
            catch
            {
                return ReplyAsync("```Wrong parameter!```");
            }
        }
    }
}
