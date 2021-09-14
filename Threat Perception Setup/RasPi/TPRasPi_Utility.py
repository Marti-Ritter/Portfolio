from enum import Enum, auto
import socket


class Instructions(Enum):
    Ready = auto()
    Start_Trial = auto()
    Set_Disk = auto()
    Tube_Reached = auto()
    Trial_Aborted = auto()
    Tube_Reset = auto()
    End_Trial = auto()
    Sending_Records = auto()
    Stop_Experiment = auto()


class Phases(Enum):
    Trial = auto()
    Reward = auto()
    Inter_Trial = auto()


def socket_setup():
    # Create a Server Socket
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind(('', 30000))
    server_socket.listen(1)

    # Create a data-transfer Socket
    data_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    data_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    data_socket.bind(('', 40000))
    data_socket.listen(1)

    # Create a Matlab-communication Socket
    matlab_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    matlab_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    matlab_socket.bind(('', 50000))
    matlab_socket.listen(1)

    return server_socket, data_socket, matlab_socket
