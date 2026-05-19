<p align="center" style="margin-bottom: 0px !important;">
  <img width="512" src="https://github.com/Stellution-Studios/Veldrith/blob/master/docs/icon_big.png?raw=true" alt="Logo" align="center">
</p>

# Veldrith 🚀
![Sponza](https://i.imgur.com/p6juqm9.jpg)

__Veldrith__ is a high-performance, cross-platform graphics library for **.NET**, built on top of modern low-level APIs. It provides a clean, unified interface over **D3D12**, **Vulkan**, and **Metal**.

---

# ✨ About

Veldrith is a from-scratch rework focused on modern graphics APIs only — no legacy backends, no compromises. It targets the three major low-level APIs that matter today:

- 🟦 **D3D12** — Windows
- 🔴 **Vulkan** — Windows, Linux, Android
- 🍎 **Metal** — macOS, iOS

Veldrith is built on top of [ppy/Veldrid](https://github.com/ppy/veldrid) and [NeoVeldrid.SPIRV](https://github.com/ppy/Veldrid.SPIRV), stripping out legacy backends and modernizing the codebase around D3D12, Vulkan, and Metal exclusively.

---

# 🪙 Installation - [Nuget](https://www.nuget.org/packages/Veldrith)

```
dotnet add package Veldrith --version [VERSION]
```

# 📖 Build from Source

> 1. Clone this repository.
> 2. Add `Veldrith.csproj` as a reference to your project.

---

# 💻 Supported Platforms

|      | D3D12 | Vulkan | Metal |
| :--- | :---: | :----: | :---: |
| [<img src="https://github.com/user-attachments/assets/f8b66880-9037-4ba8-acc4-6ea390e1dde9" alt="Windows" width="54" height="54" align="center">](https://www.microsoft.com/windows) Windows | ✔️ | ✔️ | ❌ |
| [<img src="https://github.com/user-attachments/assets/814ce8c3-5242-47f4-a51b-b185680d38ff" alt="Linux" width="54" height="54" align="center">](https://www.ubuntu.com/) Linux | ❌ | ✔️ | ❌ |
| [<img src="https://github.com/user-attachments/assets/99605868-0590-42ce-a72a-f6feb1cabf6e" alt="macOS" width="54" height="54" align="center">](https://www.apple.com/macos/) macOS | ❌ | 🔶 | ✔️ |
| [<img src="https://github.com/user-attachments/assets/8ec16850-3a1e-42e1-b35e-cf3d3ea32d46" alt="Android" width="54" height="54" align="center">](https://www.android.com/) Android | ❌ | ⚠️ | ❌ |
| [<img src="https://github.com/user-attachments/assets/a0f33f2f-bd7c-4049-a207-85d3a67bef78" alt="iOS" width="54" height="54" align="center">](https://www.apple.com/ios/) iOS | ❌ | 🔶 | ✔️ |

🔶 - Requires [MoltenVK](https://github.com/KhronosGroup/MoltenVK) as a translation layer from Vulkan to Metal.

⚠️ - Android is not tested. Use at your own risk.

---

# 🙏 Based On

Veldrith would not exist without the great work of:

- [**ppy/Veldrid**](https://github.com/ppy/veldrid) — the original cross-platform graphics library this project is forked and reworked from
- [**NeoVeldrid.SPIRV**](https://github.com/ppy/Veldrid.SPIRV) — SPIR-V shader compilation support

---

# 🧑 Contributors
<a href="https://github.com/Stellution-Studios/Veldrith/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Stellution-Studios/Veldrith&max=500&columns=20&anon=1" />
</a>

---

# ✉️ Reach us
[<img src="https://github.com/MrScautHD/Sparkle/assets/65916181/87b291cd-6506-4fb5-b032-abf3170a28c4" alt="discord" width="186" height="60">](https://discord.gg/7XKw6YQa76)
