# [CDR Plugin for SDR#]

## 项目简介
实现一个简单的CDR解调插件。

## 功能特点
实现CDR信号的解调并录制解调后的音频。

提供原始IQ数据录制，用于调试。


## 使用方法
将 libfftw3f-3.dll libcdrRelease.dll 和 SDRSharp.CDR.dll 复制到 SDR# 目录中。修改 Plugins.xml 插件配置，添加 

 ```<add key="CDR" value="SDRSharp.CDR.CDRPlugin,SDRSharp.CDR" /> ``` 


![示例图片](Doc/Screenshort.PNG)

## Todo
通过SDR#播放解调后的音频。

增加CDR的参数显示。