# built-ins
import select
import socket

# project modules
from RasPi.TPRasPi_InternalCommunication import valid_operations


class PCConnection:
    def __init__(self, server_socket):
        self.server_socket = server_socket
        self.conn = None
        self.active = False

    def activate(self):
        self.active = True

    def deactivate(self):
        self.active = False

    def send_message(self, message):
        self.conn.send(message)

    def check_pc_control(self, override=False):
        ready_to_read, _, _ = select.select([self.server_socket], [], [], 0)
        if len(ready_to_read) > 0:
            self.conn, client = self.server_socket.accept()
            return True
        else:
            return False

    def process_commands(self):
        try:    # check if select fails
            ready_to_read, ready_to_write, _ = select.select([self.conn], [self.conn], [], 1)
            if len(ready_to_read) > 0:
                data_from_client = self.conn.recv(256)
                try:
                    message = data_from_client.decode().split(' ')
                    if len(message) > 1:
                        valid_operations[message[0]](*message[1:])
                    else:
                        if message[0] in ['end', 'shutdown']:
                            self.conn.send("shutdown".encode())
                            self.server_socket.close()
                        valid_operations[message[0]]()
                except KeyError as error:
                    self.conn.send("KeyError occurred. Function invalid.".encode())
                except TypeError as error:
                    self.conn.send("TypeError occurred. Probably wrong count or type of argument.".encode())

        except select.error as error:
            self.conn.close()
            print(f'Select Error: {error}')
            control_mode = None
            self.deactivate()
            return False

        except socket.error as error:
            self.conn.close()
            print(f'Socket Error: {error}')
            control_mode = None
            self.deactivate()
            return False

        return True
