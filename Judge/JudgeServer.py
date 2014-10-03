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
        main.ShowError = False
        main.logger = self._getLogger()
        main.Main()
    def SvcStop(self):
        self.ReportServiceStatus(win32service.SERVICE_STOP_PENDING)
        main.isRunning=False

    def _getLogger(self):
        import logging
        import os
        import inspect

        logger = logging.getLogger('PythonService')

        this_file = inspect.getfile(inspect.currentframe())
        dirpath = os.path.abspath(os.path.dirname(this_file))
        handler = logging.FileHandler(os.path.join(dirpath, "service.log"))

        formatter = logging.Formatter('%(asctime)s %(name)-12s %(levelname)-8s %(message)s')
        handler.setFormatter(formatter)

        logger.addHandler(handler)
        logger.setLevel(logging.INFO)

        return logger

if __name__ == '__main__':
    win32serviceutil.HandleCommandLine(PySQOJWebServer)