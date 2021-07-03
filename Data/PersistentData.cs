using Nethereum.Web3;
using Newtonsoft.Json;
using System.Numerics;

namespace FaucetHandler
{
    [JsonObject(MemberSerialization.Fields)]
    public class PersistentData
    {
        private BigInteger faucetDropAmount_WEI = Web3.Convert.ToWei("0,01");
        private BigInteger faucetDropTreshold_WEI = Web3.Convert.ToWei("0,0025");

        // Note: In Milliseconds
        private int rewardsClaimCooldown_MS = 100 * 1000; // 0 would mean no rewards collecting

        public BigInteger FaucetDropAmount_WEI
        {
            get { return faucetDropAmount_WEI; }
            set { faucetDropAmount_WEI = value; }
        }
        public decimal FaucetDropAmount_ETH
        {
            get { return Web3.Convert.FromWei(faucetDropAmount_WEI); }
        }

        public BigInteger FaucetDropTreshold_WEI
        {
            get { return faucetDropTreshold_WEI; }
            set { faucetDropTreshold_WEI = value; }
        }
        public decimal FaucetDropTreshold_ETH
        {
            get { return Web3.Convert.FromWei(faucetDropTreshold_WEI); }
        }
        public int RewardsClaimCooldown_MS
        {
            get { return rewardsClaimCooldown_MS; }
            set { rewardsClaimCooldown_MS = System.Math.Clamp(value, 0, int.MaxValue); }
        }
        public int RewardsClaimCooldown_H
        {
            get { return (((rewardsClaimCooldown_MS / 1000) / 60) / 60); }
        }
    }
}
