import os
import sys

WebServer = 'http://127.0.0.1:8000/xmlrpc'

ROOTDIR = sys.path[0]

CompileConf = {
    "c": ["gcc $(SRC) -O2 -o $(EXE)", "$(EXE)", "c"],
    "cpp": ["g++ $(SRC) -O2 -o $(EXE)", "$(EXE)", "cpp"],
    "pas": ["fpc $(SRC) -O2 -o $(EXE)", "$(EXE)", "pas"],
}

CompileLimits = [30, 512]  # S M

JudgeKey = "judge1"

SrcDir = os.path.join(ROOTDIR, "src")
TempDir = os.path.join(ROOTDIR, "temp")
DataDir = os.path.join(ROOTDIR, "data")
ComparerDir = os.path.join(ROOTDIR, "cmp")

DefaultCompareConf = ["Diff", "$(IN)", "$(OUT)", "$(ANS)"]

