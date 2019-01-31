using System;
using System.Threading;
using System.Threading.Tasks;
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.VS.Waiting;
using Microsoft.VisualStudio.ProjectSystem.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiter
{
    public class VisualStudioOperationWaitIndicatorTests
    {
        [Fact]
        public static async Task Dispose_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            Assert.False(waitIndicator.IsDisposed);
            waitIndicator.Dispose();
            Assert.True(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task DisposeAsync_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            Assert.False(waitIndicator.IsDisposed);
            await waitIndicator.DisposeAsync();
            Assert.True(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task DisposeBlock_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            using (waitIndicator)
            {
                Assert.False(waitIndicator.IsDisposed);
            }
            Assert.True(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task DeactivateAsync_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            await waitIndicator.DeactivateAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task ActivateAsyncTwice_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.ActivateAsync();
            await waitIndicator.ActivateAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task DeactivateTwiceAsync_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.DeactivateAsync();
            await waitIndicator.DeactivateAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task LoadAsyncAndUnloadAsync_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();
            await waitIndicator.UnloadAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task LoadAsyncTwice_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();
            await waitIndicator.LoadAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task UnloadAsyncTwice_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.UnloadAsync();
            await waitIndicator.UnloadAsync();
            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForOperation_ArgumentNullException_Test(string title, string message)
        {
            bool isCancelable = false;
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    waitIndicator.WaitForAsyncOperation(title, message, isCancelable, token =>
                    {
                        throw new Exception();
                    });
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    waitIndicator.WaitForAsyncOperation("", "", false, null);
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_Exception_Test()
        {
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                Assert.Throws<Exception>(() =>
                {
                    waitIndicator.WaitForAsyncOperation("", "", false, token =>
                    {
                        throw new Exception();
                    });
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_Wrapped_Exception_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                Assert.Throws<Exception>(() =>
                {
                    waitIndicator.WaitForAsyncOperation("", "", false, async token =>
                    {
                        await Task.FromException(new Exception());
                    });
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_Wrapped_Canceled_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                waitIndicator.WaitForOperation("", "", false, token =>
                {
                    Task.FromException(new OperationCanceledException());
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_Wrapped_Canceled_Test2Async()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                waitIndicator.WaitForAsyncOperation("", "", false, async token =>
                {
                    await Task.WhenAll(
                        Task.Run(() => throw new OperationCanceledException()),
                        Task.Run(() => throw new OperationCanceledException()));
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Fact]
        public static async Task WaitForOperation_Wrapped_Canceled_Test3Async()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            using (waitIndicator)
            {
                waitIndicator.WaitForOperation("", "", false, token =>
                {
                    throw new AggregateException(new[] { new OperationCanceledException(), new OperationCanceledException() });
                });

                Assert.False(waitIndicator.IsDisposed);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperation_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitForOperation(title, message, isCancelable, token =>
            {
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationCanceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitForOperation(title, message, isCancelable, token =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });

            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForOperationWithResult_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForOperationWithResult(title, message, false, token =>
                {
                    throw new Exception();
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForOperationWithResult_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForOperationWithResult("", "", false, null);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationWithResult_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitForOperationWithResult(title, message, isCancelable, token =>
            {
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationWithResultCanceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test02";
            string message = "Testing02";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitForOperationWithResult(title, message, isCancelable, token =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperation_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitForAsyncOperation(title, message, isCancelable, token =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForAsyncOperation_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForAsyncOperation(title, message, false, token =>
                {
                    throw new Exception();
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForAsyncOperation_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                waitIndicator.WaitForAsyncOperation("", "", false, null);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationCanceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test03";
            string message = "Testing03";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitForAsyncOperation(title, message, isCancelable, token =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationWithResult_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.WaitForAsyncOperationWithResult(title, message, isCancelable, token =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            });
            Assert.True(wasCalled);
            Assert.Equal(WaitIndicatorResult.Completed, result);
        }


        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForAsyncOperationWithResult_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncOperationWithResult(title, message, false, token =>
                {
                    throw new Exception();
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForAsyncOperationWithResult_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncOperationWithResult("", "", false, null);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationWithResultCanceled_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test04";
            string message = "Testing04";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.WaitForAsyncOperationWithResult(title, message, isCancelable, token =>
            {
                wasCalled = true;
                throw new OperationCanceledException();
            });
            Assert.True(wasCalled);
            Assert.Equal(WaitIndicatorResult.Canceled, result);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForOperationReturns_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForOperation(title, message, false, token =>
                {
                    return 42;
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForOperationReturns_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForOperation("", "", false, (Func<CancellationToken, int>)null);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationReturns_Test(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.WaitForOperation(title, message, isCancelable, token =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForOperationReturnsCanceled_Test(bool isCancelable)
        {
            string title = "Test05";
            string message = "Testing05";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.WaitForOperation(title, message, isCancelable, token =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return 42;
            });
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForOperationWithResultReturns_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (canceled, result) = waitIndicator.WaitForOperationWithResult(title, message, false, token =>
                {
                    return 42;
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForOperationWithResultReturns_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (canceled, result) = waitIndicator.WaitForOperationWithResult("", "", false, (Func<CancellationToken, int>)null);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperationWithResultReturns_Test(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var (canceled, result) = waitIndicator.WaitForOperationWithResult(title, message, isCancelable, token =>
            {
                return 42;
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForOperationWithResultReturnsCanceled_Test(bool isCancelable)
        {
            string title = "Test06";
            string message = "Testing06";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var (canceled, result) = waitIndicator.WaitForOperationWithResult(title, message, isCancelable, token =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return 42;
            });
            Assert.Equal(0, result);
            Assert.Equal(WaitIndicatorResult.Canceled, canceled);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForAsyncOperationReturns_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncOperation(title, message, false, token =>
                {
                    return Task.FromResult(42);
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForAsyncOperationReturns_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var result = waitIndicator.WaitForAsyncOperation("", "", false, (Func<CancellationToken, Task<int>>)null);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationReturns_Test(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var result = waitIndicator.WaitForAsyncOperation(title, message, isCancelable, token =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForAsyncOperationReturnsCanceled_Test(bool isCancelable)
        {
            string title = "Test07";
            string message = "Testing07";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            object result = waitIndicator.WaitForAsyncOperation(title, message, isCancelable, token =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return Task.FromResult(default(object));
            });
            Assert.Null(result);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData(null, "")]
        public static async Task WaitForAsyncOperationWithResultReturns_ArgumentNullException_Test(string title, string message)
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message);
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (_, result) = waitIndicator.WaitForAsyncOperationWithResult(title, message, false, token =>
                {
                    return Task.FromResult(42);
                });
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Fact]
        public static async Task WaitForAsyncOperationWithResultReturns_ArgumentNullException_Delegate_Test()
        {
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator();
            await waitIndicator.LoadAsync();

            Assert.Throws<ArgumentNullException>(() =>
            {
                var (_, result) = waitIndicator.WaitForAsyncOperationWithResult("", "", false, (Func<CancellationToken, Task<int>>)null);
            });

            Assert.False(waitIndicator.IsDisposed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForAsyncOperationWithResultReturns_Test(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var (_, result) = waitIndicator.WaitForAsyncOperationWithResult(title, message, isCancelable, token =>
            {
                return Task.FromResult(42);
            });
            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(true)]
        public static async Task WaitForAsyncOperationWithResultReturnsCanceled_Test(bool isCancelable)
        {
            string title = "Test08";
            string message = "Testing08";
            var (waitIndicator, _) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            var (canceled, result) = waitIndicator.WaitForAsyncOperationWithResult(title, message, isCancelable, token =>
            {
                if (isCancelable)
                {
                    throw new OperationCanceledException();
                }

                return Task.FromResult(42);
            });
            Assert.Equal(0, result);
            Assert.Equal(WaitIndicatorResult.Canceled, canceled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static async Task WaitForOperation_Cancellation_Test(bool isCancelable)
        {
            bool wasCalled = false;
            string title = "Test01";
            string message = "Testing01";
            var (waitIndicator, cancel) = CreateVisualStudioWaitIndicator(title, message, isCancelable);
            await waitIndicator.LoadAsync();

            waitIndicator.WaitForOperation(title, message, isCancelable, token =>
            {
                cancel();
                Assert.Equal(isCancelable, token.IsCancellationRequested);
                wasCalled = true;
            });
            Assert.True(wasCalled);
        }

        private delegate void CreatewaitIndicatorCallback(out IVsThreadedWaitDialog2 ppIVsThreadedWaitDialog);

        private static (VisualStudioOperationWaitIndicator, Action cancel) CreateVisualStudioWaitIndicator(string title = "", string message = "", bool isCancelable = false)
        {
            IVsThreadedWaitDialogCallback callback = null;
            var threadingService = IProjectThreadingServiceFactory.Create();
            var threadedWaitDialogFactoryServiceMock = new Mock<IVsService<SVsThreadedWaitDialogFactory, IVsThreadedWaitDialogFactory>>();
            var threadedWaitDialogFactoryMock = new Mock<IVsThreadedWaitDialogFactory>();
            var threadedWaitDialogMock = new Mock<IVsThreadedWaitDialog3>();
            threadedWaitDialogMock.Setup(m => m.StartWaitDialogWithCallback(
                It.IsNotNull<string>(),
                It.IsNotNull<string>(),
                It.Is<string>(s => s == null),
                It.Is<object>(s => s == null),
                It.Is<string>(s => s == null),
                It.IsAny<bool>(),
                It.IsInRange(0, int.MaxValue, Range.Inclusive),
                It.Is<bool>(v => v == false),
                It.Is<int>(i => i == 0),
                It.Is<int>(i => i == 0),
                It.IsNotNull<IVsThreadedWaitDialogCallback>()))
                .Callback((string szWaitCaption,
                           string szWaitMessage,
                           string szProgressText,
                           object varStatusBmpAnim,
                           string szStatusBarText,
                           bool fIsCancelable,
                           int iDelayToShowDialog,
                           bool fShowProgress,
                           int iTotalSteps,
                           int iCurrentStep,
                           IVsThreadedWaitDialogCallback pCallback) =>
                {
                    Assert.Equal(title, szWaitCaption);
                    Assert.Equal(message, szWaitMessage);
                    Assert.Equal(isCancelable, fIsCancelable);
                    callback = pCallback;
                });
            threadedWaitDialogMock.Setup(m => m.EndWaitDialog(out It.Ref<int>.IsAny));
            var threadedWaitDialog = threadedWaitDialogMock.Object;
            threadedWaitDialogFactoryMock
                .Setup(m => m.CreateInstance(out It.Ref<IVsThreadedWaitDialog2>.IsAny))
                .Callback(new CreatewaitIndicatorCallback((out IVsThreadedWaitDialog2 ppIVsThreadedWaitDialog) => ppIVsThreadedWaitDialog = threadedWaitDialog))
                .Returns(HResult.OK);
            threadedWaitDialogFactoryServiceMock.Setup(m => m.GetValueAsync()).ReturnsAsync(threadedWaitDialogFactoryMock.Object);

            Action cancel = () =>
            {
                callback?.OnCanceled();
            };

            var unconfiguredProject = UnconfiguredProjectFactory.Create();

            var waitIndicator = new VisualStudioOperationWaitIndicator(unconfiguredProject, threadingService, threadedWaitDialogFactoryServiceMock.Object);
            return (waitIndicator, cancel);
        }
    }
}
