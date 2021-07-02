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
        private int claimRewardsDurationInMS = 100 * 1000; // 0 would mean no rewards collectin

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
        public int ClaimRewardsDurationInMS
        {
            get { return claimRewardsDurationInMS; }
            set { claimRewardsDurationInMS = System.Math.Clamp(value, 0, int.MaxValue); }
        }
        public int ClaimRewardsDurationInHours
        {
            get { return (((claimRewardsDurationInMS / 1000) / 60) / 60); }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("TresholdForFaucetDrop: " + faucetDropTreshold.ToString() + " wei = " + Web3.Convert.FromWei(faucetDropTreshold).ToString());
            sb.AppendLine("DropAmount: " + faucetDropAmount.ToString() + " wei = " + Web3.Convert.FromWei(faucetDropAmount).ToString());
            sb.AppendLine("TresholdForClaimRewardsInDays: " + claimRewardsDurationInMS.ToString());

            return sb.ToString();
        }
    }
}
