from multiprocessing import Manager
from ctypes import c_wchar_p
import json
import time
import select
import os

from PC.TPPC_UI import launch_gui_worker
from PC.TPPC_Camera import get_available_cameras, BaslerCameraWorker
from PC.TPPC_Microphone import get_available_microphones, MicrophoneProcess
from PC.TPPC_Utils import create_client_socket


if __name__ == '__main__':
    cameras = get_available_cameras()
    microphones = get_available_microphones(["UltraMic"])

    processes = []
    manager = Manager()
    all_alive = manager.Event()
    all_recording = manager.Event()
    check_list = manager.list([1] * (len(cameras) + len(microphones)))
    saving_location = manager.Value(c_wchar_p, r'')
    framerate = manager.Value(float, 100)
    current_output = manager.list([None] * (len(cameras) + len(microphones)))

    device_meta = {}
    trial_meta = {}

    process_id = 0
    processes = {}

    for device_id, device in enumerate(cameras):
        trial_meta[f'cam{device_id}'] = manager.dict()

        p = BaslerCameraWorker(process_id, all_alive, all_recording, check_list, saving_location,
                               trial_meta[f'cam{device_id}'], current_output, device_id, framerate)
        p.start()
        processes[f'cam{device_id}'] = p
        process_id += 1

    for device_id in microphones:
        trial_meta[f'mic{device_id}'] = manager.dict()

        p = MicrophoneProcess(process_id, all_alive, all_recording, check_list, saving_location,
                              trial_meta[f'mic{device_id}'], current_output, device_id)
        p.start()
        processes[f'mic{device_id}'] = p
        process_id += 1

    processes["_GUI"] = launch_gui_worker(all_alive, current_output, processes)

    def saving():
        check_list[:] = [1] * len(check_list)

    def set_location(location):
        folder, file_prefix = location.rsplit('\\', 1)
        if not os.path.exists(folder):
            os.makedirs(folder)
        saving_location.value = location

    def set_framerate(new_rate):
        framerate.value = float(new_rate)

    command_dict = {
        '1': all_recording.set,
        '2': all_recording.clear,
        '3': saving,
        '4': all_alive.clear,
        'set_location': set_location,
        'set_framerate': set_framerate,
    }

    client_socket = create_client_socket('localhost', 30000)

    while sum(check_list) != 0:
        time.sleep(0.1)
    all_alive.set()
    client_socket.send(b'1')

    while True:
        # inbound communication
        ready_to_read, _, _ = select.select([client_socket], [], [], 0.5)
        if len(ready_to_read) > 0:
            dataFromClient = ''
            while True:
                data = client_socket.recv(1).decode()
                if data == '|':
                    break
                dataFromClient += data
            message = dataFromClient.split(' ', 1)
            print(message)
            if len(message) > 1:
                command_dict[message[0]](*message[1:])
            else:
                command_dict[message[0]]()

        if sum(check_list) != 0:
            while sum(check_list) != 0:
                time.sleep(0.1)
            with open(rf'{saving_location.value}_meta.json', 'w') as outfile:
                copy_of_trial_meta = trial_meta.copy()
                for key in copy_of_trial_meta.keys():
                    copy_of_trial_meta[key] = dict(copy_of_trial_meta[key])
                    trial_meta[key].clear()
                json.dump(copy_of_trial_meta, outfile)
            client_socket.send(b'1')

        if not all_alive.is_set():
            for process in processes:
                process.join()
            location_split = saving_location.value.rsplit('\\', 3)
            with open(rf'{location_split[0]}\{location_split[1]}\{location_split[0]}_meta.json', 'w') as outfile:
                json.dump(device_meta, outfile)
            client_socket.send(b'1')
            break
