using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AssetBundleHub;
using AssetBundleHub.Tasks;
using System;
using System.Threading;

namespace AssetBundleHubTests
{
    public class QueueRequestDecoratorTest
    {
        public class Counter
        {
            public int startCount = 0;
            public int endCount = 0;
            int delayFrameCount;

            public Counter(int delayFrameCount = 10)
            {
                this.delayFrameCount = delayFrameCount;
            }

            public async UniTask<IDownloadResponseContext> Run(IDownloadRequestContext context, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                startCount++;
                await UniTask.DelayFrame(delayFrameCount, cancellationToken: cancellationToken);
                endCount++;
                return null;
            }
        }

        [UnityTest]
        public IEnumerator 正常系_キャパ1() => UniTask.ToCoroutine(async () =>
        {
            int runCapacity = 1;
            var counter = new Counter();
            var context = DownloadRequestContextFixture.Load();
            var describedClass = new QueueRequestDecorator(runCapacity);
            using var cts = new CancellationTokenSource();

            var task1 = describedClass.DownloadAsync(context, cts.Token, counter.Run);
            Assert.That(counter.startCount, Is.EqualTo(1));  // キャパ以内なので待たない
            Assert.That(describedClass.RunningCount, Is.EqualTo(1));
            var task2 = describedClass.DownloadAsync(context, cts.Token, counter.Run);
            Assert.That(counter.startCount, Is.EqualTo(1));  // キャパ以上なので待つ
            Assert.That(describedClass.RunningCount, Is.EqualTo(1));
            var task3 = describedClass.DownloadAsync(context, cts.Token, counter.Run);
            Assert.That(counter.startCount, Is.EqualTo(1));  // キャパ以上なので待つ
            Assert.That(describedClass.RunningCount, Is.EqualTo(1));
            await task1;
            // 待ちなし -> 待ってたやつやつを実行する場合はレスポンス返す前に次の実行してるので1フレーム待たない
            Assert.That(counter.startCount, Is.EqualTo(2));  // 1つ目が終わって2つ目が開始される
            Assert.That(counter.endCount, Is.EqualTo(1));
            await task2;
            await UniTask.Yield(); // 次のタスク開始よりresultが早く来るので1フレーム待つ
            Assert.That(counter.startCount, Is.EqualTo(3));  // 2つ目が終わって3つ目が開始される
            Assert.That(counter.endCount, Is.EqualTo(2));
            await task3;
            Assert.That(counter.startCount, Is.EqualTo(3));  // 3つ目が終わって終了
            Assert.That(counter.endCount, Is.EqualTo(3));
            await UniTask.Yield();  // RunningCount--よりresultが早く来るので1フレーム待つ
            Assert.That(describedClass.RunningCount, Is.EqualTo(0));
        });

        [UnityTest]
        public IEnumerator 正常系_キャパ4() => UniTask.ToCoroutine(async () =>
        {
            int runCapacity = 4;
            var counter = new Counter();
            var context = DownloadRequestContextFixture.Load();
            var describedClass = new QueueRequestDecorator(runCapacity);
            var cts = new CancellationTokenSource();

            var tasks = new List<UniTask<IDownloadResponseContext>>();
            for (int i = 0; i < runCapacity; i++)
            {
                tasks.Add(describedClass.DownloadAsync(context, cts.Token, counter.Run));
                await UniTask.DelayFrame(2); // 徐々にRunnningCountが減るのをテストしたいので少し待つ
                Assert.That(counter.startCount, Is.EqualTo(i + 1));  // キャパ以内なので待たない
                Assert.That(describedClass.RunningCount, Is.EqualTo(i + 1));
            }
            tasks.Add(describedClass.DownloadAsync(context, cts.Token, counter.Run));
            Assert.That(counter.startCount, Is.EqualTo(runCapacity));  // キャパ以上なので待つ
            Assert.That(describedClass.RunningCount, Is.EqualTo(runCapacity));
            await tasks[0];
            Assert.That(counter.startCount, Is.EqualTo(runCapacity + 1));  // 待ち状態のタスクが実行される
            Assert.That(counter.endCount, Is.EqualTo(1));
            Assert.That(describedClass.RunningCount, Is.EqualTo(runCapacity));
            for (int i = 1; i < tasks.Count; i++)
            {
                await tasks[i];
                await UniTask.Yield();
                Assert.That(counter.endCount, Is.EqualTo(i + 1));
                Assert.That(describedClass.RunningCount, Is.EqualTo(runCapacity - i));
            }
        });

        [UnityTest]
        public IEnumerator 正常系_キャンセル() => UniTask.ToCoroutine(async () =>
        {
            int runCapacity = 4;
            int runCount = 8;
            var counter = new Counter(20);
            var context = DownloadRequestContextFixture.Load();
            var describedClass = new QueueRequestDecorator(runCapacity);
            var ctsList = new List<CancellationTokenSource>();
            var tasks = new List<UniTask<IDownloadResponseContext>>();
            for (int i = 0; i < runCount; i++)
            {
                var cts = new CancellationTokenSource();
                var task = describedClass.DownloadAsync(context, cts.Token, counter.Run);
                if (i != runCount - 1)
                {
                    task.Forget();
                }
                tasks.Add(task);
                ctsList.Add(cts);
            }
            Assert.That(counter.startCount, Is.EqualTo(runCapacity));
            Assert.That(counter.endCount, Is.EqualTo(0));
            Assert.That(describedClass.RunningCount, Is.EqualTo(runCapacity));
            ctsList[0].Cancel(); //先頭キャンセル
            await UniTask.DelayFrame(2); // 1フレーム後にfinallyが呼ばれるので2フレ待つ
            Assert.That(counter.startCount, Is.EqualTo(runCapacity + 1));
            Assert.That(counter.endCount, Is.EqualTo(0));
            Assert.That(describedClass.RunningCount, Is.EqualTo(runCapacity));
            ctsList[runCapacity].Cancel(); // Runで実行されたタスクをキャンセル
            await UniTask.Yield(); // 次フレームにcatchされてすぐ次のrunが実行される。
            Assert.That(counter.startCount, Is.EqualTo(runCapacity + 2));
            Assert.That(counter.endCount, Is.EqualTo(0));
            Assert.That(describedClass.RunningCount, Is.EqualTo(runCapacity));
            ctsList[ctsList.Count - 2].Cancel(); //末尾から一つ手前のまだ実行されてないタスクをキャンセル
            await tasks[tasks.Count - 1]; // 全部終わるまで待つ。
            await UniTask.Yield();
            Assert.That(counter.startCount, Is.EqualTo(runCount - 1)); // 1つ実行前キャンセルしているため
            Assert.That(counter.endCount, Is.EqualTo(runCount - 3)); // 3つキャンセルしているため
            Assert.That(describedClass.RunningCount, Is.EqualTo(0));
        });
    }
}
