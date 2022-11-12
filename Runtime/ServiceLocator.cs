using System;
using System.Collections.Generic;

namespace AssetBundleHub
{
    // AssetBundleHubに特化したServiceLocator
    // AssetBundleHubの挙動を変える場合には機能単位でインタフェースを登録することで上書きする。
    // Resolve時に何も登録されてなければDefaultを呼び出す。
    // ServiceLocatorのRegisterはABHubのInitializeよりも前限定にし、ABHub.InitializeのタイミングでDefaultをいれるという手もある。
    // しかし、Initializeで使わないインスタンスはInitializeより後にRegisterしても無駄なく動くようにするために、Resolve時にDefaultのCreateを呼ぶ。
    public class ServiceLocator
    {
        public static ServiceLocator Instance { get; private set; } = new ServiceLocator(new Dictionary<Type, Func<object>>()
            {
                { typeof(ILocalAssetBundleTable), LocalAssetBundleTable.Load },
                { typeof(IAssetBundleListLoader), AssetBundleListLoader.New },
                { typeof(IDownloadAsyncDecoratorsFactory), DownloadAsyncDecoratorsFactory.New },
                { typeof(IBundlePullTasksFactory), DefaultBundlePullTasks.New },
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

        public bool UnRegister<T>() where T : class
        {
            return container.Remove(typeof(T));
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
