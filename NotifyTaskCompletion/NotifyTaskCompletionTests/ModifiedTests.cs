using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotifyTaskCompletion;
using System.Threading.Tasks;
using System.Threading;

namespace NotifyTaskCompletionTests
{
    [TestClass]
    public class ModifiedTests
    {
        [TestMethod]
        public async Task TestCancellation()
        {
            var cts = new CancellationTokenSource();
            Func<int, CancellationToken, Task<int>> function = async (x, ct) =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                return x;
            };

            var completion = new NotifyTaskCompletion<int>(() => function(9, cts.Token));
            var passing = false;
            completion.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "IsCanceled")
                {
                    passing = true;
                    Assert.AreEqual(0, completion.Result);
                    Assert.AreEqual(TaskStatus.Canceled, completion.Status);
                    Assert.IsTrue(completion.IsCompleted);
                    Assert.IsTrue(completion.IsCanceled);
                    Assert.IsFalse(completion.IsFaulted);
                    Assert.IsNull(completion.AggregateException, "no aggregate exception expected after cancellation");
                }
            };
            completion.Watch();
            cts.Cancel();
            await Task.Delay(200);
            Assert.IsTrue(passing, "should have passed PropertyChanged handler");
        }

        [TestMethod]
        public async Task TestResultAsync()
        {
            Func<int, Task<int>> function = async x =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                return x;
            };

            var i = 42;
            var task = function(i);
            var completion = new NotifyTaskCompletion<int>(() => task);
            var passing = false;
            completion.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "Result")
                {
                    passing = true;
                    Assert.AreEqual(i, completion.Result);
                    Assert.AreEqual(TaskStatus.RanToCompletion, completion.Status);
                    Assert.IsTrue(completion.IsCompleted);
                    Assert.IsFalse(completion.IsCanceled);
                    Assert.IsFalse(completion.IsFaulted);
                    Assert.IsNull(completion.AggregateException);
                }
            };
            completion.Watch();
            await Task.Delay(200);
            Assert.IsTrue(passing, "should have passed PropertyChanged handler");
        }

        [TestMethod]
        public async Task TestFaulted()
        {
            Func<int, Task<int>> function = async x =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                throw new ArgumentException();
            };

            var i = 42;
            var completion = new NotifyTaskCompletion<int>(() => function(i));
            var passing = false;
            completion.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "IsFaulted")
                {
                    passing = true;
                    Assert.AreEqual(0, completion.Result);
                    Assert.AreEqual(TaskStatus.Faulted, completion.Status);
                    Assert.IsTrue(completion.IsCompleted);
                    Assert.IsFalse(completion.IsCanceled);
                    Assert.IsTrue(completion.IsFaulted);
                    Assert.IsNotNull(completion.AggregateException);
                    
                }
            };
            completion.Watch();
            await Task.Delay(200);
            Assert.IsTrue(passing, "should have passed PropertyChanged handler");
        }
    }
}