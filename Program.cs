using CoinGecko.Clients;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FaucetHandler
{
    public class Program
    {
        public static Program Instance;

        // .ENV Variables
        private string WEB3_ENDPOINT;
        private string BOT_TOKEN;
        private ulong DISCORD_CHANNEL_ID;
        private string FAUCET_HANDLER_PRIVATE_KEY;
        private string FAUCET_CONTRACT_ADDRESS;
        private int REFRESH_MS;
        private int CHAIN_ID;

        private PersistenData persistenData;

        private int MillisecondsSinceLastRewardsClaim;

        private DiscordSocketClient _client;
        private CommandService _commandService;
        private CommandHandler _commandHandler;

        private Web3 _web3Endpoint;

        private string FAUCET_ABI = @"[{'inputs':[{'internalType':'address','name':'faucetOwner_','type':'address'},{'internalType':'address','name':'faucetHandler_','type':'address'},{'internalType':'address','name':'faucetTarget_','type':'address'},{'internalType':'uint256','name':'dailyLimit_','type':'uint256'},{'internalType':'address','name':'aaveLendingPool','type':'address'},{'internalType':'address','name':'aaveIncentivesController','type':'address'},{'internalType':'address','name':'aweth_','type':'address'},{'internalType':'address','name':'weth_','type':'address'}],'stateMutability':'nonpayable','type':'constructor'},{'stateMutability':'payable','type':'fallback'},{'inputs':[],'name':'aweth','outputs':[{'internalType':'contract IERC20','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'claimRewards','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'cooldownEnds','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'dailyLimit','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'doFaucetDrop','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'emergencyEtherTransfer','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'faucetFunds','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'faucetHandler','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'faucetOwner','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'faucetTarget','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'incentivesController','outputs':[{'internalType':'contract IAaveIncentivesController','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'lendingPool','outputs':[{'internalType':'contract ILendingPool','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'rewardsAmount','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'uint256','name':'newDailyLimit','type':'uint256'}],'name':'setDailyLimit','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'newFaucetHandler','type':'address'}],'name':'setFaucetHandler','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'newFaucetOwner','type':'address'}],'name':'setFaucetOwner','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'newFaucetTarget','type':'address'}],'name':'setFaucetTarget','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'token','type':'address'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'tokenTransfer','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'weth','outputs':[{'internalType':'contract IWETH','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'stateMutability':'payable','type':'receive'}]";

        private Contract Faucet_Contract;

        private ulong LastBotMessageID = 0;

        private Account faucetHandlerAccount;

        public static void Main(string[] args)
        {
            Instance = new Program();
            Instance.MainAsync().GetAwaiter().GetResult();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task MainAsync()
        {
            // ENVIROMENT VARIABLES
            DotNetEnv.Env.TraversePath().Load();

            WEB3_ENDPOINT = DotNetEnv.Env.GetString("WEB3_ENDPOINT");
            BOT_TOKEN = DotNetEnv.Env.GetString("BOT_TOKEN");
            DISCORD_CHANNEL_ID = ulong.Parse(DotNetEnv.Env.GetString("DISCORD_CHANNEL_ID"));
            FAUCET_HANDLER_PRIVATE_KEY = DotNetEnv.Env.GetString("FAUCET_HANDLER_PRIVATE_KEY");
            FAUCET_CONTRACT_ADDRESS = DotNetEnv.Env.GetString("FAUCET_CONTRACT_ADDRESS");

            REFRESH_MS = int.Parse(DotNetEnv.Env.GetString("REFRESH_MS"));

            CHAIN_ID = int.Parse(DotNetEnv.Env.GetString("CHAIN_ID"));

            // persistenData
            persistenData = JsonUtil.ReadFromJsonFile<PersistenData>("Data");

            // Prepare WEB3
            faucetHandlerAccount = new Account(FAUCET_HANDLER_PRIVATE_KEY, CHAIN_ID);
            _web3Endpoint = new Web3(faucetHandlerAccount, WEB3_ENDPOINT);

            Faucet_Contract = _web3Endpoint.Eth.GetContract(FAUCET_ABI, FAUCET_CONTRACT_ADDRESS);

            // Prepare DISCORD

            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.Ready += Ready;

            _commandService = new CommandService(new CommandServiceConfig
            {
                // Again, log level:
                LogLevel = LogSeverity.Info,

                // There's a few more properties you can set,
                // for example, case-insensitive commands.
                CaseSensitiveCommands = false,
            });

            _commandHandler = new CommandHandler(_client, _commandService, DISCORD_CHANNEL_ID);

            await _commandHandler.InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, BOT_TOKEN);
            await _client.StartAsync();


            //First refresh  importatnt if we have long refresh time.
            await Task.Delay(5000);
            await Refresh();

            while (true) // And we loop here
            {
                await Task.Delay(REFRESH_MS);

                MillisecondsSinceLastRewardsClaim += REFRESH_MS;
                if (MillisecondsSinceLastRewardsClaim >= persistenData.TresholdForClaimRewardsInMilliseconds)
                {
                    MillisecondsSinceLastRewardsClaim = 0;
                    await ClaimRewards();
                }

                await Refresh();
            }
        }

        private async Task Ready()
        {
            await CleanChannel();
        }

        private async Task ClaimRewards()
        {
            var rewardsAmount_Func = Faucet_Contract.GetFunction("rewardsAmount");
            BigInteger rewardsAmount = await rewardsAmount_Func.CallAsync<BigInteger>();

            Console.WriteLine("rewardsAmount: " + rewardsAmount);

            if (rewardsAmount <= BigInteger.Zero) return; // Note: If rewardsAmount is 0 transaction would revert.

            var claimRewards_Func = Faucet_Contract.GetFunction("claimRewards");

            HexBigInteger gasEstimate = await claimRewards_Func.EstimateGasAsync();
            HexBigInteger gasPrice = await GetAvgGasPrice(); 

            Console.WriteLine("gasEstimate: " + gasEstimate.Value);
            Console.WriteLine("gasPrice: " + gasPrice.Value);

            HexBigInteger hotWalletBalance = await AddressBalance(faucetHandlerAccount.Address);
          
            if (hotWalletBalance.Value < (gasEstimate.Value * gasPrice.Value)) return; // Not enough funds for transaction.
            
            var txhash = await claimRewards_Func.SendTransactionAsync(faucetHandlerAccount.Address, gasEstimate, gasPrice, new HexBigInteger(BigInteger.Zero));
          
            Console.WriteLine("Claimed rewards txhash: " + txhash);
        }

        private Task<HexBigInteger> GetAvgGasPrice()
        {
            return _web3Endpoint.Eth.GasPrice.SendRequestAsync();
        }

        private Task<HexBigInteger> AddressBalance(string address)
        {
            return _web3Endpoint.Eth.GetBalance.SendRequestAsync(address);
        }

        public async Task Refresh()
        {
            var faucetTarget_Func = Faucet_Contract.GetFunction("faucetTarget");
            string faucetTargetAddress = await faucetTarget_Func.CallAsync<string>();

            var faucetTargetBalance = await AddressBalance(faucetTargetAddress);
            var balanceInEth = Web3.Convert.FromWei(faucetTargetBalance.Value);

            // Time until next claim

            int msToNextClaim = persistenData.TresholdForClaimRewardsInMilliseconds - MillisecondsSinceLastRewardsClaim;
            int hoursToNextClaim = ((msToNextClaim / 1000) / 60) / 60;

            StringBuilder sb = new StringBuilder();
            sb.Append($"```"); // CODE FORMATTING START
            sb.AppendLine("FaucetTarget Balance: " + balanceInEth);
            sb.AppendLine("msToNextClaim : " + msToNextClaim);
            sb.AppendLine("hoursToNextClaim : " + hoursToNextClaim);

            sb.AppendLine("-- persistenData --");
            sb.AppendLine(persistenData.ToString());

            sb.Append($"```"); // CODE FORMATTING END

            await WriteToMyChannel(sb.ToString());
        }

        private async Task WriteToMyChannel(string text)
        {
            IMessageChannel channel = _client.GetChannel(DISCORD_CHANNEL_ID) as IMessageChannel;
            if (channel != null)
            {
                if (LastBotMessageID != 0)
                {
                    await channel.DeleteMessageAsync(LastBotMessageID);
                }

                var message = await channel.SendMessageAsync(text);
                LastBotMessageID = message.Id;
            }
        }

        private async Task CleanChannel()
        {
            IMessageChannel channel = _client.GetChannel(DISCORD_CHANNEL_ID) as IMessageChannel;
            if (channel != null)
            {
                var messages = await channel.GetMessagesAsync().FlattenAsync();

                foreach (var message in messages)
                {
                    await Task.Delay(1000); // Clean but do not spam One per second
                    await channel.DeleteMessageAsync(message.Id);
                }
            }
        }

        // TODO CHANING INTERNAL DATA


        //public void SetNewLP(float newLP)
        //{
        //    newLP = MathF.Max(0, newLP);

        //    Console.WriteLine("Setting new LP value to: " + newLP);

        //    LPTokens = newLP;
        //}
    }
}
