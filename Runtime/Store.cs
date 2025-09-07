using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Persistence;
using ReactiveUnity.JsonConverters;
using UnityEngine;

namespace Stores
{
    public class StoreCreationConverter : CustomCreationConverter<Store>
    {
        private readonly Type type;

        public StoreCreationConverter(Type storeType)
        {
            type = storeType;
        }

        public override Store Create(Type _)
        {
            return Store.GetDynamic(type);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Store : ScriptableObject
    {
        [HideInInspector]
        [SerializeField]
        // used for displaying the json in the editor, needs to be serialized
        // in order to persist across domain reloads
        private string prettyString = null;

        private static readonly string assetFolder = "Stores";
        private static readonly Dictionary<Type, Store> storeCache = new();
        private static readonly JsonConverter[] defaultConverters = new JsonConverter[]
        {
            new ReactiveConverter<int>(),
            new ReactiveConverter<bool>(),
            new ReactiveConverter<float>(),
            new ReactiveConverter<double>(),
            new Vector2Converter(),
            new Vector2IntConverter(),
            new Vector3Converter(),
            new Vector3IntConverter(),
        };

        private List<JsonConverter> Converters
        {
            get
            {
                List<JsonConverter> converters = new() { new StoreCreationConverter(GetType()) };
                converters.AddRange(defaultConverters);
                return converters;
            }
        }

        // TODO: saving to json based on asset name is not a good idea, because we could rename
        // the asset.  Should use a persistent ID instead
        public string FileName => $"{name.ToLower().Replace(" ", "-")}.json";

        // For editor and custom creation reflection wizardry.  The cache is god
        public static Store GetDynamic(Type type)
        {
            MethodInfo methodInfo = typeof(Store).GetMethod("Get");
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(type);
            return (Store)genericMethod.Invoke(null, null);
        }

        public static T Get<T>()
            where T : Store
        {
            Type storeType = typeof(T);
            if (storeCache.ContainsKey(storeType))
            {
                return storeCache[storeType] as T;
            }

            T[] stores = Resources.LoadAll<T>(assetFolder);
            if (stores.Length == 0)
            {
                throw new Exception("Tried to get a store that doesnt exist");
            }

            if (stores.Length == 2)
            {
                throw new Exception("Tried to get a store that had multiple instances");
            }

            // If it wasn't in the cache, it means we need to trigger OnGet
            T store = stores[0];
            store.SubscribeToComputed();
            storeCache[storeType] = store;
            return store;
        }

        // TODO: what if it doesn't exist yet?
        // TODO: memory use here isnt great
        public static async Task<T> LoadFromDisk<T>(T store, bool triggerSubscribeToComputed = true)
            where T : Store
        {
            JsonSerializerSettings settings = new()
            {
                Converters = store.Converters,

                // TODO: bring this back, if needed.
                // without this, lists have weird double serialize glitch
                // ObjectCreationHandling = ObjectCreationHandling.Replace,
            };

            T loaded = await JsonPersistence.FromJson<T>(store.FileName, settings);

            // TODO: this isn't ideal... might have some mem leak problems here
            if (triggerSubscribeToComputed)
            {
                loaded.SubscribeToComputed();
            }

            return loaded;
        }

        public static async Task<string> PersistToDisk<T>(T store)
            where T : Store
        {
            return await JsonPersistence.PersistJson(
                store,
                store.FileName,
                // Always persist to disk in pretty form, because its just more practical to debug that
                // way and we don't really have perf problems (yet)
                // TODO: rethink this if we have perf problems
                // TODO: memory use here isnt great
                new JsonSerializerSettings()
                {
                    Converters = store.Converters,
                    Formatting = Formatting.Indented,
                }
            );
        }

        public static string PersistToDiskSync<T>(T store)
            where T : Store
        {
            return JsonPersistence.PersistJsonSync(
                store,
                store.FileName,
                // Always persist to disk in pretty form, because its just more practical to debug that
                // way and we don't really have perf problems (yet)
                // TODO: rethink this if we have perf problems
                // TODO: memory use here isnt great
                new JsonSerializerSettings()
                {
                    Converters = store.Converters,
                    Formatting = Formatting.Indented,
                }
            );
        }

        // Useful to do any store related setup, like Computed<T> callbacks.
        protected virtual void SubscribeToComputed() { }
    }
}
