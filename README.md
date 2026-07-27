# DahuaGrab - 大华相机采集插件

DIAVision 平台的大华工业相机采集算子插件，支持 GigE / USB3 Vision 相机的图像采集。

## 功能

- 大华相机自动扫描与连接
- 曝光时间、增益参数调节
- 连续采集 / 软触发 / 硬触发模式
- 单帧采集（GrabOne）
- 图像保存（BMP/JPG/PNG）
- 自动曝光 / 自动增益 / 自动白平衡
- 相机信息读取（型号、固件版本）
- 带重试机制的连接（3次重试，指数退避）
- 文件日志记录（按日期自动归档）
- XML 序列化支持

## 环境要求

- .NET 8.0
- DIAVision 1.6.x（需提供 `DIAVision.Core.dll`）
- 大华 MV Viewer（提供原生 SDK DLL）

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
│       ├── CameraLogger.cs     # 日志 + 重试机制
│       ├── ImageData.cs        # 图像数据结构
│       └── CameraSDK.csproj
├── tests/
│   └── DahuaGrab.Tests/        # 单元测试
└── DahuaGrab.sln
```

## 大华 SDK 位置

SDK 来自大华 MV Viewer 安装目录，需复制以下 DLL 到插件部署目录：

| 源路径 | 文件 |
|--------|------|
| `D:\dahua\MV Viewer\Runtime\x64\` | `MVSDKmd.dll`, `GenApi_MD_VC120_v3_0.dll`, `CLAllSerial_MD_VC120_v3_0.dll`, `CLProtocol_MD_VC120_v3_0.dll` |

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
MVSDKmd.dll
GenApi_MD_VC120_v3_0.dll
CLAllSerial_MD_VC120_v3_0.dll
CLProtocol_MD_VC120_v3_0.dll
```

## 日志

运行日志自动写入：
```
C:\Users\Public\Documents\DMV-IVS\Plugin\DahuaGrab\logs\camera_YYYYMMDD.log
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
| 大华工业相机 SDK | 原生库 `MVSDKmd.dll`，需安装 [大华MV Viewer](https://www.dahuatech.com/) 后复制到 `references/Dahua/` |
| DIAVision.Core | 台达视觉平台核心库（闭源） |
