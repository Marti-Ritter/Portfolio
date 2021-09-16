# built-ins
from multiprocessing import Process, Pipe
from subprocess import call

# project modules
from RasPi.TPRasPi_Utility import Instructions
import RasPi.TPRasPi_SetupControl as TPRasPi_SetupControl


record_process = None
with open('TPRasPi_settings.json', 'r') as read_file:
    settings = json.load(read_file)
screen_pipe, at_screen = Pipe()


def init_debug(*args):
    global record_process
    if not record_process:
        record_process = Process(target=TPRasPi_SetupControl.debug_loop,
                                 args=(at_screen, settings))
        record_process.daemon = True
        record_process.start()


def init_experiment(*args):
    global record_process
    if not record_process:
        record_process = Process(target=TPRasPi_SetupControl.experiment_loop,
                                 args=(at_screen, settings))
        record_process.daemon = True
        record_process.start()


def start_trial(*args):
    if record_process:
        screen_pipe.send((Instructions.Start_Trial, True))


def start_trial_no_recording(*args):
    if record_process:
        screen_pipe.send((Instructions.Start_Trial, False))


def set_disk(disk_state, *args):
    if record_process:
        screen_pipe.send((Instructions.Set_Disk, int(disk_state)))


def set_disk0():
    if record_process:
        screen_pipe.send((Instructions.Set_Disk, 0))


def set_disk1():
    if record_process:
        screen_pipe.send((Instructions.Set_Disk, 1))


def set_disk2():
    if record_process:
        screen_pipe.send((Instructions.Set_Disk, 2))


def set_disk3():
    if record_process:
        screen_pipe.send((Instructions.Set_Disk, 3))


def end_trial(*args):
    if record_process:
        screen_pipe.send((Instructions.End_Trial,))


def shutdown_screen(*args):
    global record_process
    if record_process:
        screen_pipe.send((Instructions.Stop_Experiment,))
        print('recorder told to stop')
        record_process.join()
        record_process = None


def get_option(key=None, *args):
    if key:
        try:
            conn.send('{}: {}'.format(key, settings[key]).encode())
        except KeyError:
            conn.send("Unknown setting requested.".encode())
    else:
        conn.send(str(settings).encode())


def set_option(key, value, *args):
    try:
        settings[key] = type(settings[key])(value)
    except ValueError as error:
        conn.send('ValueError occurred. The provided value didnt type-match the existing value.'.encode())
        return
    with open('TPRasPi_settings.json', 'w') as write_file:
        json.dump(settings, write_file)
    conn.send('{}: {}'.format(key, settings[key]).encode())


def end(with_poweroff=False, *args):
    if record_process:
        screen_pipe.send((Instructions.Stop_Experiment,))
        record_process.join()
    if with_poweroff:
        call("sudo shutdown --poweroff now", shell=True)
    else:
        quit()


def shutdown(*args):
    end(with_poweroff=True)


valid_operations = {'end': end,
                    'shutdown': shutdown,
                    'get_option': get_option,
                    'set_option': set_option,
                    'init_debug': init_debug,
                    '1': start_trial,
                    'set_disk': set_disk,
                    '2': end_trial,
                    'shutdown_screen': shutdown_screen,
                    'init_experiment': init_experiment,
                    }


byte_operations = {1: start_trial,
                   10: start_trial_no_recording,
                   2: end_trial,
                   30: set_disk0,
                   31: set_disk1,
                   32: set_disk2,
                   33: set_disk3,
                   4: init_experiment,
                   5: shutdown_screen
                   }
