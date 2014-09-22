#!/usr/bin/python
# -*- coding: UTF-8 -*-
from string import strip
import copy
import types
import urllib
import zipfile
from Conf import *
from Win_R import *
from HashFile import *
from Web import *
import traceback

ShowError = True
logger = None

def GetConf(name):
    """
    读取name文件中的json
    """
    try:
        ret = json.loads(file(name).read())
    except ValueError:
        raise
        ret = None
    return ret


def SaveToFile(text, name):
    f = open(name, "w")
    f.write(text)
    f.close()


def Split(s):
    '''
    按空格拆分参数，暂时不能处理双引号
    '''
    if type(s) == types.StringType or type(s) == types.UnicodeType:
        return s.split(" ")
    else:
        return copy.deepcopy(s)


def Replace(L, M):
    for i in range(len(L)):
        if M.has_key(L[i]):
            L[i] = M[L[i]]


def Compile(Src, Exe, conf):
    conf = Split(conf)
    Replace(conf, {"$(SRC)": Src, "$(EXE)": Exe})
    pp = CreatePipe()
    Ret = Exec(conf, CompileLimits, [None, pp[1], pp[1]])
    return [Ret[0], ReadPipe(pp)]


def GetData(name, dir , sha):
    f = os.path.join(DataDir,str(name) + ".zip")
    shafn = os.path.join(DataDir, str(name) + ".sha256")
    fsha = ""
    if os.path.exists(shafn):
        fsha = file(shafn).read()
    url = GetDataURL(JudgeKey, name)
    if fsha != sha:
        try:
            urllib.urlretrieve(url, f)
        except IOError:
            return False
        fsha = HashFile(open(f, "rb"))
        if fsha != sha:
            return False
        SaveToFile(fsha, shafn)
        if os.path.exists(dir):
            os.rmdir(dir)
        os.mkdir(dir)
        zipFile = zipfile.ZipFile(f)
        zipFile.extractall(dir)
        zipFile.close()
        mk = os.path.join(dir, "Makefile")
        if os.path.exists(mk):
            e = Exec(["make"], RootDir=dir)
            if not e[0]:
                return False
    return True


def Decode(str,code):
    for c in code:
        try:
            return str.decode(c)
        except UnicodeError:
            pass
    return None

def Judge(s):
    Ret = {
        "ID": s["ID"],
        "Score": 0,
        "State": "",
        "Result": [],
        "CompilerRes": "",
    }
    try:
        DataRoot = os.path.join(DataDir, str(s["ProbID"]))
        DataConfFile = os.path.join(DataRoot, "config.json")
        if not GetData(s["ProbID"], DataRoot,s["ProbCheckSum"]):
            Ret["State"] = u"SystemError"
            Ret["CompilerRes"] = u"数据未找到"
        ProbConf = GetConf(DataConfFile)
        if ProbConf is None:
            Ret["State"] = u"SystemError"
            Ret["CompilerRes"] = u"数据配置无法读取"
        if Ret["State"]:
            return Ret

        Src = os.path.join(SrcDir, str(s["ID"]) + "." + CompileConf[s["Lang"]][2])
        Exe = os.path.join(TempDir, str(s["ID"])) + ExeExt

        SaveToFile(s["Source"], Src)

        tmp,Ret["CompilerRes"] = Compile(Src, Exe, CompileConf[s["Lang"]][0])
        if not tmp:
            Ret["State"] = u"CompileError"
            return Ret

        RunCmd = Split(CompileConf[s["Lang"]][1])
        Replace(RunCmd, {"$(EXE)": Exe})
        if "Compare" in ProbConf:
            CompareConf = Split(ProbConf["Compare"])
        else:
            CompareConf = Split(DefaultCompareConf)
        if os.path.exists(os.path.join(DataRoot, CompareConf[0])):
            CompareConf[0] = os.path.join(DataRoot, CompareConf[0])
        else:
            CompareConf[0] = os.path.join(ComparerDir, CompareConf[0])
        OutputFileName = os.path.join(TempDir, "output.txt")
        DataConf = ProbConf["Data"]

        for Data in DataConf:
            Input = os.path.join(DataRoot, "data", Data[0])
            Output = os.path.join(DataRoot, "data", Data[1])

            Limit = Data[2:4]
            inf = win32file.CreateFileW(Input, win32file.GENERIC_READ, win32file.FILE_SHARE_READ, sa, win32file.OPEN_EXISTING, 0, None)
            ouf = win32file.CreateFileW(OutputFileName, win32file.GENERIC_WRITE, 0, sa, win32file.CREATE_ALWAYS, 0, None)
            pp = [inf, ouf, None]
            DataRes = Exec(RunCmd, Limit, pp, TempDir)
            win32file.CloseHandle(inf)
            win32file.CloseHandle(ouf)
            if DataRes[0]:
                if not os.path.exists(OutputFileName):
                    DataRes[0:2] = [0, u"输出文件未找到"]
                else:
                    tmpCC = copy.deepcopy(CompareConf)
                    Replace(tmpCC, {"$(IN)": Input, "$(OUT)": OutputFileName, "$(ANS)": Output, })
                    pp = CreatePipe()  # (read_end,write_end)
                    CRet = Exec(tmpCC, pp=[None, pp[1], None])
                    CRes = Decode(ReadPipe(pp),["gbk","utf-8"])
                    DataRes[0:2] = [0, u"ValidatorError"]
                    if CRet[0]:
                        tmpList = CRes.split(" ", 1)
                        tmpList[0] = float(tmpList[0])
                        if 0 <= tmpList[0] <= 1:
                            DataRes[0] = tmpList[0] * float(Data[4])
                            DataRes[1] = strip(tmpList[1])
                            if tmpList[0] == 1:
                                DataRes[1] = u"Accepted"
                            else:
                                DataRes[1] = DataRes[1] or u"WrongAnswer"
                                Ret["State"] =  Ret["State"] or u"WrongAnswer"
                    #Clean up
                    DelFile(OutputFileName)
            if DataRes[1] != u"Accepted" and not Ret["State"] :
                Ret["State"] = DataRes[1]
            Ret["Score"] += DataRes[0]
            Ret["Result"].append(DataRes)
        DelFile(Exe)
        if not Ret["State"]:
            Ret["State"]=u"Accepted"
    except Exception as e:
        if logger:
            logger.error(str(e))
            logger.error(traceback.format_exc())
        if ShowError:
            raise
        Ret["State"] = u"SystemError"

    return Ret


isRunning = True


def Main():
    """
    主程序
    """
    for x in [SrcDir,DataDir,TempDir,ComparerDir]:
        if not os.path.isdir(x):
            os.mkdir(x)
    while isRunning:
        try:
            s = GetSubmit(JudgeKey)
            if s is not None:
                Res = Judge(s)
                PostRes(JudgeKey, Res)
            else:
                sleep(5)
        except Exception as e:
            if logger:
                logger.error(str(e))
                logger.error(traceback.format_exc())
            if ShowError:
                raise
            sleep(10)
            


if __name__ == '__main__':
    Main()