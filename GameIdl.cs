using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using KhelContracts;
using KhelContracts.Program;
using KhelContracts.Errors;
using KhelContracts.Accounts;

namespace KhelContracts
{
    namespace Accounts
    {
        public partial class Player
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 15766710478567431885UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{205, 222, 112, 7, 165, 155, 206, 218};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "bSBoKNsSHuj";
            public PublicKey Authority { get; set; }

            public string Username { get; set; }

            public ushort Level { get; set; }

            public static Player Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Player result = new Player();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                offset += _data.GetBorshString(offset, out var resultUsername);
                result.Username = resultUsername;
                result.Level = _data.GetU16(offset);
                offset += 2;
                return result;
            }
        }

        public partial class Stats
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 17160057810888916414UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{190, 125, 51, 63, 169, 197, 36, 238};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "YryzEM17bE5";
            public PublicKey Authority { get; set; }

            public ushort TimesPlayed { get; set; }

            public ushort PreviousScore { get; set; }

            public ushort HighestScore { get; set; }

            public static Stats Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Stats result = new Stats();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                result.TimesPlayed = _data.GetU16(offset);
                offset += 2;
                result.PreviousScore = _data.GetU16(offset);
                offset += 2;
                result.HighestScore = _data.GetU16(offset);
                offset += 2;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum KhelContractsErrorKind : uint
        {
            WrongAuthority = 6000U
        }
    }

    public partial class KhelContractsClient : TransactionalBaseClient<KhelContractsErrorKind>
    {
        public KhelContractsClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Player>>> GetPlayersAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Player.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Player>>(res);
            List<Player> resultingAccounts = new List<Player>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Player.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Player>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Stats>>> GetStatssAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Stats.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Stats>>(res);
            List<Stats> resultingAccounts = new List<Stats>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Stats.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Stats>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Player>> GetPlayerAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Player>(res);
            var resultingAccount = Player.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Player>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Stats>> GetStatsAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Stats>(res);
            var resultingAccount = Stats.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Stats>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribePlayerAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Player> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Player parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Player.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeStatsAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Stats> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Stats parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Stats.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitializeAsync(InitializeAccounts accounts, string username, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KhelContractsProgram.Initialize(accounts, username, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendInitializeGameAsync(InitializeGameAccounts accounts, string gameName, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KhelContractsProgram.InitializeGame(accounts, gameName, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<KhelContractsErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<KhelContractsErrorKind>>{{6000U, new ProgramError<KhelContractsErrorKind>(KhelContractsErrorKind.WrongAuthority, "Wrong Authority")}, };
        }
    }

    namespace Program
    {
        public class InitializeAccounts
        {
            public PublicKey Player { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class InitializeGameAccounts
        {
            public PublicKey PlayerStats { get; set; }

            public PublicKey Player { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public static class KhelContractsProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction Initialize(InitializeAccounts accounts, string username, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17121445590508351407UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(username, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeGame(InitializeGameAccounts accounts, string gameName, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerStats, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Player, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15529203708862021164UL, offset);
                offset += 8;
                offset += _data.WriteBorshString(gameName, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}