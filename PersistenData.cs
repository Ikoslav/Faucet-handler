using Nethereum.Web3;
using System.Numerics;
using System.Text;

namespace FaucetHandler
{
    class PersistenData
    {
        public BigInteger TresholdForFaucetDrop = Web3.Convert.ToWei("0,0025");
        public BigInteger DropAmount = Web3.Convert.ToWei("0,01");
        public int TresholdForClaimRewardsInMilliseconds = 1 * 60 * 1000;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("TresholdForFaucetDrop: " + TresholdForFaucetDrop.ToString() + " wei = " + Web3.Convert.FromWei(TresholdForFaucetDrop).ToString());
            sb.AppendLine("DropAmount: " + DropAmount.ToString() + " wei = " + Web3.Convert.FromWei(DropAmount).ToString());
            sb.AppendLine("TresholdForClaimRewardsInDays: " + TresholdForClaimRewardsInMilliseconds.ToString());

            return sb.ToString();
        }
    }
}
