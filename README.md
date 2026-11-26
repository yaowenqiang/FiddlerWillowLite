# FiddlerWillowLite

[![CI](https://github.com/yaowenqiang/FiddlerWillowLite/actions/workflows/ci.yml/badge.svg)](https://github.com/yaowenqiang/FiddlerWillowLite/actions/workflows/ci.yml)
[![Release](https://github.com/yaowenqiang/FiddlerWillowLite/actions/workflows/release.yml/badge.svg)](https://github.com/yaowenqiang/FiddlerWillowLite/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

一个参考 Fiddler Willow 插件实现的 **Fiddler Classic** 扩展：以项目为单位管理 Host 映射规则，一键切换整套环境，并在独立标签页中查看抓包与过滤结果。

> A lightweight Willow-like extension for Fiddler Classic: project-based host mapping with one-click environment switching, plus a built-in traffic capture and filter tab.

## 功能特性

- **项目 / 环境一键切换**：用树形结构管理项目，勾选项目即启用整套规则；多个项目启用时按自上而下第一个命中生效。
- **Host 映射**：域名 → 指定 `IP` 或 `IP:端口`，支持 `*.example.com` 通配。通过 Fiddler 的 `x-overrideHost` 改写连接目标，不改 Host 请求头，并设置 `bypassGateway`。
- **抓包与过滤**：独立列表记录请求方法、域名、路径、状态码、耗时和命中项目；支持关键字过滤、仅看命中规则、暂停、清空，双击复制 URL。
- **配置持久化**：自动保存到 `%APPDATA%\FiddlerWillowLite\config.json`，改动即时写入。
- **无需 Visual Studio**：自带脚本化构建流程，一键编译、校验、安装。

## 环境要求

- Windows
- **Fiddler Classic**（已测试 6.0.20261.7291 / 6.0.20262.10021）
- 构建方式二选一：
  - `bootstrap.py` + `build.py`（只需 Python 3，无需 VS）
  - Visual Studio + .NET Framework 4.7.2 开发工具包

## 安装插件

1. 获取 `FiddlerWillowLite.dll`：
   - **推荐**：从 [Releases](https://github.com/yaowenqiang/FiddlerWillowLite/releases) 下载最新版本的 zip 压缩包（含 DLL、README 和 LICENSE），解压取用。
   - 或按下方步骤自行编译。
2. 复制到 Fiddler 的扩展目录：

   ```
   %USERPROFILE%\Documents\Fiddler2\Scripts\FiddlerWillowLite.dll
   ```

3. **完全退出并重启 Fiddler Classic**。
4. Fiddler 6 会弹出 `Caution: Unverified Extension Detected`，请点击 **Always allow**（插件未做代码签名，这是 Fiddler 的正常安全提示）。
5. 标签栏出现 **Willow Lite** 即安装成功。

> 注意：每次重新编译 DLL 后文件哈希变化，Fiddler 会再次弹出上述对话框，需要重新点一次 `Always allow`。

## 使用说明

1. 打开 **Willow Lite** 标签。
2. 左侧点击 **新增项目**，填写项目名称。
3. 选中项目后点击 **新增 Host**，填写域名与目标地址，例如：
   - 域名：`api.example.com`，目标：`127.0.0.1:8080`
   - 域名：`*.test.local`，目标：`10.0.0.5`
4. 勾选项目节点即可启用该项目下的全部规则；取消勾选即关闭。
5. 「启用 Host 映射」总开关可一键停用所有映射。
6. 右侧抓包列表可按关键字过滤、只看命中规则，双击行复制完整 URL。
7. 配置保存在 `%APPDATA%\FiddlerWillowLite\config.json`，可直接查看或备份。

示例配置：

```json
{
  "MappingEnabled": true,
  "Projects": [
    {
      "Name": "本地开发",
      "Enabled": true,
      "Rules": [
        { "Domain": "api.example.com", "Target": "127.0.0.1:8080", "Enabled": true }
      ]
    }
  ]
}
```

## 从源码构建

### 方式一：脚本构建（推荐，无需 Visual Studio）

```bash
python bootstrap.py        # 仅首次：从 NuGet 下载 Roslyn，复制 .NET Framework 引用
python build.py --test     # 编译 → 反射校验 → 7 项行为测试 → 安装到 Fiddler Scripts
```

其他常用命令：

```bash
python build.py                       # 编译 + 校验 + 安装
python build.py --no-install          # 只编译，不安装
python build.py -c Release            # 发布版本
python build.py --fiddler-ref "D:\path\Fiddler.exe"   # 指定 Fiddler 引用
python build.py --install-dir "D:\custom\Scripts"     # 指定安装目录
```

Windows 也可直接运行 `build.bat`，Git Bash 下用 `./build.sh`。

`bootstrap.py` 只准备 `.toolchain/` 中的第三方二进制（Roslyn 编译器、.NET Framework 引用程序集），这些文件不纳入版本控制。`build.py` 会自动优先使用本机已安装的 Fiddler Classic 作为引用。

### 方式二：Visual Studio

用 Visual Studio 2017+ 打开 `FiddlerWillowLite.csproj`（目标框架 .NET Framework 4.7.2）。工程会自动探测常见安装目录下的 `Fiddler.exe`；若安装在自定义位置，可传：

```
msbuild FiddlerWillowLite.csproj /p:FiddlerExe="D:\path\Fiddler.exe"
```

编译后 `PostBuildEvent` 会把 DLL 自动复制到 `%USERPROFILE%\Documents\Fiddler2\Scripts\`。

## 测试

`python build.py --test` 会在 shim 环境中运行 7 项行为测试，覆盖：

- 精确域名映射写入 `x-overrideHost`
- 命中后 `bypassGateway = true`
- 会话列表自定义列写入项目名
- `*.test.local` 通配符映射并自动补端口
- 禁用规则不生效
- 未命中域名不改写
- 映射总开关关闭时不应用

测试脚本会自动备份并还原 `%APPDATA%\FiddlerWillowLite\config.json`，不会污染真实配置。

## CI 与自动发布

仓库内置 GitHub Actions：

- **CI**（`.github/workflows/ci.yml`）：push 到 `main`、PR 或手动触发时，在 `windows-latest` 上准备工具链、下载 Fiddler 引用、编译、`--test` 全套行为测试，并上传 DLL 构建产物。
- **Release**（`.github/workflows/release.yml`）：推送 `v*` 标签时触发，从标签名写入程序集版本（`v0.1.0` → `0.1.0.0`），编译并测试通过后打包 `FiddlerWillowLite-<tag>.zip`，自动创建 GitHub Release 并生成更新说明。

CI 环境与本机不同，没有预装 Fiddler Classic。`fetch_fiddler_ref.py` 会从 Telerik 官方地址下载安装包，提取内部的 `Fiddler.exe` 作为编译引用（不随仓库分发，避免授权问题）。本地如果已安装 Fiddler，`build.py` 会优先使用本机版本，无需运行该脚本。

维护者发布新版本：

```bash
git tag v0.1.0
git push origin v0.1.0
```

推送后 Release 工作流会自动完成编译、测试与发布。

## 诊断

插件会在 `%TEMP%\FiddlerWillowLite-debug.log` 记录构造与 `OnLoad` 阶段的关键步骤。如果标签没有出现，先看这个日志，再看 Fiddler 的 **Log** 标签页。

## 已知限制

- 第一版**不包含** Mock 响应、请求重定向和限速模拟。
- HTTPS 场景下 `x-overrideHost` 会影响 SNI，映射到纯 IP 时个别站点可能报证书错误（Fiddler 本身限制）。
- 插件未做代码签名，Fiddler 6 每次更新 DLL 后都会提示未验证扩展。
- Fiddler 启动时可能单独弹出 `AppContainer Configuration` 提示，与插件无关，点 `No` 关闭即可。
- 仅支持 Windows / Fiddler Classic，不支持 Fiddler Everywhere。

## 目录结构

```
FiddlerWillowLite/
├── bootstrap.py             # 工具链引导（首次构建前运行）
├── build.py                 # 一键编译 / 校验 / 安装
├── build.bat / build.sh     # 构建脚本包装
├── fetch_fiddler_ref.py     # CI 用：下载并提取 Fiddler.exe 编译引用
├── FiddlerWillowLite.csproj # Visual Studio 工程
├── WillowPlugin.cs          # Fiddler 钩子与规则应用
├── WillowView.cs            # 选项卡 UI
├── Models.cs                # 数据模型
├── RuleStore.cs             # JSON 配置读写
├── EditDialog.cs            # 项目/规则编辑对话框
├── Properties/AssemblyInfo.cs
├── tests/                   # shim 与行为测试
├── .github/workflows/       # CI 与 Release 工作流
└── .toolchain/              # 本地工具链（gitignore，由 bootstrap.py 生成）
```

## 许可证

MIT License，详见 [LICENSE](LICENSE)。

本项目为独立实现，与 Telerik / Progress Software 无关联。Fiddler Classic 是 Telerik 的商标产品，其本体不随本仓库分发。
