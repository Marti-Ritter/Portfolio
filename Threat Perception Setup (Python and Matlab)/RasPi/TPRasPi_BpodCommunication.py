# built-ins
import serial
import struct
import time

# project modules
from RasPi.TPRasPi_Commands import valid_operations, byte_operations


class BpodConnection:
    def __init__(self, server_socket, data_socket, matlab_socket):
        self.firmwareVersion = 1
        self.moduleName = "RaspbPi"
        self.ser = serial.Serial("/dev/ttyS0", 1312500)
        self.current_json = None
        self.sockets = {
            "server": server_socket,
            "data": data_socket,
            "matlab": matlab_socket,
        }
        self.active = False

    def activate(self):
        self.active = True

    def deactivate(self):
        self.active = False

    def bpod_echo(self):
        # This code returns a self-description to the state machine.
        Msg = struct.pack('B', 65)  # Acknowledgement
        Msg += struct.pack('I', self.firmwareVersion)  # Firmware version as 32-bit unsigned int
        Msg += struct.pack('B', len(self.moduleName))  # Length of module name
        Msg += struct.pack(str(len(self.moduleName)) + 's', self.moduleName.encode('utf-8'))  # Module name
        Msg += struct.pack('B', 0)  # 0 to indicate no more self description to follow
        self.ser.write(Msg)

    def check_bpod_control(self):
        bytesAvailable = self.ser.in_waiting
        if bytesAvailable > 0:
            return True
        else:
            return False

    def send_message(self, message):
        matlab_conn, _ = self.sockets["matlab"].accept()
        matlab_conn.send(message)

    def send_struct(self, encoding, message):
        self.ser.write(struct.pack(encoding, message))

    def set_json(self, new_json):
        self.current_json = new_json

    def process_bytes(self):
        bytesAvailable = self.ser.in_waiting
        if bytesAvailable > 0:
            inByte = self.ser.read()
            unpackedByte = struct.unpack('B', inByte)
            print(f'{inByte} = {unpackedByte[0]}')
            if unpackedByte[0] == 20:
                if self.current_json is not None:
                    print(f"Sending json:{self.current_json}")
                    matlab, _ = self.sockets["data"].accept()  # Bpod is too dumb for this
                    matlab.send(self.current_json.encode('utf-8'))
                    self.current_json = None
                else:
                    matlab, _ = self.sockets["data"].accept()  # Bpod is too dumb for this
                    matlab.send('{}'.encode('utf-8'))
            elif unpackedByte[0] == 254:
                message_string = ''
                next_letter = ''
                time.sleep(0.25)
                if ser.in_waiting:
                    while next_letter != '|':
                        message_string += next_letter
                        next_letter = struct.unpack('s', self.ser.read())[0].decode()
                    message = message_string.split(' ')
                    if len(message) > 1:
                        valid_operations[message[0]](*message[1:])
                    else:
                        valid_operations[message[0]]()
            elif unpackedByte[0] == 255:
                self.bpod_echo()
            else:
                try:
                    print(byte_operations[unpackedByte[0]])
                    byte_operations[unpackedByte[0]]()
                except KeyError as error:
                    print("KeyError occurred. Function invalid.")
            self.ser.flush()
