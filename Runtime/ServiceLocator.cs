using System;
using System.Collections.Generic;

namespace AssetBundleHub
{
    // ABHubに特化したServiceLocator
    // Resolve時に何も登録されてなければDefaultを呼び出す。
    // ServiceLocatorのRegisterはABHubのInitializeよりも前限定にし、ABHub.InitializeのタイミングでDefaultをいれるという手もある。
    // しかし、Initializeで使わないインスタンスはInitializeより後にRegisterしても無駄なく動くようにするために、Resolve時にDefaultのCreateを呼ぶ。
    public class ServiceLocator
    {
        public readonly static ServiceLocator instance = new ServiceLocator(new Dictionary<Type, Func<object>>()
            {
                { typeof(ILocalAssetBundleTable), LocalAssetBundleTable.Create }
            }
        );

        readonly Dictionary<Type, object> container = new Dictionary<Type, object>();
        readonly Dictionary<Type, Func<object>> defaultFactories;

        public ServiceLocator(Dictionary<Type, Func<object>> defaultFactories = null)
        {
            this.defaultFactories = defaultFactories;
        }

        public void Register<T>(T obj) where T : class
        {
            container[typeof(T)] = obj;
        }

        public T Resolve<T>() where T : class
        {
            Type targetType = typeof(T);
            if (container.TryGetValue(targetType, out object obj))
            {
                return obj as T;
            }

            if (defaultFactories.TryGetValue(targetType, out var createFunc))
            {
                T newObj = createFunc() as T;
                this.Register<T>(newObj);
                return newObj;
            }
            return null;
        }

        public void Clear()
        {
            container.Clear();
        }
    }
}
