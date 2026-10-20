# 鲸鱼近景视觉复核

这是真实 Editor Play Mode 中的固定相对机位检查，使用生产主相机、原模型/材质/骨骼/漂移/光照/后处理；不是鼠标手动游戏，也不是 WebGL 水色的逐像素对比（Editor 的 Application.absoluteURL 为空）。相机画面为 640×480，不含 IMGUI 遮挡。

## 初次检查有一张异常，不能视为完全通过

- `Logs/bugfix-20261020/whale-visual/whale-side-01.png`：Time=0.581s/frame9。鲸身大块褐黑平色，海底同时呈异常暗纹。
- 原 `whale-side-02.png`：Time=1.031s/frame11。鲸身和海底均正常。

原始两帧和 JSON 均保留，没有删除坏帧。鲸、海底同时异常，不像只发生在鲸网格上的持续破损。

不能用两帧相机高度 7.247/7.475m 解释为穿越水面：生产水面由 `CreateWaterSurface` 放在 y=48m。两相机均远低于该平面。

## 重验

只调整 Editor fixture 的采集时机：等待游戏时间至少3秒，保留首次离屏渲染请求的预热图，再间隔0.3秒以上采集两张。没有修改任何运行时或源资产。

| 采集 | Time / frame | 鲸 z | 相机 y | 目视结果 |
| --- | --- | ---: | ---: | --- |
| recheck/whale-warmup.png | 3.100s / 47 | 110.00 | 7.105 | 鲸和海底正常 |
| recheck/whale-side-01.png | 3.605s / 49 | 111.44 | 7.390 | 正常，尾部连续 |
| recheck/whale-side-02.png | 4.091s / 51 | 112.87 | 7.440 | 正常，尾部姿态变化 |

新三帧均已人工查看，没有复现褐黑片或海底异常暗纹。首次离屏请求本身在充分预热后也是正常，因此不是“任何首次 StandardRequest 必坏”的确定性问题。数据支持早期渲染状态尚未稳定的瞬态，但还没有严格区分纹理上传、渲染全局状态或着色器变体等具体原因。

ShaderUtil.anythingCompiling 在新三帧均为 false。IsPassCompiled(material,0) 在正常帧也均为 false，该 pass 检查不能作为活跃 URP 变体的可靠证据；不能据此把根因直接归为异步着色器编译。

命令执行退出码0，日志：`Logs/bugfix-20261020/whale-visual-recheck.log`。新图片及逐帧参数：`Logs/bugfix-20261020/whale-visual/recheck/`。

结论限定：本次稳定态近景确认鲸身不再呈旧U形褶皱，尾部连续且推进姿态变化，实际沿路线前进。初次0.58秒坏帧仍列为早期采集异常；本复核不是照片级真实感或所有浏览器/显卡均无首帧问题的保证。
