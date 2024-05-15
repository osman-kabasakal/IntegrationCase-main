using System.Collections.Concurrent;
using Integration.Common;
using Integration.Backend;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();
    
    private readonly ConcurrentDictionary<string, object> itemContentLocks = new ConcurrentDictionary<string, object>();
    
    private readonly ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");

    // Redis kilit anahtarlarının ön eki
    private const string LOCK_KEY_PREFIX = "item-content-lock:";

    private RedLockFactory _redisLock ;

    public ItemIntegrationService()
    {
        _redisLock = RedLockFactory.Create(new List<RedLockMultiplexer>() { redis });
    }
    
    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.

    public async Task<Result> SaveItem(string itemContent)
    {
        var lockKey = $"{LOCK_KEY_PREFIX}{itemContent}";

        await using (var distributedLock = await _redisLock.CreateLockAsync(lockKey, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(1)))
        {
            if (distributedLock.IsAcquired)
            {
                if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
                {
                    return new Result(false, $"Duplicate item received with content {itemContent}.");
                }

                var item = ItemIntegrationBackend.SaveItem(itemContent);
                return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
            }
            
        }
        return new Result(false, $"Can not saved item with content {itemContent}");
    }
    


    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
}