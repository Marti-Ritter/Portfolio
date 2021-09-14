from multiprocessing import Process
from imageio import get_writer
import wave


class VideoSaveWorker(Process):
    def __init__(self, saving_location, fps, record):
        super(VideoSaveWorker, self).__init__()
        self.writer = None
        self.saving_location = saving_location
        self.fps = fps
        self.record = record

    def run(self):
        self.writer = get_writer(self.saving_location, fps=self.fps)
        print(f'In saving: {len(self.record)}')
        for image in self.record:
            self.writer.append_data(image)
        self.writer.close()
        print(f'saving at {self.saving_location} done.')


class AudioSaveWorker(Process):
    def __init__(self, saving_location, n_channels, sample_width, frame_rate, record):
        super(AudioSaveWorker, self).__init__()
        self.wave_file = None
        self.saving_location = saving_location
        self.n_channels = n_channels
        self.sample_width = sample_width
        self.frame_rate = frame_rate
        self.record = record

    def run(self):
        self.wave_file = wave.open(self.saving_location, 'wb')
        self.wave_file.setnchannels(self.n_channels)
        self.wave_file.setsampwidth(self.sample_width)
        self.wave_file.setframerate(self.frame_rate)
        self.wave_file.writeframes(self.record)
