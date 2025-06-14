using Microsoft.AspNetCore.SignalR;
using SecureNetBackend.Data;
using SecureNetBackend.Models;
using System.Collections.Concurrent;

namespace SecureNetBackend.Hubs
{
    public class AdminHub : Hub
    {
        private readonly MongoDbContext _dbContext;

        // Store connection info: PCName -> ConnectionId (clients)
        private static ConcurrentDictionary<string, string> connectedClients = new ConcurrentDictionary<string, string>();

        public AdminHub(MongoDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override async Task OnConnectedAsync()
        {
            // Could check if client or admin here via query string or headers
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Remove disconnected PC from dictionary
            var disconnected = connectedClients.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (!string.IsNullOrEmpty(disconnected.Key))
            {
                connectedClients.TryRemove(disconnected.Key, out _);

                // Update DB status
                var pc = await _dbContext.PCs.Find(x => x.PCName == disconnected.Key).FirstOrDefaultAsync();
                if (pc != null)
                {
                    pc.IsConnected = false;
                    await _dbContext.PCs.ReplaceOneAsync(x => x.Id == pc.Id, pc);
                    await BroadcastPCList();
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Client calls this on connect with its PC info
        public async Task RegisterClient(string pcName, string ip, string adminCode)
        {
            connectedClients[pcName] = Context.ConnectionId;

            var pc = await _dbContext.PCs.Find(x => x.PCName == pcName).FirstOrDefaultAsync();
            if (pc == null)
            {
                pc = new PCInfo()
                {
                    PCName = pcName,
                    IP = ip,
                    AdminCode = adminCode,
                    IsConnected = true
                };
                await _dbContext.PCs.InsertOneAsync(pc);
            }
            else
            {
                pc.IP = ip;
                pc.AdminCode = adminCode;
                pc.IsConnected = true;
                await _dbContext.PCs.ReplaceOneAsync(x => x.Id == pc.Id, pc);
            }

            await BroadcastPCList();
        }

        // Admin calls this to request initial data
        public async Task RequestInitialData()
        {
            await BroadcastPCList();
            await BroadcastBlacklist();
        }

        // Send updated PC list to all connected clients (admin apps)
        private async Task BroadcastPCList()
        {
            var pcs = await _dbContext.PCs.Find(_ => true).ToListAsync();
            await Clients.All.SendAsync("UpdatePCList", pcs);
        }

        // Send updated blacklist to all connected clients
        private async Task BroadcastBlacklist()
        {
            var bl = await _dbContext.Blacklist.Find(_ => true).ToListAsync();
            var items = bl.Select(x => x.Item).ToList();
            await Clients.All.SendAsync("UpdateBlacklist", items);
        }

        // Admin adds new blacklist word
        public async Task AddToBlacklist(string item)
        {
            if (string.IsNullOrEmpty(item)) return;

            var exists = await _dbContext.Blacklist.Find(x => x.Item == item).AnyAsync();
            if (!exists)
            {
                await _dbContext.Blacklist.InsertOneAsync(new BlacklistItem() { Item = item });
                await BroadcastBlacklist();
            }
        }

        // Admin removes blacklist word
        public async Task RemoveFromBlacklist(string item)
        {
            if (string.IsNullOrEmpty(item)) return;

            await _dbContext.Blacklist.DeleteOneAsync(x => x.Item == item);
            await BroadcastBlacklist();
        }

        // Admin sends command to a client
        public async Task SendCommandToPC(string pcName, string command, string param = null)
        {
            if (connectedClients.TryGetValue(pcName, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveCommand", command, param);
            }
        }
    }
}
