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



        public PersistentData PersistenData { get; private set; }
        public ContractData ContractData { get; private set; } = new ContractData();



        private int SinceLastRewardsClaim_MS;

        private DiscordSocketClient _client;
        private CommandService _commandService;
        private CommandHandler _commandHandler;

        private Web3 _web3Endpoint;

        private string FAUCET_ABI = @"[{'inputs':[{'internalType':'address','name':'faucetOwner_','type':'address'},{'internalType':'address','name':'faucetHandler_','type':'address'},{'internalType':'address','name':'faucetTarget_','type':'address'},{'internalType':'uint256','name':'dailyLimit_','type':'uint256'},{'internalType':'address','name':'aaveLendingPool','type':'address'},{'internalType':'address','name':'aaveIncentivesController','type':'address'},{'internalType':'address','name':'aweth_','type':'address'},{'internalType':'address','name':'weth_','type':'address'}],'stateMutability':'nonpayable','type':'constructor'},{'stateMutability':'payable','type':'fallback'},{'inputs':[],'name':'aweth','outputs':[{'internalType':'contract IERC20','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'claimRewards','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'cooldownEnds','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'dailyLimit','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'doFaucetDrop','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'emergencyEtherTransfer','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'faucetFunds','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'faucetHandler','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'faucetOwner','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'faucetTarget','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'incentivesController','outputs':[{'internalType':'contract IAaveIncentivesController','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'lendingPool','outputs':[{'internalType':'contract ILendingPool','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'rewardsAmount','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'uint256','name':'newDailyLimit','type':'uint256'}],'name':'setDailyLimit','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'newFaucetHandler','type':'address'}],'name':'setFaucetHandler','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'newFaucetOwner','type':'address'}],'name':'setFaucetOwner','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'newFaucetTarget','type':'address'}],'name':'setFaucetTarget','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'token','type':'address'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'tokenTransfer','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'weth','outputs':[{'internalType':'contract IWETH','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'stateMutability':'payable','type':'receive'}]";

        private Contract Faucet_Contract;

        private ulong LastBotMessageID = 0;

        private Account acc;

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


        private void LoadPersistentData()
        {
            PersistenData = JsonUtil.ReadFromJsonFile<PersistentData>("Data");
        }
        public void SavePersistentData()
        {
            JsonUtil.WriteToJsonFile<PersistentData>("Data", PersistenData);
        }

        public async Task RefreshContractData()
        {
            var rewardsAmount_Func = Faucet_Contract.GetFunction("rewardsAmount");
            ContractData.RewardsAmount_WEI = await rewardsAmount_Func.CallAsync<BigInteger>();

            var faucetTarget_Func = Faucet_Contract.GetFunction("faucetTarget");
            ContractData.FaucetTarget_ADDRESS = await faucetTarget_Func.CallAsync<string>();

            ContractData.FaucetTargetBalance_WEI = await AddressBalance(ContractData.FaucetTarget_ADDRESS);

            var dailyLimit_Func = Faucet_Contract.GetFunction("dailyLimit");
            ContractData.DailyLimit_WEI = await dailyLimit_Func.CallAsync<BigInteger>();

            var faucetFunds_Func = Faucet_Contract.GetFunction("faucetFunds");
            ContractData.FaucetFunds_WEI = await faucetFunds_Func.CallAsync<BigInteger>();

            var faucetHandler_Func = Faucet_Contract.GetFunction("faucetHandler");
            ContractData.FaucetHandler_ADDRESS = await faucetHandler_Func.CallAsync<string>();

            var faucetOwner_Func = Faucet_Contract.GetFunction("faucetOwner");
            ContractData.FaucetOwner_ADDRESS = await faucetOwner_Func.CallAsync<string>();

            ContractData.BotWalletBalance_WEI = await AddressBalance(acc.Address);

            var cooldownEnds_Func = Faucet_Contract.GetFunction("cooldownEnds");
            ContractData.CooldownEnds_UNIX_SECONDS = await cooldownEnds_Func.CallAsync<BigInteger>();

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
            LoadPersistentData();

            // Prepare WEB3
            acc = new Account(FAUCET_HANDLER_PRIVATE_KEY, CHAIN_ID);
            _web3Endpoint = new Web3(acc, WEB3_ENDPOINT);

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

            await RefreshContractData();
            await RefreshInfo();

            while (true) // MAIN LOOP
            {
                await Task.Delay(REFRESH_MS);

                await RefreshContractData();

                if (PersistenData.RewardsClaimCooldown_MS > 0) // Do not claim if duration is 0
                {
                    SinceLastRewardsClaim_MS += REFRESH_MS;
                    if (SinceLastRewardsClaim_MS >= PersistenData.RewardsClaimCooldown_MS)
                    {
                        SinceLastRewardsClaim_MS = 0;
                        await ClaimRewards();
                    }
                }

                await DoFaucetDrop();

                await RefreshInfo();
            }
        }


        private async Task Ready()
        {
            await CleanChannel();
        }

        private async Task DoFaucetDrop()
        {
            if (ContractData.FaucetTargetBalance_WEI >= PersistenData.FaucetDropTreshold_WEI)
            {
                // Faucet target balance is above treshold - 
                return;
            }

            var doFaucetDrop_Func = Faucet_Contract.GetFunction("doFaucetDrop");

            HexBigInteger gasEstimate = await doFaucetDrop_Func.EstimateGasAsync();
            HexBigInteger gasPrice = await GetAvgGasPrice();

            if (ContractData.FaucetFunds_WEI < (gasEstimate.Value * gasPrice.Value + PersistenData.FaucetDropAmount_WEI))
            {
                Console.WriteLine("DoFaucetDrop - not enough funds in faucet!");
                return;
            }

            HexBigInteger hotWalletBalance = await AddressBalance(acc.Address);

            if (hotWalletBalance.Value < (gasEstimate.Value * gasPrice.Value))
            {
                Console.WriteLine("DoFaucetDrop - not enough funds for transaction!");
                return;
            }

            if (PersistenData.FaucetDropAmount_WEI < PersistenData.FaucetDropTreshold_WEI)
            {
                Console.WriteLine("DoFaucetDrop - drop amount is less than treshold!"); // Faucet drop would be triggered multiple times in a row!
                return;
            }

            if (ContractData.SecondsUntilCooldownEnds == 0)
            {
                Console.WriteLine("DoFaucetDrop - faucet is still on cooldown!");
                return;
            }
        }


        private async Task ClaimRewards()
        {
            var claimRewards_Func = Faucet_Contract.GetFunction("claimRewards");

            HexBigInteger gasEstimate = await claimRewards_Func.EstimateGasAsync();
            HexBigInteger gasPrice = await GetAvgGasPrice();

            if (ContractData.RewardsAmount_WEI < (gasEstimate.Value * gasPrice.Value))
            {
                Console.WriteLine("ClaimRewards - Reward is not bigger than gas fees!");
                return;
            }

            HexBigInteger hotWalletBalance = await AddressBalance(acc.Address);

            if (hotWalletBalance.Value < (gasEstimate.Value * gasPrice.Value))
            {
                Console.WriteLine("ClaimRewards - not enough funds for transaction!");
                return;
            }

            var txhash = await claimRewards_Func.SendTransactionAsync(acc.Address, gasEstimate, gasPrice, new HexBigInteger(BigInteger.Zero));

            Console.WriteLine("Claimed rewards: " + txhash);
        }

        public async Task RefreshInfo()
        {
            int msToNextClaim = PersistenData.RewardsClaimCooldown_MS - SinceLastRewardsClaim_MS;
            int hoursToNextClaim = ((msToNextClaim / 1000) / 60) / 60;

            StringBuilder sb = new StringBuilder();
            sb.Append($"```"); // CODE FORMATTING START

            sb.AppendLine($"Bot wallet                  : {acc.Address}");
            sb.AppendLine($"Bot wallet balance          :{string.Format("{0,15:N8} eth", ContractData.BotWalletBalance_ETH)}");

            sb.AppendLine($"");

            sb.AppendLine($"Faucet Contract             : {Faucet_Contract.Address}");
            sb.AppendLine($"Owner                       : {ContractData.FaucetOwner_ADDRESS}");
            sb.AppendLine($"Handler                     : {ContractData.FaucetHandler_ADDRESS}");
            sb.AppendLine($"Faucet Target               : {ContractData.FaucetTarget_ADDRESS}");

            sb.AppendLine($"");

            sb.AppendLine($"Faucet Funds                :{string.Format("{0,15:N8} eth", ContractData.FaucetFunds_ETH)}");
            sb.AppendLine($"Faucet Rewards              :{string.Format("{0,15:N8} eth", ContractData.RewardsAmount_ETH)}");

            sb.AppendLine($"");

            sb.AppendLine($"Faucet Target balance       :{string.Format("{0,15:N8} eth", ContractData.FaucetTargetBalance_ETH)}");
            sb.AppendLine($"Faucet drop treshold        :{string.Format("{0,15:N8} eth", PersistenData.FaucetDropTreshold_ETH)}");
            sb.AppendLine($"Faucet daily limit          :{string.Format("{0,15:N8} eth", ContractData.DailyLimit_ETH)}");

            if (PersistenData.FaucetDropAmount_WEI > ContractData.DailyLimit_WEI)
            {
                sb.AppendLine($"!!! Faucet drop amount is bigger than daily limit !!!");
            }
            else if (PersistenData.FaucetDropAmount_WEI < PersistenData.FaucetDropTreshold_WEI)
            {
                sb.AppendLine($"!!! Faucet drop amount is less than faucet drop treshold !!!");
            }
            else
            {
                sb.AppendLine($"Faucet drop amount          :{string.Format("{0,15:N8} eth", PersistenData.FaucetDropAmount_ETH)}");
            }

            sb.AppendLine($"Cooldown                    :{string.Format("{0,15:N0} s",  ContractData.SecondsUntilCooldownEnds)}");

            sb.AppendLine($"");

            sb.AppendLine($"Next rewards claim in       :{string.Format("{0,15:N0} ms", msToNextClaim)} = {hoursToNextClaim} hours");
            sb.AppendLine($"Rewards claim cooldown      :{string.Format("{0,15:N0} ms", PersistenData.RewardsClaimCooldown_MS)} = {PersistenData.RewardsClaimCooldown_H} hours");

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

        private Task<HexBigInteger> GetAvgGasPrice()
        {
            return _web3Endpoint.Eth.GasPrice.SendRequestAsync();
        }

        private Task<HexBigInteger> AddressBalance(string address)
        {
            return _web3Endpoint.Eth.GetBalance.SendRequestAsync(address);
        }
    }
}
