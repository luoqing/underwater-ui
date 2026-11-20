# 海底救援

网页服务器和手机浏览器使用 play.html。它会在进入海域后加载模型，动物台词按需加载。离线版仍是单文件 index.html。选择“沿鲸歌去救援”，从珊瑚花园亲自游到幼鲸身边，辨认鲸歌，带它穿过背流湾和峡口与母鲸会合，再返回沙坡打捞沉笼。画面下沿显示目标方向和距离，点按即可转向。动物对白在底部以字幕显示。

用支持 WebGL 2 的浏览器打开 index.html 即可离线游玩。桌面和触屏都能拖动画面。两条救援和鱼类观察共用一个实时三维峡谷。

- **雾里的鲸歌**　调谐水听器、比较两个方向的录音、抓住缓流时机，并用舵杆或方向键把幼鲸带过背流湾和峡口。
- **沉笼之下**　选择托举位置，跟随潮流与载荷变化调整托举力度，把笼架抬到蝠鲼能通过的高度。
- **峡谷鱼类图鉴**　选择六种鱼之一，近距离观察体形与游姿，拖动可绕鱼观察。

## 模型与制作

assets 中的 GLB 可导入 Blender 等支持 glTF 的工具。峡谷、砂岩、海带与六种图鉴海鱼为本版新作，六种鱼均包含循环游动动画。鲸与蝠鲼沿用原项目的模型。[原项目](https://github.com/luoqing/underwater-ui)

鱼皮、砂岩、沙地和开场概念图使用生成图像。鱼类外形与栖位参考下列图鉴，此处为游戏化表现。

- [日本竹荚鱼 · 台湾鱼类资料库](https://fishdb.sinica.edu.tw/taxon/381555-fishdb)
- [赤点石斑鱼 · 台湾鱼类资料库](https://fishdb.sinica.edu.tw/e_books/fishshell2011pic.php?id=250)
- [新月锦鱼 · 台湾鱼类资料库](https://fishdb.sinica.edu.tw/taxon/382032-fishdb)
- [牙鲆 · 鸟羽水族馆](https://aquarium.co.jp/picturebook/paralichthys-olivaceus.html)

## 构建

运行 npm ci 和 npm run build 生成独立 HTML。模型纹理、游戏界面和渲染库都嵌在文件内。峡谷建模脚本使用 Python 标准库。鱼类建模脚本需要 NumPy 和 Pillow。

## 珊瑚花园和自由潜水

从首页选“潜入珊瑚花园”。WASD 前后左右游动，拖动转向，空格或 E 上浮，Q 下潜，Shift 加速。方向键也可环顾。触屏使用左侧模拟摇杆前后左右游动，摇杆推得越远速度越快。右手滑动转头，右下按钮可上浮和下潜。在单项任务或图鉴中按 WASD 也可以暂时离开跟随镜头，点“回到救援”或“回到图鉴”继续原进度。

珊瑚花园、水母湾和鲸游峡口由同一条可游动的海底水道相连。潜水时可沿礁肩自由观察珊瑚群落。

本版使用原项目的 Reef、ReefShelf、CoralCluster 搭建礁体、礁台和丛生珊瑚，保留原 GLB 的贴图，并减面以适配浏览器。海龟、小丑鱼、水母、蝠鲼、鲸、蓝吊鱼与丛生海藻也来自原项目。新建的立体网孔海扇、叶片珊瑚、鹿角状珊瑚、脑珊瑚和软珊瑚用于填充礁缘与近景。海扇从生成纹理重建，模型制作脚本另需 scipy、scikit-image 和 trimesh。自由潜水中的摄像机检测地形、旧礁体三角面、新珊瑚的近似碰撞体以及大型动物的避让范围。

[原始模型目录](https://github.com/luoqing/underwater-ui/tree/main/Assets/Resources/Models) · [原场景布置代码](https://github.com/luoqing/underwater-ui/blob/main/Assets/Scripts/UnderwaterWorld.cs)

## 珊瑚礁配色

开场图片 assets/reef-color-reference.jpg 是生成的浅水礁配色参考。游戏保留旧模型的色图与法线纹理，按局部高度、表面明暗和丛间差异为活组织着色。新增的拟花鮨和丝蝴蝶鱼有独立纹理与游泳动画。水中远景逐渐偏蓝，潜水员近处有暖白补光。

## 公开试玩与录像

- [妙搭试玩](https://x9rh6mrc62.feishuapp.com/app/app_17j9p095fkf)
- [Sites 试玩](https://undersea-rescue.lqtsing.chatgpt.site/)
- [手机横屏实录和玩法口播对比](https://github.com/luoqing/underwater-ui/releases/tag/undersea-rescue-voices-v1)

在本目录运行 `npm ci` 和 `npm run build`。打开生成的 `index.html` 即可游玩，也可以把 `play.html` 和 `assets/` 部署到静态网站。
