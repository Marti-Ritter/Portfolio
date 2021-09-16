# built-ins
from collections import deque
import random as rdm

# externals
import pygame


class MarkerLayer:
    def __init__(self, parent, marker_distribution, marker_height,
                 image_location, mirrored=True):
        assert isinstance(parent, pygame.Surface), \
            'Parent must be a Pygame surface.'
        assert isinstance(marker_distribution, tuple) and len(marker_distribution) == 3 and \
               all(i > 0 for i in marker_distribution), \
            'The marker distribution must be a int-tuple of length 3 (left of screen, on screen, right of screen).'
        assert isinstance(marker_height, int) and marker_height > 0, \
            'The marker height must be an integer above 0.'
        assert isinstance(image_location, list) and len(image_location) > 0 and \
               all(isinstance(location, str) for location in image_location), \
            'The image location must be a list of strings, which show the possible images for the markers.'
        assert isinstance(mirrored, bool), \
            'The mirrored flag must be a boolean deciding whether the markers are mirrored at the middle of the screen.'

        self.parent = parent
        self.image_location = image_location
        self.mirrored = mirrored
        self.markers = deque(maxlen=sum(marker_distribution))

        if self.mirrored:
            self.marker_height = min(parent.get_height() / 2, marker_height)
        else:
            self.marker_height = min(parent.get_height(), marker_height)

        _surf = [None] * sum(marker_distribution)
        _rect = [None] * sum(marker_distribution)

        for i in range(sum(marker_distribution)):
            loaded_image = pygame.image.load(image_location[rdm.randint(0, len(image_location) - 1)])
            zoom_factor = self.marker_height / loaded_image.get_height()
            if self.mirrored:
                _surf[i] = [pygame.transform.rotozoom(loaded_image, 0, zoom_factor),
                            pygame.transform.flip(pygame.transform.rotozoom(loaded_image, 180, zoom_factor),
                                                  True, False)]
            else:
                _surf[i] = pygame.transform.rotozoom(loaded_image, 0, zoom_factor)

        self.space = parent.get_width() / marker_distribution[1]
        self.left_spawn = -(marker_distribution[0] * self.space)
        self.right_spawn = parent.get_width() + marker_distribution[2] * self.space

        self.location_pointer = 0.5 * self.space

        for i in range(sum(marker_distribution)):
            if self.mirrored:
                _rect[i] = [_surf[i][0].get_rect(), _surf[i][1].get_rect()]
                _rect[i][0].center = [(-marker_distribution[0] + i + 0.5) * self.space, parent.get_height() * 1 / 4]
                _rect[i][1].center = [(-marker_distribution[0] + i + 0.5) * self.space, parent.get_height() * 3 / 4]
            else:
                _rect[i] = _surf[i].get_rect()
                _rect[i].center = [(-marker_distribution[0] + i + 0.5) * self.space, parent.get_height() * 2 / 4]

            wobble_x_abs = int(self.space / 3)
            wobble_x_random = rdm.randint(-wobble_x_abs, wobble_x_abs)

            if self.mirrored:
                wobble_y_abs = int(parent.get_height() / 4 - _rect[i][0].height / 2)
                wobble_y_random = rdm.randint(-wobble_y_abs, wobble_y_abs)
                _rect[i] = [_rect[i][0].move(wobble_x_random, wobble_y_random),
                            _rect[i][1].move(wobble_x_random, -wobble_y_random)]
                self.markers.append([[_surf[i][0], _rect[i][0]], [_surf[i][1], _rect[i][1]]])

            else:
                wobble_y_abs = int((parent.get_height() / 2) - _rect[i].height / 2)
                wobble_y_random = rdm.randint(-wobble_y_abs, wobble_y_abs)
                _rect[i] = _rect[i].move(wobble_x_random, wobble_y_random)
                self.markers.append([_surf[i], _rect[i]])

        self.move_and_blit(0)

    def spawn_marker(self, spawn_right):
        if spawn_right:
            lag_correction = self.location_pointer
            spawn = self.right_spawn + lag_correction
        else:
            lag_correction = self.location_pointer - self.space
            spawn = self.left_spawn + lag_correction

        loaded_image = pygame.image.load(self.image_location[rdm.randint(0, len(self.image_location) - 1)])
        zoom_factor = self.marker_height / loaded_image.get_height()
        if self.mirrored:
            _surf = [pygame.transform.rotozoom(loaded_image, 0, zoom_factor),
                     pygame.transform.flip(pygame.transform.rotozoom(loaded_image, 180, zoom_factor),
                                           True, False)]
            _rect = [_surf[0].get_rect(),
                     _surf[1].get_rect()]
            _rect[0].center = [spawn, self.parent.get_height() * 1 / 4]
            _rect[1].center = [spawn, self.parent.get_height() * 3 / 4]
            wobble_y_abs = int(self.parent.get_height() / 4 - _rect[0].height / 2)
            wobble_y_random = rdm.randint(-wobble_y_abs, wobble_y_abs)
        else:
            _surf = pygame.transform.rotozoom(loaded_image, 0, zoom_factor)
            _rect = _surf.get_rect()
            _rect.center = [spawn, self.parent.get_height() * 2 / 4]
            wobble_y_abs = int(self.parent.get_height() / 2 - _rect.height / 2)
            wobble_y_random = rdm.randint(-wobble_y_abs, wobble_y_abs)

        wobble_x_abs = int(self.space / 3)
        wobble_x_random = rdm.randint(-wobble_x_abs, wobble_x_abs)

        if self.mirrored:
            _rect = (_rect[0].move(wobble_x_random, wobble_y_random),
                     _rect[1].move(wobble_x_random, -wobble_y_random))
            new_marker = [[_surf[0], _rect[0]], [_surf[1], _rect[1]]]
        else:
            _rect = _rect.move(wobble_x_random, wobble_y_random)
            new_marker = [_surf, _rect]

        if spawn_right:
            self.markers.append(new_marker)
        else:
            self.markers.appendleft(new_marker)

    def move_and_blit(self, delta):
        self.location_pointer += delta

        update_rectangle_list = []

        for i in range(0, len(self.markers)):
            if self.mirrored:
                old_rect1 = self.markers[i][0][1]
                old_rect2 = self.markers[i][1][1]
                self.markers[i][0][1] = self.markers[i][0][1].move(delta, 0)
                self.markers[i][1][1] = self.markers[i][1][1].move(delta, 0)
                new_rect1 = self.parent.blit(*self.markers[i][0])
                new_rect2 = self.parent.blit(*self.markers[i][1])
                if not new_rect1.width == 0:
                    update_rectangle_list.append(old_rect1.union(new_rect1))
                if not new_rect2.width == 0:
                    update_rectangle_list.append(old_rect2.union(new_rect2))

            else:
                old_rect = self.markers[i][1]
                self.markers[i][1] = self.markers[i][1].move(delta, 0)
                new_rect = self.parent.blit(*self.markers[i])
                if not new_rect.width == 0:
                    update_rectangle_list.append(old_rect.union(new_rect))

        if self.location_pointer < 0 or self.location_pointer >= self.space:
            if self.location_pointer < 0:
                self.spawn_marker(spawn_right=True)
            else:
                self.spawn_marker(spawn_right=False)

            self.location_pointer = self.location_pointer % self.space

        return update_rectangle_list


def scale_rectangles(list_of_rectangles, scale_factor):
    scaled_list = []
    for rect in list_of_rectangles:
        scaled_list.append(pygame.Rect(int(rect.left * scale_factor),
                                       int(rect.top * scale_factor),
                                       int(rect.width * scale_factor),
                                       int(rect.height * scale_factor)))
    return scaled_list
