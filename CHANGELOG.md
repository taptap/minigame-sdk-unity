---
uid: changelog
---
# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.18] - 2025-04-21
### Changed
1. 修复UnityEditor.Build.Profile适用的unity版本

## [2.0.17] - 2025-01-09
### Changed
1. 多人联机API名称规范化，对齐移动端命名规范

## [2.0.16] - 2025-12-11
### Fixed
1. 调试工具更新，支持最新版多人联机

## [2.0.15] - 2025.9.24
### Added
1. 新增多人联机模块

## [2.0.14] - 2025.9.24
### Fixed
1. 修复成就注册事件报错bug

## [2.0.13] - 2025.9.23
### Fixed
1. dev tools 配置文件选择时支持导出目录为空

## [2.0.12] - 2025.9.23
### Fixed
1. dev tools支持同时显示BuildProfile和MiniGameConfig

## [2.0.11] - 2025.9.5
### Fixed
1. dev tools优化获取本机IP方式，并增加手动输入IP选项

## [2.0.10] - 2025.9.2
### Fixed
1. dev tools去掉离线模式参数

## [2.0.9] - 2025.9.1

### Added
1. 调试客户端支持：排行榜，成就，云存档等功能
2. BuildProfile支持点击查看tap文档

### Fixed
1. 修复音频构建bug
2. 修复构建时CheckTapFSReady报错
3. 修复CreateUserInfoButton缺少参数问题
4. 修复Tap.Getsetting返回内容为空

## [2.0.8] - 2025.8.18

### Added
1. 新增云存档功模块

### Fixed
1. 更新convert版本
2. 修复排行榜API报错


## [2.0.7] - 2025.7.10

### Added
1. 新增Unity调试工具

### Fixed
1. 修复分享接口的报错

## [2.0.6] - 2025.6.5

### Fixed
1. 修复编辑器下和addressable接口冲突问题

### Added
1. 新增创建桌面快捷图标接口

## [2.0.5] - 2025.5.30

### Added
1. 新增成就接口
2. 新增桌面文件夹接口

### Fixed
1. 修复部分接口回调错误

## [2.0.4] - 2025.5.20

### Fixed
1. 修复分包遇到的报错
2. 修复Brotli压缩时的崩溃问题

## [2.0.3] - 2025.5.9

### Fixed
1. 修复TCP/UDP回调的报错
2. 更新 js 中的 TapSDKManagerHandler
3. 重命名js到c#的回调函数名

### Changed
1. 更新启动页logo

### Removed
1. 移除 WebGLTemplates，使用默认 templates

## [2.0.2] - 2025.4.29

### Fixed
1. 修复已知bug

### Changed
1. 更新替换规则

## [2.0.1] - 2025.4.25

### Changed
1. 更新命名空间为TapTapMiniGame，函数前缀为Tap.
2. 适配 Tuanjie 引擎 1.5.0
3. 更新面板

## [2.0.0] - 2025.3.25

### Changed
1. 更新为对 minihost 的依赖
