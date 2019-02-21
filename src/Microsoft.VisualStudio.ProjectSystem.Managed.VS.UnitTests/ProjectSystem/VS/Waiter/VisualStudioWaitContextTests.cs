﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.ProjectSystem.VS.Waiting;
using Microsoft.VisualStudio.Shell.Interop;

using Moq;

using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Waiter
{
    public class VisualStudioWaitContextTests
    {
        [Fact]
        public static void SetPropertyAllowCancel_Test()
        {
            string title = "Test001";
            string message = "Testing001";
            bool isCancelable = true;
            var context = Create(title, message, isCancelable);
            Assert.True(context.AllowCancel);
            context.AllowCancel = false;
            Assert.False(context.AllowCancel);
        }

        [Fact]
        public static void SetPropertyMessage_Test()
        {
            string title = "Test001";
            string message = "Testing001";
            bool isCancelable = true;
            var context = Create(title, message, isCancelable);
            Assert.Equal(message, context.Message);
            var message2 = "Testing002";
            context.Message = message2;
            Assert.Equal(message2, context.Message);
        }

        [Fact]
        public static void CreateWrongType_Test()
        {
            Assert.Throws<ArgumentNullException>(() => _ = CreateWrongType(string.Empty, string.Empty, false));
        }

        private delegate void CreateInstanceCallback(out IVsThreadedWaitDialog2 ppIVsThreadedWaitDialog);

        private static VisualStudioWaitContext Create(string title, string message, bool allowCancel)
        {
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
                    Assert.Equal(allowCancel, fIsCancelable);
                });
            threadedWaitDialogMock.Setup(m => m.EndWaitDialog(out It.Ref<int>.IsAny));
            var threadedWaitDialog = threadedWaitDialogMock.Object;

            threadedWaitDialogFactoryMock
                .Setup(m => m.CreateInstance(out It.Ref<IVsThreadedWaitDialog2>.IsAny))
                .Callback(new CreateInstanceCallback((out IVsThreadedWaitDialog2 ppIVsThreadedWaitDialog) =>
                {
                    ppIVsThreadedWaitDialog = threadedWaitDialog;
                }))
                .Returns(HResult.OK);
            return new VisualStudioWaitContext(threadedWaitDialogFactoryMock.Object, title, message, allowCancel);
        }

        private static VisualStudioWaitContext CreateWrongType(string title, string message, bool allowCancel)
        {
            var threadedWaitDialogFactoryMock = new Mock<IVsThreadedWaitDialogFactory>();
            var threadedWaitDialog = new Mock<IVsThreadedWaitDialog2>().Object;
            threadedWaitDialogFactoryMock
                .Setup(m => m.CreateInstance(out It.Ref<IVsThreadedWaitDialog2>.IsAny))
                .Callback(new CreateInstanceCallback((out IVsThreadedWaitDialog2 ppIVsThreadedWaitDialog) =>
                {
                    ppIVsThreadedWaitDialog = null;
                }))
                .Returns(HResult.OK);
            return new VisualStudioWaitContext(threadedWaitDialogFactoryMock.Object, title, message, allowCancel);
        }
    }
}
