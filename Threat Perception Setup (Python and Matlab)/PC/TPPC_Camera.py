from multiprocessing import Process
from pypylon import pylon
import numpy as np
import time


def get_available_cameras():
    tl_factory = pylon.TlFactory.GetInstance()
    return tl_factory.EnumerateDevices()


class RecorderImageEventHandler(pylon.ImageEventHandler):
    def __init__(self, process_id, recording_flag, current_output, meta):
        super(RecorderImageEventHandler, self).__init__()
        self.buffer = []
        self.process_id = process_id
        self.recording_flag = recording_flag
        self.current_output = current_output
        self.meta = meta

    def OnImagesSkipped(self, camera, count_of_skipped_images):
        print("OnImagesSkipped event for device ", camera.GetDeviceInfo().GetModelName())
        print(count_of_skipped_images, " images have been skipped.")
        print()

    def OnImageGrabbed(self, camera, grab_result):
        if grab_result.GrabSucceeded():
            grab_array = grab_result.GetArray()
            self.current_output[self.process_id] = np.array(grab_array)[::10, ::10]
            if self.recording_flag.is_set:
                self.buffer.append(grab_array)
                self.meta[grab_result.ImageNumber] = {
                    'NumberOfSkippedImages': grab_result.NumberOfSkippedImages,
                    'TimeStamp': grab_result.TimeStamp,
                }
        else:
            print("Error: ", grabResult.GetErrorCode(), grab_result.GetErrorDescription())


class BaslerCameraWorker(Process):
    def __init__(self, process_id, alive_flag, recording_flag, check_list, saving_location, meta_dict, current_output,
                 device_id, framerate):
        super(BaslerCameraWorker, self).__init__()
        self.process_id = process_id
        self.alive_flag = alive_flag
        self.recording_flag = recording_flag
        self.check_list = check_list
        self.saving_location = saving_location
        self.device_id = device_id
        self.framerate = framerate

        self.TlFactory = None
        self.devices = None
        self.camera = None

        self.current_output = current_output
        self.meta = meta_dict

        self.ImageEventHandler = None
        self.saving_process = None

    def run(self):
        self.TlFactory = pylon.TlFactory.GetInstance()
        self.devices = self.TlFactory.EnumerateDevices()
        self.camera = pylon.InstantCamera(self.TlFactory.CreateDevice(self.devices[self.device_id]))
        self.ImageEventHandler = RecorderImageEventHandler(self.process_id, self.recording_flag, self.current_output,
                                                           self.meta)
        self.camera.Open()
        pylon.FeaturePersistence.Load("Camera_Settings.pfs",  # Config file with the capturing properties
                                      self.camera.GetNodeMap())
        # https://github.com/basler/pypylon/issues/76
        # https://github.com/basler/pypylon/blob/master/samples/grabusinggrabloopthread.py

        self.camera.TriggerSelector.SetValue("FrameStart")
        self.camera.TriggerMode.SetValue("On")
        self.camera.TriggerSource.SetValue('Line3')

        # The image event printer serves as sample image processing.
        # When using the grab loop thread provided by the Instant Camera object, an image event handler processing the grab
        # results must be created and registered.
        self.camera.RegisterImageEventHandler(self.ImageEventHandler,
                                              pylon.RegistrationMode_Append, pylon.Cleanup_Delete)

        # Start the grabbing using the grab loop thread, by setting the grabLoopType parameter
        # to GrabLoop_ProvidedByInstantCamera. The grab results are delivered to the image event handlers.
        # The GrabStrategy_OneByOne default grab strategy is used.
        self.camera.StartGrabbing(pylon.GrabStrategy_OneByOne, pylon.GrabLoop_ProvidedByInstantCamera)
        print('camera ' + str(self.device_id) + ' model name')
        print(self.devices[self.device_id].GetModelName(), "-", self.devices[self.device_id].GetSerialNumber())

        self.check_list[self.process_id] = 0
        self.alive_flag.wait()

        while self.alive_flag.is_set():
            time.sleep(0.05)

            """            if not self.recording_flag.is_set() and self.camera.TriggerMode.GetValue() != "Off":
                            self.camera.TriggerMode.SetValue("Off")
                            switcher = CameraSwitcher(self.recording_flag, self.camera)
                            switcher.daemon = True
                            switcher.start()
            """
            if self.check_list[self.process_id]:
                self.meta['save'] = time.time()
                if self.saving_process and self.saving_process.is_alive():
                    self.saving_process.join()
                self.saving_process = VideoSaveWorker(rf'{self.saving_location.value}_cam{self.device_id}.mp4',
                                                       self.framerate.value, self.ImageEventHandler.buffer)
                self.saving_process.start()
                print(f'camera {self.device_id} has recorded: {len(self.ImageEventHandler.buffer)} frames')
                self.ImageEventHandler.buffer = []
                self.check_list[self.process_id] = 0
                print(self.check_list)
                self.ImageEventHandler.counter = 0

        self.camera.StopGrabbing()
        self.camera.Close()

        if self.saving_process and self.saving_process.is_alive():
            self.saving_process.join()
