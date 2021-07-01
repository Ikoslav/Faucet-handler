using CoinGecko.Clients;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Nethereum.Contracts;
using Nethereum.Web3;
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
        private string WEB3Endpoint;
        private string BotToken;
        private ulong ChannelID;
        private string FAUCET_HANDLER_PRIVATE_KEY;
        private string FaucetAddress;
        private int RefreshTimeInMilliseconds;

        private PersistenData persistenData;

        private int MillisecondsUntilNextClaimReward;

        private DiscordSocketClient _client;
        private CommandService _commandService;
        private CommandHandler _commandHandler;

        private Web3 _web3Endpoint;

        private string FAUCET_ABI = @"[{'inputs':[{'internalType':'address','name':'faucetOwner_','type':'address'},{'internalType':'address','name':'faucetHandler_','type':'address'},{'internalType':'address','name':'faucetTarget_','type':'address'},{'internalType':'uint256','name':'dailyLimit_','type':'uint256'},{'internalType':'address','name':'aaveLendingPool','type':'address'},{'internalType':'address','name':'aaveIncentivesController','type':'address'},{'internalType':'address','name':'aweth_','type':'address'},{'internalType':'address','name':'weth_','type':'address'}],'stateMutability':'nonpayable','type':'constructor'},{'stateMutability':'payable','type':'fallback'},{'inputs':[],'name':'aweth','outputs':[{'internalType':'contract IERC20','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'claimRewards','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'cooldownEnds','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'dailyLimit','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'doFaucetDrop','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'emergencyEtherTransfer','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'faucetFunds','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'faucetHandler','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'faucetOwner','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'faucetTarget','outputs':[{'internalType':'address','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'incentivesController','outputs':[{'internalType':'contract IAaveIncentivesController','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'lendingPool','outputs':[{'internalType':'contract ILendingPool','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'inputs':[],'name':'rewardsAmount','outputs':[{'internalType':'uint256','name':'','type':'uint256'}],'stateMutability':'view','type':'function'},{'inputs':[{'internalType':'uint256','name':'newDailyLimit','type':'uint256'}],'name':'setDailyLimit','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'newFaucetHandler','type':'address'}],'name':'setFaucetHandler','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'newFaucetOwner','type':'address'}],'name':'setFaucetOwner','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'newFaucetTarget','type':'address'}],'name':'setFaucetTarget','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[{'internalType':'address','name':'token','type':'address'},{'internalType':'address','name':'to','type':'address'},{'internalType':'uint256','name':'amount','type':'uint256'}],'name':'tokenTransfer','outputs':[],'stateMutability':'nonpayable','type':'function'},{'inputs':[],'name':'weth','outputs':[{'internalType':'contract IWETH','name':'','type':'address'}],'stateMutability':'view','type':'function'},{'stateMutability':'payable','type':'receive'}]";

        private Contract Faucet_Contract;

        private ulong LastBotMessageID = 0;

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
            // Test musim vyskusat JSON ako funguje
            persistenData = JsonUtil.ReadFromJsonFile<PersistenData>("Data");
            MillisecondsUntilNextClaimReward = persistenData.TresholdForClaimRewardsInMilliseconds;

            // ENVIROMENT VARIABLES
            DotNetEnv.Env.TraversePath().Load();

            WEB3Endpoint = DotNetEnv.Env.GetString("WEB3_ENDPOINT");
            BotToken = DotNetEnv.Env.GetString("BOT_TOKEN");
            ChannelID = ulong.Parse(DotNetEnv.Env.GetString("DISCORD_CHANNEL_ID"));
            FAUCET_HANDLER_PRIVATE_KEY = DotNetEnv.Env.GetString("FAUCET_HANDLER_PRIVATE_KEY");
            FaucetAddress = DotNetEnv.Env.GetString("FAUCET_CONTRACT_ADDRESS");
            if (!int.TryParse(DotNetEnv.Env.GetString("REFRESH_MS"), out RefreshTimeInMilliseconds))
            {
                RefreshTimeInMilliseconds = 3600000; // Evety hour
            }

            RefreshTimeInMilliseconds = int.Parse(DotNetEnv.Env.GetString("REFRESH_MS"));
            //RefreshTimeInMilliseconds = int.Parse(DotNetEnv.Env.GetString("REFRESH_MS"));

            // Prepare WEB3

            _web3Endpoint = new Web3(WEB3Endpoint);

            Faucet_Contract = _web3Endpoint.Eth.GetContract(FAUCET_ABI, FaucetAddress);

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

            _commandHandler = new CommandHandler(_client, _commandService, ChannelID);

            await _commandHandler.InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, BotToken);
            await _client.StartAsync();


            //First refresh  importatnt if we have long refresh time.
            await Task.Delay(5000);
            await Refresh();

            while (true) // And we loop here
            {
                await Task.Delay(RefreshTimeInMilliseconds);
                await Refresh();
            }
        }

        private async Task Ready()
        {
            await CleanChannel();
            //BigInteger.Parse()
        }

        private async Task ClaimRewards()
        {

        }

        public async Task Refresh()
        {
            // CLAMING AWARDS 
            MillisecondsUntilNextClaimReward -= RefreshTimeInMilliseconds;

            if (MillisecondsUntilNextClaimReward <= 0)
            {
                MillisecondsUntilNextClaimReward = persistenData.TresholdForClaimRewardsInMilliseconds;
                await ClaimRewards();
            }

            // SHOW GENERAL INFO

            // TODO REFRESH FAUCET STATE ..
            // vypisem   incentives / balance


          //  var balance = _web3Endpoint.Eth.GetBalance.SendRequestAsync(FaucetAddress);
           // var etherAmount = Web3.Convert.FromWei(balance.Value);

            var balance = await _web3Endpoint.Eth.GetBalance.SendRequestAsync(FaucetAddress);
            var etherAmount = Web3.Convert.FromWei(balance.Value);

            StringBuilder sb = new StringBuilder();
            sb.Append($"```"); // CODE FORMATTING START
            sb.AppendLine("");
            sb.Append($"```");




               await WriteToMyChannel(sb.ToString());
        }

        private async Task WriteToMyChannel(string text)
        {
            IMessageChannel channel = _client.GetChannel(ChannelID) as IMessageChannel;
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
            IMessageChannel channel = _client.GetChannel(ChannelID) as IMessageChannel;
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

        //public void SetNewLP(float newLP)
        //{
        //    newLP = MathF.Max(0, newLP);

        //    Console.WriteLine("Setting new LP value to: " + newLP);

        //    LPTokens = newLP;
        //}
    }
}
