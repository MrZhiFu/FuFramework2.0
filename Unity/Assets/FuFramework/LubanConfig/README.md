
# 非常强大的配置表解决方案

uban是一个强大、易用、优雅、稳定的游戏配置解决方案。它设计目标为满足从小型到超大型游戏项目的简单到复杂的游戏配置工作流需求。

luban可以处理丰富的文件类型，支持主流的语言，可以生成多种导出格式，支持丰富的数据检验功能，具有良好的跨平台能力，并且生成极快。 luban有清晰优雅的生成管线设计，支持良好的模块化和插件化，方便开发者进行二次开发。开发者很容易就能将luban适配到自己的配置格式，定制出满足项目要求的强大的配置工具。

luban标准化了游戏配置开发工作流，可以极大提升策划和程序的工作效率。

该库主要服务于 `https://github.com/GameFrameX/GameFrameX` 作为子库使用。


# 使用方式(三种方式)
1. 直接在 `manifest.json` 文件中添加以下内容
   ```json
      {"com.gameframex.unity.focus-creative-games.luban": "https://github.com/gameframex/com.gameframex.unity.focus-creative-games.luban.git"}
    ```
2. 在Unity 的`Packages Manager` 中使用`Git URL` 的方式添加库,地址为：https://github.com/gameframex/com.gameframex.unity.focus-creative-games.luban.git

3. 直接下载仓库放置到Unity 项目的`Packages` 目录下。会自动加载识别

# 改动功能

1. 增加 `Packages` 的支持
2. 移除ODIN 的依赖
3. 增加防裁剪的帮助类。需要在启动的主场景中挂载 `LuBanCroppingHelper` 脚本即可

# 使用文档

https://luban.doc.code-philosophy.com/docs/intro