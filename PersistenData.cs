using Nethereum.Web3;
using Newtonsoft.Json;
using System.Numerics;
using System.Text;

namespace FaucetHandler
{
    [JsonObject(MemberSerialization.Fields)]
    public class PersistenData
    {
        private BigInteger faucetDropAmount = Web3.Convert.ToWei("0,01");
        private BigInteger faucetDropTreshold = Web3.Convert.ToWei("0,0025");

        // Note: In Milliseconds
        private int rewardsClaimCooldown = 100 * 1000; // 0 would mean no rewards collecting

        public BigInteger FaucetDropAmount
        {
            get { return faucetDropAmount; }
            set { faucetDropAmount = value; }
        }
        public BigInteger FaucetDropTreshold
        {
            get { return faucetDropTreshold; }
            set { faucetDropTreshold = value; }
        }
        public int RewardsClaimCooldown
        {
            get { return rewardsClaimCooldown; }
            set { rewardsClaimCooldown = System.Math.Clamp(value, 0, int.MaxValue); }
        }
        public int ClaimRewardsDurationInHours
        {
            get { return (((rewardsClaimCooldown / 1000) / 60) / 60); }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Faucet drop amount     :{ string.Format("{0,15:N8} eth", Web3.Convert.FromWei(faucetDropAmount))}");
            sb.AppendLine($"Faucet drop treshold   :{ string.Format("{0,15:N8} eth", Web3.Convert.FromWei(faucetDropTreshold))}");
            sb.AppendLine($"Rewards claim cooldown :{ string.Format("{0,15:N0} ms", RewardsClaimCooldown)} = {ClaimRewardsDurationInHours} hours");

            return sb.ToString();
        }
    }
}
