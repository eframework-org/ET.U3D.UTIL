// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using EP.U3D.UTIL;
using ET.U3D.UTIL;

internal class TestXEditorUtility
{
    private string testDirectory;
    private string testAsset;

    [OneTimeSetUp]
    public void Setup()
    {
        // 创建测试目录和文件
        testDirectory = "Assets/Temp/XEditorUtilityTest";
        if (!XFile.HasDirectory(testDirectory))
        {
            XFile.CreateDirectory(testDirectory);
        }
        testAsset = XFile.PathJoin(testDirectory, "test.txt");
        XFile.SaveText(testAsset, "test content");
        AssetDatabase.Refresh();
    }

    [OneTimeTearDown]
    public void Cleanup()
    {
        // 清理测试文件和目录
        if (XFile.HasDirectory(testDirectory))
        {
            XFile.DeleteDirectory(testDirectory);
            AssetDatabase.Refresh();
        }
    }

    [Test]
    public void CollectFiles()
    {
        var files = new List<string>();
        XEditor.Utility.CollectFiles(testDirectory, files, ".meta");
        Assert.That(files, Contains.Item(XFile.NormalizePath(testAsset)));
        Assert.That(files.Count, Is.EqualTo(1));
    }

    [Test]
    public void CollectAssets()
    {
        var assets = new List<string>();
        XEditor.Utility.CollectAssets(testDirectory, assets, ".meta");
        Assert.That(assets, Contains.Item(testAsset));
        Assert.That(assets.Count, Is.EqualTo(1));
    }

    [Test]
    public void CollectDependency()
    {
        var sourceAssets = new List<string> { testAsset };
        var dependencies = XEditor.Utility.CollectDependency(sourceAssets);
        Assert.That(dependencies, Contains.Key(testAsset));
        Assert.That(dependencies[testAsset], Is.Not.Null);
    }

    [Test]
    public void GetSelectedPath()
    {
        var path = XEditor.Utility.GetSelectedPath();
        Assert.That(path, Is.EqualTo("Assets"));
    }

    [Test]
    public void ZipDirectory()
    {
        var zipPath = XFile.PathJoin(testDirectory, "test.zip");
        var result = XEditor.Utility.ZipDirectory(XFile.NormalizePath(testDirectory), XFile.NormalizePath(zipPath));
        Assert.That(result, Is.True);
        Assert.That(XFile.HasFile(zipPath), Is.True);
    }

    [Test]
    public void GetEditorAssembly()
    {
        var assembly = XEditor.Utility.GetEditorAssembly();
        Assert.That(assembly, Is.Not.Null);
        Assert.That(assembly.GetType("UnityEditor.EditorWindow"), Is.Not.Null);
    }

    [Test]
    public void GetEditorClass()
    {
        var clazz = XEditor.Utility.GetEditorClass("UnityEditor.EditorWindow");
        Assert.That(clazz, Is.Not.Null);
        Assert.That(clazz.Name, Is.EqualTo("EditorWindow"));
    }

    [Test]
    public void FindPackage()
    {
        var package = XEditor.Utility.FindPackage();
        Assert.That(package, Is.Not.Null);
    }

    [Test]
    public void ShowToast()
    {
        var content = "Test Toast Message";
        Assert.DoesNotThrow(() => XEditor.Utility.ShowToast(content));
    }
}
#endif