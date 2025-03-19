// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using ET.U3D.UTIL;
using EP.U3D.UTIL;

public class TestXEditorTitle
{
    [SetUp]
    public void Setup()
    {
        XEditor.Title.isRefreshing = false;
        XEditor.Title.prefsLabel = "";
        XEditor.Title.gitBranch = "";
        XEditor.Title.gitPushCount = 0;
        XEditor.Title.gitPullCount = 0;
        XEditor.Title.gitDirtyCount = 0;
    }

    [TearDown]
    public async Task Cleanup()
    {
        XEditor.Title.isRefreshing = false;
        await XEditor.Title.Refresh();
    }

    [Obsolete]
    [TestCase("", "", 0, 0, 0, false, "Unity")]
    [TestCase("[Prefs: Test/Channel/1.0.0/Debug/Info]", "", 0, 0, 0, false, "Unity - [Prefs: Test/Channel/1.0.0/Debug/Info]")]
    [TestCase("", "main", 1, 2, 3, false, "Unity - [Git*: main ↑2 ↓3]")]
    [TestCase("", "main", 0, 0, 0, true, "Unity - [Git: main ⟳]")]
    [TestCase("[Prefs: Test/Channel/1.0.0/Debug/Info]", "main", 1, 0, 0, false, "Unity - [Prefs: Test/Channel/1.0.0/Debug/Info] - [Git*: main]")]
    public void SetTitle(string prefsLabel, string gitBranch, int gitDirtyCount, int gitPushCount, int gitPullCount, bool isRefreshing, string expected)
    {
        var descriptor = new ApplicationTitleDescriptor("Unity", "Editor", "6000.0.32f1", "Personal", false) { title = "Unity" };

        XEditor.Title.prefsLabel = prefsLabel;
        XEditor.Title.gitBranch = gitBranch;
        XEditor.Title.gitDirtyCount = gitDirtyCount;
        XEditor.Title.gitPushCount = gitPushCount;
        XEditor.Title.gitPullCount = gitPullCount;
        XEditor.Title.isRefreshing = isRefreshing;

        XEditor.Title.SetTitle(descriptor);

        Assert.That(descriptor.title, Is.EqualTo(expected));
    }

    [Test]
    public async Task Refresh()
    {
        XEditor.Title.isRefreshing = false;
        await XEditor.Title.Refresh();

        var prefsDirty = !XFile.HasFile(XPrefs.Asset.File) || !XPrefs.Asset.Keys.MoveNext() ? "*" : "";
        var expectedPrefs = $"[Prefs{prefsDirty}: {XEnv.Author}/{XEnv.Channel}/{XEnv.Version}/{XEnv.Mode}/{XLog.Level()}]";
        Assert.That(XEditor.Title.prefsLabel, Is.EqualTo(expectedPrefs), "Should update preferences label");

        var gitResult = await XEditor.Cmd.Run("git", print: false, args: new string[] { "rev-parse", "--git-dir" });
        if (gitResult.Code == 0) Assert.That(XEditor.Title.gitBranch, Is.Not.Empty); // 在 Git 仓库中
        else Assert.That(XEditor.Title.gitBranch, Is.Empty); // 不在 Git 仓库中
    }
}
#endif
