using Nethereum.Web3;
using Newtonsoft.Json;
using System;
using System.Numerics;

namespace FaucetHandler
{
    [JsonObject(MemberSerialization.Fields)]
    public class PersistentData
    {
        private const int RewardsClaimLowerLimit = 60000;
        private const int GasPriceLowerLimit = 1;

        private BigInteger faucetDropAmount_WEI = new BigInteger(10000000000000000);   // 0.01   eth
        private BigInteger faucetDropTreshold_WEI = new BigInteger(2500000000000000);  // 0.0025 eth

        // Note: In Milliseconds , 4days = 345600000
        private int rewardsClaimCooldown_MS = 345600000; // 0 would mean no rewards collecting

        private int gasPriceForClaimRewards_GWEI = 1;
        private int gasPriceForFaucetDrop_GWEI = 1;

        public int GasPriceForClaimRewards_GWEI
        {
            get { return gasPriceForClaimRewards_GWEI >= GasPriceLowerLimit ? gasPriceForClaimRewards_GWEI : GasPriceLowerLimit; }
            set { gasPriceForClaimRewards_GWEI = Math.Max(GasPriceLowerLimit, value); }
        }
        public int GasPriceForFaucetDrop_GWEI
        {
            get { return gasPriceForFaucetDrop_GWEI >= GasPriceLowerLimit ? gasPriceForFaucetDrop_GWEI : GasPriceLowerLimit; }
            set { gasPriceForFaucetDrop_GWEI = Math.Max(GasPriceLowerLimit, value); }
        }

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
            get { return rewardsClaimCooldown_MS >= RewardsClaimLowerLimit ? rewardsClaimCooldown_MS : RewardsClaimLowerLimit; }
            set { rewardsClaimCooldown_MS = Math.Clamp(value, RewardsClaimLowerLimit, int.MaxValue); }
        }
        public int RewardsClaimCooldown_H
        {
            get { return (((RewardsClaimCooldown_MS / 1000) / 60) / 60); }
        }
        public int RewardsClaimCooldown_S
        {
            get { return RewardsClaimCooldown_MS / 1000; }
        }
    }
}
