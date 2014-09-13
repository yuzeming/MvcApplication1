#!/usr/bin/python
 # -*- coding: utf-8 -*-
import json
import requests
import time
from pprint import pprint
__author__ = 'yzm'

url = "http://localhost:54764/api/Judge"
DLurl = "http://localhost:54764/Problem/Downland/%d"


def GetSubmit(jk):
    x = requests.get(url)
    return x.json() if x.status_code == 200 else None

def PostRes(jk, res):
    #pprint(res)
    res["Result"] = json.dumps(res["Result"])
    res["Score"] = int(res["Score"])
    r = requests.post(url, res)

def GetDataURL(jk, name):
    return DLurl % ( name, )