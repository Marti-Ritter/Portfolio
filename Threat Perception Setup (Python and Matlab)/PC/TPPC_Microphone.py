from multiprocessing import Process
import pyaudio
import time


def microphone_test(device_id, pa):
    pa = pyaudio.PyAudio()

    """given a device ID and a rate, return TRUE/False if it's valid."""
    try:
        info = pa.get_device_info_by_index(device_id)
        if not info["maxInputChannels"] > 0:
            return False
        stream = pa.open(format=pyaudio.paInt16, channels=1,
                         input_device_index=device_id, frames_per_buffer=4096,
                         rate=int(info["defaultSampleRate"]), input=True)
        stream.close()
        return True

    except Exception:
        return False


def get_available_microphones(known_microphones, host_api=0):
    pa = pyaudio.PyAudio()
    microphones = []
    for i in range(pa.get_device_count()):
        devinfo = pa.get_device_info_by_index(i)
        if any(valid_names in devinfo['name'] for valid_names in known_microphones) and microphone_test(i, pa) and \
                devinfo["hostApi"] == host_api:
            microphones.append(i)
    return microphones


class MicrophoneProcess(Process):
    def __init__(self, process_id, alive_flag, recording_flag, check_list, saving_location, meta_dict, current_output,
                 device_id):
        super(MicrophoneProcess, self).__init__()
        self.process_id = process_id
        self.alive_flag = alive_flag
        self.recording_flag = recording_flag
        self.check_list = check_list
        self.saving_location = saving_location
        self.device_id = device_id

        self.pa = None
        self.stream = None

        self.buffer = b''
        self.current_output = current_output
        self.meta = meta_dict
        self.saving_process = None

    def run(self):
        self.pa = pyaudio.PyAudio()
        self.stream = self.pa.open(format=pyaudio.paInt32,
                                   channels=1,
                                   rate=384000,
                                   input=True,
                                   input_device_index=self.device_id,
                                   frames_per_buffer=4096)

        print('microphone ' + str(self.device_id))

        self.check_list[self.process_id] = 0
        self.alive_flag.wait()
        time_reference = time.perf_counter()

        while self.alive_flag.is_set():
            self.current_output[self.process_id] = self.stream.read(4096, exception_on_overflow=False)

            if self.recording_flag.is_set():
                self.buffer += self.current_output[self.process_id]

            if self.check_list[self.process_id]:
                self.meta['save'] = time.time()
                self.meta['delta'] = time.perf_counter() - time_reference
                if self.saving_process and self.saving_process.is_alive():
                    self.saving_process.join()
                self.saving_process = AudioSaveWorker(
                    saving_location=rf'{self.saving_location.value}_mic{self.device_id}.wav',
                    nchannels=1,
                    sample_width=self.pa.get_sample_size(pyaudio.paInt32),
                    frame_rate=384000,
                    record=self.buffer)
                self.saving_process.start()
                print(f'microphone {self.device_id} has recorded: {len(self.buffer)} samples')
                self.buffer = b''
                self.check_list[self.process_id] = 0
                print(self.check_list)

        self.stream.stop_stream()
        self.stream.close()
        self.pa.terminate()

        if self.saving_process and self.saving_process.is_alive():
            self.saving_process.join()
