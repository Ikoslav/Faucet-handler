using Nethereum.Web3;
using Newtonsoft.Json;
using System.Numerics;
using System.Text;

namespace FaucetHandler
{
    [JsonObject(MemberSerialization.Fields)]
    public class PersistenData
    {
        private BigInteger tresholdForFaucetDrop = Web3.Convert.ToWei("0,0025");
        private BigInteger faucetDropAmount = Web3.Convert.ToWei("0,01");
        private int claimRewardsDurationInMS = 100 * 1000; // 0 would mean no rewards collectin

        public BigInteger TresholdForFaucetDrop
        {
            get { return tresholdForFaucetDrop; }
            set { tresholdForFaucetDrop = value; }
        }
        public BigInteger DropAmount
        {
            get { return faucetDropAmount; }
            set { faucetDropAmount = value; }
        }
        public int ClaimRewardsDurationInMS
        {
            get { return claimRewardsDurationInMS; }
            set { claimRewardsDurationInMS = System.Math.Clamp(value, 0, int.MaxValue); }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("TresholdForFaucetDrop: " + tresholdForFaucetDrop.ToString() + " wei = " + Web3.Convert.FromWei(tresholdForFaucetDrop).ToString());
            sb.AppendLine("DropAmount: " + faucetDropAmount.ToString() + " wei = " + Web3.Convert.FromWei(faucetDropAmount).ToString());
            sb.AppendLine("TresholdForClaimRewardsInDays: " + claimRewardsDurationInMS.ToString());

            return sb.ToString();
        }
    }
}
