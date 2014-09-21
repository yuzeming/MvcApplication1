#!/usr/bin/python
# -*- coding: UTF-8 -*-
import os
import sys

#服务器位置
WebServer = 'http://localhost'

#指定根目录位置
ROOTDIR = sys.path[0]

#编译器配置
CompileConf = {
#名称 ：[编译命令 运行命令 文件扩展名]（部分编译器依赖于扩展名）
    "c": ["gcc $(SRC) -O2 -o $(EXE)", "$(EXE)", "c"],
    "cpp": ["g++ $(SRC) -O2 -o $(EXE)", "$(EXE)", "cpp"],
    "pas": ["fpc $(SRC) -O2 -o $(EXE)", "$(EXE)", "pas"],
}

#编译时间内存限制（以避免超大静态数组 int arr[100000000]={0}）

CompileLimits = [30, 512]  # S M

#交互密钥

JudgeKey = "judge1"

#设定各个工作目录

SrcDir = os.path.join(ROOTDIR, "src")
TempDir = os.path.join(ROOTDIR, "temp")
DataDir = os.path.join(ROOTDIR, "data")
ComparerDir = os.path.join(ROOTDIR, "cmp")

#默认比较器

DefaultCompareConf = ["Diff", "$(IN)", "$(OUT)", "$(ANS)"]

