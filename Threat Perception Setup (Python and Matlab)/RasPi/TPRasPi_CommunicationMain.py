# built-ins
import os

# project modules
from RasPi.TPRasPi_Utility import socket_setup
from RasPi.TPRasPi_BpodCommunication import BpodConnection
from RasPi.TPRasPi_PCCommunication import PCConnection
from RasPi.TPRasPi_InternalCommunication import InternalConnection


if __name__ == '__main__':
    try:
        os.nice(-20)
    except AttributeError:
        # not available on Windows
        pass

    server_socket, data_socket, matlab_socket = socket_setup()

    pc_connection = PCConnection()
    bpod_connection = BpodConnection(server_socket, data_socket, matlab_socket)
    internal_communication = InternalConnection(pc_connection, bpod_connection, screen_pipe)

    control_mode = None

    while True:
        print("Raspberry Pi listening on socket 6666 and on serial.")
        while True:
            if pc_connection.check_pc_control():
                print(f'PC control activated.')
                pc_connection.activate()
                break

            if bpod_connection.check_bpod_control():
                print('Bpod control activated.')
                bpod_connection.activate()
                break

        # Receive data from client and decide which function to call
        while True:
            if pc_connection.active:
                result = pc_connection.process_commands()
                if not result:
                    break

            if bpod_connection.active:
                # BPod communications
                bpod_connection.process_bytes()

                # Backup check for pc control, in case the Bpod dies
                if pc_connection.check_pc_control(override=True):
                    print(f'PC override activated.')
                    pc_connection.activate()
                    bpod_connection.deactivate()

            # internal communications
            internal_communication.process_commands()