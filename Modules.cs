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
            sb.AppendLine($"!setRewardsClaimCooldown [DAYS]");


            sb.Append($"```"); // CODE FORMATTING END

            return Context.Channel.SendMessageAsync(sb.ToString());
        }

        [Command("r")]
        [Summary("Refresh.")]
        public Task Refresh()
        {
            return Program.Instance.RefreshInfo();
        }



        [Command("setFaucetDropAmount")]
        [Summary("...")]
        public Task SetFaucetDropAmount([Remainder][Summary("Value in ETH units")] string eth)
        {
            BigInteger newFaucetDropAmount;

            try
            {
                newFaucetDropAmount = Web3.Convert.ToWei(eth);
                Program.Instance.PersistenData.FaucetDropAmount_WEI = newFaucetDropAmount;
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
                Program.Instance.PersistenData.FaucetDropTreshold_WEI = newFaucetDropTreshold;
                Program.Instance.SavePersistentData();
                return ReplyAsync("```Setting FaucetDropTreshold to: " + newFaucetDropTreshold + " wei = " + eth + " eth```");
            }
            catch
            {
                return ReplyAsync("```Wrong parameter!```");
            }
        }

        [Command("setRewardsClaimCooldown")]
        [Summary("...")]
        public Task SetClaimRewardsDuration([Remainder][Summary("Days")] string days)
        {
            try
            {
                int daysParsed = int.Parse(days);
                int convertedToMS = daysParsed * 24 * 60 * 60 * 1000;
              
                Program.Instance.PersistenData.RewardsClaimCooldown_MS = convertedToMS;
                Program.Instance.SavePersistentData();
                return ReplyAsync("```Setting RewardsClaimCooldown to: " + convertedToMS + " ms = " + Program.Instance.PersistenData.RewardsClaimCooldown_H + " hours```");
            }
            catch
            {
                return ReplyAsync("```Wrong parameter!```");
            }
        }
    }
}
