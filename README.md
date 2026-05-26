# DahuaGrab - 大华相机采集插件

DIAVision 平台的大华工业相机采集算子插件，支持 GigE / USB3 Vision 相机的图像采集。

## 功能

- 大华相机自动扫描与连接
- 曝光时间、增益参数调节
- 连续采集 / 软触发 / 硬触发模式
- 单帧采集（GrabOne）
- XML 序列化支持

## 环境要求

- .NET 8.0
- DIAVision 1.6.x（需提供 `DIAVision.Core.dll`）
- 大华工业相机 SDK（`MVSDKmd.dll` 等原生库，已包含在 `references/Dahua/` 中）

## 项目结构

```
DH-development/
├── src/
│   ├── DahuaGrab/              # 大华采集插件
│   │   ├── DahuaGrab.cs        # FlowElement 算子入口
│   │   ├── DahuaCamera.cs      # 相机控制封装
│   │   ├── DahuaInterop.cs     # P/Invoke 原生 SDK 互操作层
│   │   ├── DahuaGrab.csproj
│   │   └── DahuaGrab.json      # 插件描述文件
│   └── Shared/Enums/           # 共享相机接口与枚举
│       ├── ICameraDevice.cs    # 相机统一接口
│       ├── TriggerMode.cs      # 触发模式/像素格式枚举
│       ├── CameraException.cs  # 自定义异常
│       └── CameraSDK.csproj
├── tests/
│   └── DahuaGrab.Tests/        # 单元测试
├── references/
│   ├── DIAVision.Core.dll      # DIAVision 核心库
│   └── Dahua/                  # 大华原生 SDK
│       ├── MVSDKmd.dll
│       ├── GenApi_MD_VC120_v3_0.dll
│       └── ...
└── DahuaGrab.sln
```

## 构建

```bash
dotnet build
```

## 部署

将以下文件复制到 `C:\Users\Public\Documents\DMV-IVS\Plugin\DahuaGrab\`：

```
CameraSDK.dll
DahuaGrab.dll
DahuaGrab.json
（以及 references/Dahua/ 下的所有原生 DLL）
```

## 使用

1. 重启 DIAVision
2. 在算子工具箱 **Camera** 分类下找到 **DahuaGrab**
3. 配置相机序列号、曝光、触发模式等参数
4. 连接相机后执行采集

## 技术说明

大华插件通过 P/Invoke 直接调用大华 SDK 原生库（`MVSDKmd.dll`），不依赖额外的 .NET 封装层。互操作层定义在 `DahuaInterop.cs` 中，包含所有原生函数声明和结构体定义。

## 相关依赖

| 依赖 | 说明 |
|------|------|
| 大华工业相机 SDK | 原生库 `MVSDKmd.dll`，已随项目提供 |
| DIAVision.Core | 台达视觉平台核心库（闭源） |
