using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using Runeheir.Accounts;
using Runeheir.Characters;
using Runeheir.Items;
using Runeheir.Social;
using UnityEngine;

namespace Runeheir.Net
{
    /// <summary>
    /// The login and character server of an online realm, as the front end sees it: the same <see cref="IAccountService"/>
    /// the offline JSON file implements, carried over the network. Replies are matched to requests by id; a realm that
    /// doesn't answer within 20 seconds counts as a failure.
    /// </summary>
    internal sealed class NetworkAccountService : IAccountService, ILogoutService
    {
        private const int TimeoutMs = 20000;

        private readonly Dictionary<int, TaskCompletionSource<AccountReply>> _pending = new Dictionary<int, TaskCompletionSource<AccountReply>>();
        private int _nextId;

        public bool IsOffline => false;

        /// <summary>From the last successful login.</summary>
        public string RealmName { get; private set; } = "Realm";

        public bool AllowGmCommands { get; private set; }

        public void Attach()
        {
            NetworkClient.RegisterHandler<AccountReply>(OnReply, false);
        }

        public void Detach()
        {
            NetworkClient.UnregisterHandler<AccountReply>();
            FailAll("Disconnected from the realm.");
        }

        public async Task<OpResult<string>> RegisterAsync(string username, string password)
        {
            var reply = await Send(new AccountRequest { Op = AccountOp.Register, Text1 = username, Text2 = password });
            return reply.Ok ? OpResult<string>.Ok(reply.Value) : OpResult<string>.Fail(reply.Error);
        }

        public async Task<OpResult<string>> LoginAsync(string username, string password)
        {
            var reply = await Send(new AccountRequest { Op = AccountOp.Login, Text1 = username, Text2 = password });
            if (!reply.Ok)
            {
                return OpResult<string>.Fail(reply.Error);
            }

            RealmName = string.IsNullOrEmpty(reply.RealmName) ? "Realm" : reply.RealmName;
            AllowGmCommands = reply.AllowGm;
            return OpResult<string>.Ok(reply.Value);
        }

        public async Task LogoutAsync()
        {
            await Send(new AccountRequest { Op = AccountOp.Logout });
        }

        public async Task<IReadOnlyList<ServerInfo>> GetServersAsync()
        {
            var reply = await Send(new AccountRequest { Op = AccountOp.Servers });
            var list = reply.Ok ? Json.Read<ServerListJson>(reply.Json) : null;
            IReadOnlyList<ServerInfo> servers = list?.Items ?? new List<ServerInfo>
            {
                new ServerInfo { Id = "realm", Name = RealmName, Description = reply.Error ?? "Unreachable", Online = false },
            };
            return servers;
        }

        public async Task<List<CharacterRecord>> GetCharactersAsync(string username)
        {
            var reply = await Send(new AccountRequest { Op = AccountOp.Characters });
            if (!reply.Ok)
            {
                throw new System.InvalidOperationException(reply.Error ?? "The realm didn't send the character list.");
            }

            var list = Json.Read<CharacterListJson>(reply.Json)?.Items ?? new List<CharacterRecord>();
            foreach (var record in list)
            {
                record?.Sanitize();
            }

            list.RemoveAll(r => r == null);
            return list;
        }

        public async Task<OpResult<CharacterRecord>> CreateCharacterAsync(string username, int slot, CharacterCreateRequest request)
        {
            var reply = await Send(new AccountRequest
            {
                Op = AccountOp.Create,
                Slot = slot,
                Json = Json.Write(new CreateRequestJson
                {
                    Name = request.Name, Gender = (int)request.Gender, HairStyle = request.HairStyle, HairColor = request.HairColor,
                }),
            });
            var record = reply.Ok ? Json.Read<CharacterRecord>(reply.Json) : null;
            if (record == null)
            {
                return OpResult<CharacterRecord>.Fail(reply.Error ?? "The character couldn't be created.");
            }

            record.Sanitize();
            return OpResult<CharacterRecord>.Ok(record);
        }

        public async Task<OpResult> DeleteCharacterAsync(string username, int slot, string confirmName)
        {
            var reply = await Send(new AccountRequest { Op = AccountOp.Delete, Slot = slot, Text1 = confirmName });
            return reply.Ok ? OpResult.Ok() : OpResult.Fail(reply.Error);
        }

        public async Task<OpResult> SaveCharacterAsync(string username, CharacterRecord record)
        {
            var reply = await Send(new AccountRequest { Op = AccountOp.Save, Json = JsonUtility.ToJson(record) });
            return reply.Ok ? OpResult.Ok() : OpResult.Fail(reply.Error);
        }

        public async Task<OpResult<List<ItemStack>>> GetStorageAsync(string username)
        {
            var reply = await Send(new AccountRequest { Op = AccountOp.Storage });
            var list = reply.Ok ? Json.Read<ItemListJson>(reply.Json) : null;
            return list != null ? OpResult<List<ItemStack>>.Ok(ItemTransfer.Clean(list.Items)) : OpResult<List<ItemStack>>.Fail(reply.Error);
        }

        public async Task<OpResult> SaveCharacterAndStorageAsync(string username, CharacterRecord record, List<ItemStack> storage)
        {
            var reply = await Send(new AccountRequest
            {
                Op = AccountOp.SaveWithStorage,
                Json = JsonUtility.ToJson(record),
                Json2 = Json.Write(new ItemListJson { Items = storage ?? new List<ItemStack>() }),
            });
            return reply.Ok ? OpResult.Ok() : OpResult.Fail(reply.Error);
        }

        private async Task<AccountReply> Send(AccountRequest request)
        {
            if (!NetworkClient.isConnected)
            {
                return new AccountReply { Error = "Not connected to the realm." };
            }

            request.Id = ++_nextId;
            request.Protocol = RealmConfig.ProtocolVersion;
            var source = new TaskCompletionSource<AccountReply>();
            _pending[request.Id] = source;
            NetworkClient.Send(request);
            var finished = await Task.WhenAny(source.Task, Task.Delay(TimeoutMs));
            _pending.Remove(request.Id);
            return finished == source.Task ? source.Task.Result : new AccountReply { Error = "The realm didn't answer in time." };
        }

        private void OnReply(AccountReply reply)
        {
            if (_pending.TryGetValue(reply.Id, out var source))
            {
                _pending.Remove(reply.Id);
                source.TrySetResult(reply);
            }
        }

        private void FailAll(string reason)
        {
            foreach (var source in _pending.Values)
            {
                source.TrySetResult(new AccountReply { Error = reason });
            }

            _pending.Clear();
        }
    }
}
