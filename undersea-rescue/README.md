# 海底救援

首页可选「开始潜水」自由探索海域、「海底救援」游玩连续剧情，或打开「海洋动物图鉴」。救援进度在抵达路线节点时保存在浏览器中，下次可选「继续海底救援」。

- **救援故事**　循声游到礁口，遇见幼鲸泡泡，陪它穿过峡口寻找鲸鱼妈妈。母子重逢后，礁石后面传来敲击声。绕过礁肩找到小翼，托稳横梁，看它游出笼子。静音时可以跟随声纹与字幕。点按画面下沿的方向提示可转向目的地。
- **海洋动物图鉴**　可选择十五个观察对象，包括鲸鱼妈妈和幼鲸、海龟、海月水母、小丑鱼、狮子鱼和蝠鲼。拖动画面可绕着动物观察。在图鉴或自由潜水中双击游动的动物（手机双点）即可同游，移动鼠标或划动屏幕引导它转弯，再双击海水画面即可退出。

鲸鸣使用美国国家公园管理局在阿拉斯加冰川湾采集的座头鲸水下录音，经淡入淡出和响度处理，保留原速和音高。游戏中的寻亲对白为虚构剧情。[原始录音与频谱](https://www.nps.gov/subjects/sound/sounds-humpback-whale.htm)

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

在图鉴内点潜水图标，或在救援结尾选「一起潜水」。WASD 前后左右游动，拖动转向，空格或 E 上浮，Q 下潜，Shift 加速。方向键也可环顾。触屏使用左侧模拟摇杆前后左右游动，摇杆推得越远速度越快。右手滑动转头，右下按钮可上浮和下潜。在图鉴中按 WASD 也可以暂时离开观察镜头，再点潜水图标返回。

珊瑚花园、水母湾和鲸游峡口由同一条可游动的海底水道相连。潜水时可沿礁肩自由观察珊瑚群落。

本版使用原项目的 Reef、ReefShelf、CoralCluster 搭建礁体、礁台和丛生珊瑚，保留原 GLB 的贴图，并减面以适配浏览器。海龟、小丑鱼、水母、蝠鲼、鲸、蓝吊鱼与丛生海藻也来自原项目。新建的立体网孔海扇、叶片珊瑚、鹿角状珊瑚、脑珊瑚和软珊瑚用于填充礁缘与近景。海扇从生成纹理重建，模型制作脚本另需 scipy、scikit-image 和 trimesh。自由潜水中的摄像机检测地形、旧礁体三角面、新珊瑚的近似碰撞体以及大型动物的避让范围。

[原始模型目录](https://github.com/luoqing/underwater-ui/tree/main/Assets/Resources/Models) · [原场景布置代码](https://github.com/luoqing/underwater-ui/blob/main/Assets/Scripts/UnderwaterWorld.cs)

## 珊瑚礁配色

开场图片 assets/reef-color-reference.jpg 是生成的浅水礁配色参考。游戏保留旧模型的色图与法线纹理，按局部高度、表面明暗和丛间差异为活组织着色。新增的拟花鮨和丝蝴蝶鱼有独立纹理与游泳动画。水中远景逐渐偏蓝，潜水员近处有暖白补光。

## 公开试玩与录像

- [妙搭试玩](https://x9rh6mrc62.feishuapp.com/app/app_17j9p095fkf)
- [Sites 试玩](https://undersea-rescue.lqtsing.chatgpt.site/)
- [轻配乐版 · 手机横屏游戏原声](https://github.com/luoqing/underwater-ui/releases/tag/undersea-rescue-audio-v5)
- [连续救援试玩 · 初版混音](https://github.com/luoqing/underwater-ui/releases/tag/undersea-rescue-journey-v4)
- [前版手机横屏游戏原声实录](https://github.com/luoqing/underwater-ui/releases/tag/undersea-rescue-atlas-v3)

在本目录运行 `npm ci` 和 `npm run build`。打开生成的 `index.html` 即可游玩，也可以把 `play.html` 和 `assets/` 部署到静态网站。
