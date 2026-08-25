# SkillsManager 技能管理器

**中文** | [English](README.md)

一个 Windows 桌面端的 **AI 智能体技能(Skill)管理器**,统一管理:

- **Microsoft Copilot Cowork** 技能(企业 OneDrive 同步的 `文档\Cowork\Skills`)
- **Claude Code** 技能(`%USERPROFILE%\.claude\skills`)
- **OpenAI Codex** 技能(`%CODEX_HOME%\skills`,默认 `%USERPROFILE%\.codex\skills`)
- **任意自定义目录** —— 只要遵循「一个技能一个文件夹,里面一个 `SKILL.md`」的通用约定

这些智能体都用**文件夹名**作为技能身份,每个文件夹里的文件都叫 `SKILL.md` ——
在资源管理器里手工维护极易出错。SkillsManager 给它们一个真正的编辑器。

## 下载安装

前往 [Releases 页面](https://github.com/RootaAI/SkillsManager/releases)
下载最新的 `SkillsManager-vX.Y.Z-win-x64.zip`,解压后双击
`SkillsManager.exe` 即可运行 —— 自带运行时,**无需安装 .NET**。

### 首次运行提示"Windows 已保护你的电脑"?

这是 SmartScreen 对未签名新程序的例行提示,不是病毒警告(本项目完全开源,
代码就在这个仓库里,欢迎审阅)。通过方法:

1. 点击提示框中的 **"更多信息"**
2. 点击 **"仍要运行"**

只需操作一次,之后不再提示。

## 功能

- **多技能库切换** —— 左上角下拉框在 Copilot Cowork / Claude Code / Codex
  及自定义库之间切换,自动记住上次所在的库。
- **自定义技能库** —— ⚙ 按钮添加任意 `<根目录>\<技能名>\SKILL.md`
  结构的文件夹(比如团队共享盘、项目仓库里的 `.claude/skills`)。
- **技能列表** —— 每个技能文件夹一行,按名称即时过滤。
- **就地编辑 SKILL.md** —— 显式保存(按钮或 **Ctrl+S**),切换/关闭时
  未保存内容有确认保护,自动换行适合 Markdown 长文。
- **外部修改保护** —— 文件在磁盘上被改过(OneDrive 从其他设备同步、
  或被别的程序编辑)时,保存前会提示,不会静默覆盖别人的版本。
- **粘贴安全** —— 从浏览器 / VS Code / Copilot 复制的 Markdown 常带
  LF 换行,原生文本框会粘成一整行;本工具拦截所有粘贴路径并归一化,
  列表结构不会被破坏。
- **新建技能** —— 创建 `<库根目录>\<名称>\SKILL.md`(整棵目录树首次使用
  时自动创建);Windows 非法文件夹字符、保留设备名(CON、NUL 等)、
  尾部点号等陷阱全部提前拦截。
- **打开文件夹 / 刷新** —— 一键跳到资源管理器;外部改动后重新扫描。
- **操作日志** —— `%LOCALAPPDATA%\SkillsManager\SkillsManagerLog.txt`
  记录 SKILL-CREATE / SKILL-SAVE(含库标识)。
- **刻意不做删除** —— 删除请走"打开文件夹"→ 资源管理器,可进回收站、
  可用 OneDrive 版本历史恢复,比应用内永久删除安全。

## 技能库位置解析

**Copilot Cowork**(第一个存在的候选生效;都不存在时用第一候选,首次保存时创建):

1. `<文档>\Cowork\Skills` —— `SpecialFolder.MyDocuments`,自动跟随
   OneDrive known Folder Move(企业环境通常就是 OneDrive 路径)
2. `%OneDriveCommercial%\Documents\Cowork\Skills`
3. `%OneDrive%\Documents\Cowork\Skills`
4. 兜底:`%LOCALAPPDATA%\SkillsManager\Cowork\Skills`

**Claude Code**:`%USERPROFILE%\.claude\skills` ·
**Codex**:`%CODEX_HOME%\skills`(未设置则 `%USERPROFILE%\.codex\skills`)

自定义库保存在 `%LOCALAPPDATA%\SkillsManager\settings.json`(人类可读可编辑)。

## 从源码构建

.NET 8 SDK,Windows 目标:

```
dotnet build SkillsManager.sln
```

(非 Windows 构建机加 `-p:EnableWindowsTargeting=true`,且需微软官方 SDK
而非发行版精简包。)运行测试:

```
dotnet test SkillsManager.Core.Tests
```

应用零 NuGet 依赖;全部 UI 代码化创建 —— 无 Designer.cs、无 .resx。
可测逻辑(库解析、设置、名称校验、换行归一化)在跨平台的
`SkillsManager.Core` 中,由 xunit 覆盖。

---

由 **Roota AI** 打造 · 小红书:**若塔AI**
