# built-ins
import json

# project modules
from RasPi.TPRasPi_Utility import Instructions


class InternalConnection:
    def __init__(self, pc_connection, bpod_connection, screen_pipe):
        self.pc_connection = pc_connection
        self.bpod_connection = bpod_connection
        self.screen_pipe = screen_pipe

    def process_commands(self):
        if self.screen_pipe.poll():
            message = self.screen_pipe.recv()
            print(message)
            command = message[0]
            arguments = message[1:]
            if command is Instructions.Ready:
                print('screen is ready')
            elif command is Instructions.End_Trial:
                print('Trial end byte sent.')
                if self.pc_connection.active:
                    self.pc_connection.send('0'.encode())
                elif self.bpod_connection.active:
                    self.bpod_connection.send_message(b'0')
            elif command is Instructions.Sending_Records:
                received_dict = arguments[0]
                self.bpod_connection.set_json(json.dumps(received_dict))
                print('RECEIVED ' + str(len(received_dict)) + ' LINES.')
            elif command is Instructions.Trial_Aborted:
                print('trial aborted')
                if self.pc_connection.active:
                    self.pc_connection.send('2'.encode())
                elif self.bpod_connection.active:
                    self.bpod_connection.send_struct('B', 2)
            elif command is Instructions.Tube_Reached:
                print('tube reached')
                if self.pc_connection.active:
                    self.pc_connection.send('1'.encode())
                elif self.bpod_connection.active:
                    self.bpod_connection.send_struct('B', 1)
            elif command is Instructions.Tube_Reset:
                print('tube reset')
                if self.pc_connection.active:
                    self.pc_connection.send('0'.encode())
                elif self.bpod_connection.active:
                    self.bpod_connection.send_message(b'0')
            else:
                raise ValueError(f'Unknown message received: {message}')
