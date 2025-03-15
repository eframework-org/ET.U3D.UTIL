// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using ET.U3D.UTIL;
using UnityEngine;
using EP.U3D.UTIL;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

/// <summary>
/// XEditor.Prefs 模块的单元测试类。
/// </summary>
/// <remarks>
/// 测试范围：
/// 1. 面板生命周期管理
///    - 面板激活和停用
///    - 数据验证和保存
/// 2. 构建处理流程
///    - 首选项验证
///    - 变量求值
///    - 配置清理
/// </remarks>
internal class TestXEditorPrefs
{
    #region Test Class and Handlers

    /// <summary>
    /// 测试用首选项面板类。
    /// </summary>
    /// <remarks>
    /// 用于测试：
    /// 1. 面板生命周期回调
    /// 2. 数据验证机制
    /// 3. 状态跟踪功能
    /// </remarks>
    internal class TestPrefsPanel : XPrefs.IPanel
    {
        public override string Section => "Test";
        public override string Tooltip => "Test Panel";
        public override bool Foldable => true;
        public override int Priority => 0;

        internal static bool onActivateCalled;
        internal static bool onDeactivateCalled;
        internal static bool onSaveCalled;
        internal static bool onApplyCalled;
        internal static bool validateCalled;
        internal static bool shouldValidateSuccess = true;

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement root)
        {
            onActivateCalled = true;
        }

        public override void OnDeactivate()
        {
            onDeactivateCalled = true;
        }

        public override void OnSave()
        {
            onSaveCalled = true;
        }

        public override void OnApply()
        {
            onApplyCalled = true;
        }

        public override bool Validate()
        {
            validateCalled = true;
            return shouldValidateSuccess;
        }

        internal static void Cleanup()
        {
            onActivateCalled = false;
            onDeactivateCalled = false;
            onSaveCalled = false;
            onApplyCalled = false;
            validateCalled = false;
            shouldValidateSuccess = true;
        }
    }

    #endregion

    #region Test Cases
    /// <summary>
    /// 测试首选项面板生命周期。
    /// </summary>
    /// <remarks>
    /// 验证：
    /// 1. 面板激活和停用回调
    /// 2. 数据验证机制
    /// 3. 状态跟踪准确性
    /// </remarks>
    [Test]
    public void Panel()
    {
        TestPrefsPanel.Cleanup();

        var provider = new XEditor.Prefs();
        var testPanel = ScriptableObject.CreateInstance<TestPrefsPanel>();
        provider.panelCache[typeof(TestPrefsPanel)] = testPanel;

        try
        {
            // 测试面板激活
            provider.OnActivate("", new UnityEngine.UIElements.VisualElement());
            Assert.That(TestPrefsPanel.onActivateCalled, Is.True,
                "面板激活时应该触发 OnActivate 回调");

            // 测试数据验证成功
            TestPrefsPanel.shouldValidateSuccess = true;
            Assert.That(provider.Validate(), Is.True,
                "当验证条件满足时，Validate 方法应返回 true");
            Assert.That(TestPrefsPanel.validateCalled, Is.True,
                "执行验证时应该触发 Validate 回调");

            // 测试数据验证失败
            TestPrefsPanel.Cleanup();
            TestPrefsPanel.shouldValidateSuccess = false;
            Assert.That(provider.Validate(), Is.False,
                "当验证条件不满足时，Validate 方法应返回 false");

            // 测试面板停用
            provider.OnDeactivate();
            Assert.That(TestPrefsPanel.onDeactivateCalled, Is.True,
                "面板停用时应该触发 OnDeactivate 回调");
        }
        finally
        {
            provider.panelCache.Remove(typeof(TestPrefsPanel));
        }
    }

    /// <summary>
    /// 测试首选项构建处理。
    /// </summary>
    /// <remarks>
    /// 验证：
    /// 1. 构建前的首选项验证
    /// 2. 变量求值处理
    /// 3. 编辑器配置移除
    /// </remarks>
    [Test]
    public void Build()
    {
        var testPrefsDir = XFile.PathJoin(XEnv.ProjectPath, "Temp", "TestXEditorPrefs");
        var applyFile = XPrefs.IAsset.Uri;

        var handler = new XEditor.Prefs() as XEditor.Event.Internal.OnPreprocessBuild;

        try
        {
            // 准备测试数据
            XPrefs.IAsset.Uri = XFile.PathJoin(testPrefsDir, "test_streaming.json"); // 重定向构建时拷贝的首选项文件

            var originPrefs = new XPrefs.IBase { File = XFile.PathJoin(testPrefsDir, "test_origin.json") };
            originPrefs.Set("test_ref_key", "${Env.ProjectPath}");
            originPrefs.Set("test_const_key@Const", "${Env.LocalPath}");
            originPrefs.Set("test_editor_key@Editor", "editor_value");
            originPrefs.Save();

            // 设置当前首选项
            XPrefs.Asset.File = originPrefs.File;
            XPrefs.Asset.Read();

            // 模拟构建处理
            handler.Process();

            // 验证变量求值
            var processedPrefs = new XPrefs.IBase(encrypt: true); // 读取加密首选项
            processedPrefs.Read(XPrefs.IAsset.Uri);
            Assert.That(processedPrefs.GetString("test_ref_key"), Is.EqualTo(XEnv.ProjectPath),
                "环境变量引用应该被正确求值");
            Assert.That(processedPrefs.GetString("test_const_key@Const"), Is.EqualTo("${Env.LocalPath}"),
                "常量值不应被求值处理");
            Assert.That(processedPrefs.Has("test_editor_key@Editor"), Is.False,
                "编辑器专用配置应该在构建时被移除");

            // 测试不存在的首选项文件
            XPrefs.Asset.File = XFile.PathJoin(testPrefsDir, "test_nonexist.json");
            Assert.Throws<UnityEditor.Build.BuildFailedException>(() => handler.Process(),
                "使用不存在的首选项文件时应抛出构建失败异常");

            // 测试空首选项文件
            XPrefs.Asset.File = XFile.PathJoin(testPrefsDir, "test_empty.json");
            XFile.SaveText(XPrefs.Asset.File, "{}");
            Assert.Throws<UnityEditor.Build.BuildFailedException>(() => handler.Process(),
                "使用空的首选项文件时应抛出构建失败异常");

            // 测试首选项文件读取失败
            XPrefs.Asset.File = XFile.PathJoin(testPrefsDir, "test_invalid.json");
            XFile.SaveText(XPrefs.Asset.File, "invalid_content");
            LogAssert.Expect(LogType.Error, new Regex(@"XPrefs\.Read: load .* with error: Invalid instance\."));
            Assert.Throws<UnityEditor.Build.BuildFailedException>(() => handler.Process(),
                "使用无效的首选项文件时应抛出构建失败异常");
        }
        finally
        {
            if (XFile.HasDirectory(testPrefsDir)) XFile.DeleteDirectory(testPrefsDir); // 删除测试目录

            XPrefs.IAsset.Uri = applyFile; // 恢复首选项文件
            if (string.IsNullOrEmpty(XPrefs.IAsset.Uri)) LogAssert.Expect(LogType.Error, new Regex(@"XPrefs\.Read: load .* with error: Null file for instantiating preferences\."));
            else if (!XFile.HasFile(XPrefs.IAsset.Uri)) LogAssert.Expect(LogType.Error, new Regex(@"XPrefs\.Read: load .* with error: Non exist file .* for instantiating preferences\."));
            XPrefs.Asset.File = XPrefs.IAsset.Uri;
            XPrefs.Asset.Read();
        }
    }

    #endregion
}
#endif
