using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System;
using System.Numerics;

namespace FaucetHandler
{
    public class ContractData
    {
        public BigInteger RewardsAmount_WEI { get; set; }
        public decimal RewardsAmount_ETH
        {
            get { return Web3.Convert.FromWei(RewardsAmount_WEI); }
        }

        public string FaucetOwner_ADDRESS { get; set; }
        public string FaucetTarget_ADDRESS { get; set; }

        public string FaucetHandler_ADDRESS { get; set; }

        public HexBigInteger FaucetTargetBalance_WEI { get; set; }
        public decimal FaucetTargetBalance_ETH
        {
            get { return Web3.Convert.FromWei(FaucetTargetBalance_WEI); }
        }

        public HexBigInteger BotWalletBalance_WEI { get; set; }
        public decimal BotWalletBalance_ETH
        {
            get { return Web3.Convert.FromWei(BotWalletBalance_WEI); }
        }


        public BigInteger DailyLimit_WEI { get; set; }
        public decimal DailyLimit_ETH
        {
            get { return Web3.Convert.FromWei(DailyLimit_WEI); }
        }

        public BigInteger FaucetFunds_WEI { get; set; }
        public decimal FaucetFunds_ETH
        {
            get { return Web3.Convert.FromWei(FaucetFunds_WEI); }
        }

        public BigInteger CooldownEnds_UNIX_SECONDS { get; set; }

        public long SecondsUntilCooldownEnds
        {
            get
            {
                long now_UNIX_SECONDS = (long)(DateTime.Now - DateTime.UnixEpoch).TotalSeconds;

                return Math.Clamp((long)CooldownEnds_UNIX_SECONDS - now_UNIX_SECONDS, 0, long.MaxValue);
            }
        }
    }
}
