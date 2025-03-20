// Copyright (c) 2025 EFramework Organization. All rights reserved.
// Use of this source code is governed by a MIT-style
// license that can be found in the LICENSE file.

#if UNITY_INCLUDE_TESTS
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using ET.U3D.UTIL;
using EP.U3D.UTIL;

/// <summary>
/// XEditor.Cmd 命令行工具类的单元测试。
/// </summary>
/// <remarks>
/// 测试内容：
/// 1. 命令查找功能
/// 2. 命令执行功能
/// 3. 跨平台兼容性
/// </remarks>
public class TestXEditorCmd
{
    /// <summary>
    /// 测试目录路径。
    /// </summary>
    private string testDir;

    /// <summary>
    /// 测试命令文件路径。
    /// </summary>
    private string testCmd;

    /// <summary>
    /// 测试命令文件名。
    /// </summary>
    private string testName;

    /// <summary>
    /// 测试环境初始化。
    /// </summary>
    /// <remarks>
    /// 执行以下操作：
    /// 1. 创建临时测试目录
    /// 2. 创建测试命令文件
    /// 3. 设置命令文件执行权限(非 Windows 平台)
    /// </remarks>
    [OneTimeSetUp]
    public void Setup()
    {
        // 创建测试目录
        testDir = Path.Combine(Path.GetTempPath(), "TestXEditorCmd");
        if (!XFile.HasDirectory(testDir)) XFile.CreateDirectory(testDir);

        // 创建测试命令文件
        testName = Application.platform == RuntimePlatform.WindowsEditor ? "mytest.cmd" : "mytest";
        testCmd = Path.Combine(testDir, testName);
        File.WriteAllText(testCmd, Application.platform == RuntimePlatform.WindowsEditor ?
            "@echo Hello World\r\n@exit 0" :  // Windows 命令格式
            "#!/bin/bash\necho Hello World\nexit 0");  // Unix 命令格式

        // 非 Windows 平台设置执行权限
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            XEditor.Cmd.Run("/bin/chmod", testDir, false, false, "+x", testCmd).Wait();
        }
    }

    /// <summary>
    /// 测试环境清理。
    /// </summary>
    /// <remarks>
    /// 删除测试过程中创建的临时目录和文件。
    /// </remarks>
    [OneTimeTearDown]
    public void Cleanup()
    {
        if (XFile.HasDirectory(testDir)) XFile.DeleteDirectory(testDir);
    }

    /// <summary>
    /// 测试命令查找功能。
    /// </summary>
    /// <remarks>
    /// 验证以下场景：
    /// 1. 空命令名称：应返回空字符串
    /// 2. 不存在的命令：应返回空字符串
    /// 3. 无路径的命令：应返回空字符串
    /// 4. 指定路径的命令：应返回完整路径
    /// </remarks>
    [Test]
    public void Find()
    {
        Assert.AreEqual(XEditor.Cmd.Find(""), "", "空命令名称应返回空字符串");
        Assert.AreEqual(XEditor.Cmd.Find("nonexistent"), "", "不存在的命令应返回空字符串");
        Assert.AreEqual(XEditor.Cmd.Find(testName), "", "无路径的命令应返回空字符串");
        Assert.AreEqual(XEditor.Cmd.Find(testName, testDir), XFile.NormalizePath(testCmd), "指定路径的命令应返回完整路径");
    }

    /// <summary>
    /// 测试命令执行功能。
    /// </summary>
    /// <param name="print">是否打印输出</param>
    /// <param name="progress">是否显示进度</param>
    /// <param name="args">命令参数</param>
    /// <param name="expectedCode">期望的返回码</param>
    /// <param name="expectedOutput">期望的输出内容</param>
    /// <remarks>
    /// 验证以下场景：
    /// 1. 基本命令执行：验证命令是否能正常执行
    /// 2. 打印输出控制：验证是否正确控制输出打印
    /// 3. 进度显示控制：验证是否正确控制进度显示
    /// 4. 返回值验证：验证命令返回码是否符合预期
    /// 5. 输出内容验证：验证命令输出内容是否符合预期
    /// </remarks>
    [TestCase(false, false, new string[] { }, 0, "Hello World", Description = "验证基本命令执行，无打印输出，无进度显示")]
    [TestCase(true, false, new string[] { }, 0, "Hello World", Description = "验证基本命令执行，启用打印输出，无进度显示")]
    [TestCase(false, true, new string[] { }, 0, "Hello World", Description = "验证基本命令执行，无打印输出，启用进度显示")]
    [TestCase(true, true, new string[] { }, 0, "Hello World", Description = "验证基本命令执行，启用打印输出和进度显示")]
    public async Task Run(bool print, bool progress, string[] args, int expectedCode, string expectedOutput)
    {
        var result = await XEditor.Cmd.Run(bin: testCmd, print: print, progress: progress, args: args);

        Assert.That(result.Code, Is.EqualTo(expectedCode), "命令应返回正确的退出码");
        Assert.That(result.Data.Trim(), Is.EqualTo(expectedOutput), "命令应输出预期的内容");
    }
}
#endif
