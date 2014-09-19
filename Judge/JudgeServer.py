#!/usr/bin/env python
import os
import sys
import win32service
import win32serviceutil
import main

class PySQOJWebServer(win32serviceutil.ServiceFramework):
    _svc_name_ = "PySQOJ_judge"
    _svc_display_name_ = "PySQOJ Judge Server"
    def SvcDoRun(self):
        main.Main()
    def SvcStop(self):
        self.ReportServiceStatus(win32service.SERVICE_STOP_PENDING)
        main.isRunning=False

if __name__ == '__main__':
    win32serviceutil.HandleCommandLine(PySQOJWebServer)