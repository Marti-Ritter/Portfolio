from brainrender_gui.apputils.regions_control import RegionsControl

from brainrender.Utils.camera import get_camera_params, set_camera_params
from brainrender_gui.widgets.actors_list import remove_from_list
from brainrender_gui.utils import (
    get_region_actors,
    get_color_from_string,
    get_alpha_from_string,
)

from PyQt5.Qt import Qt


class RegionsControlMod(RegionsControl):
    def __init__(self):
        """
            Collections of functions to control the
            addition of regions meshes to the brainrender
            Scene for the GUI
        """
        return

    def add_regions(self, regions, alpha, color):
        # Get params
        alpha = get_alpha_from_string(alpha)
        if alpha is None:
            alpha = brainrender.DEFAULT_MESH_ALPHA

        color = get_color_from_string(color)
        if color == "atlas":
            use_original = True
            colors = None
        else:
            use_original = False
            colors = color

        previous_params = get_camera_params(camera=self.scene.plotter.camera)

        # Add brain regions
        for region in regions:
            item = self.search_for_item_by_text(self.treeView.model().invisibleRootItem(), region)
            if item is None:
                self.statusBar().showMessage(
                    f"The region {region} doesn't seem to belong to the atlas being used. Skipping", 5000)
                continue
            item.setCheckState(Qt.Checked)
            item._checked = True

            checked_parent = self.get_checked_parent(item)
            if checked_parent is not None:
                checked_parent_region = ' '.join(checked_parent.tag.split(' ')[:-1])
            else:
                checked_parent_region = 'root'
            checked_children = self.get_checked_children(item)

            if get_region_actors(self.scene.actors, region) is None:
                self.scene.add_brain_regions(region,
                                             alpha=alpha,
                                             use_original_color=use_original,
                                             colors=colors,
                                             )
                self.add_actors_with_clipping(get_region_actors(self.scene.actors, region), fix=True)

                self.store[checked_parent_region]['PolyDataAppender'].AddInputData(
                    self.store[region]['InvertedMesh'])
                if checked_children:
                    checked_child_regions = [' '.join(child.tag.split(' ')[:-1]) for child in checked_children]
                    for child_region in checked_child_regions:
                        self.store[checked_parent_region]['PolyDataAppender'].RemoveInputData(
                            self.store[child_region]['InvertedMesh'])
                        self.store[region]['PolyDataAppender'].AddInputData(
                            self.store[child_region]['InvertedMesh'])

                self.store[checked_parent_region]['PolyDataAppender'].Update()
                self.store[region]['PolyDataAppender'].Update()

        self.scene.plotter.camera.SetFocalPoint(self.get_reference_point())
        set_camera_params(self.scene.plotter.camera, previous_params)
        self.update_clippers(force_update=True)

        # update
        self._update()

    # FIXED: This now also allows for removal of an actor
    def add_region_from_tree(self, val):
        """
            When an item on the hierarchy tree is double clicked, the
            corresponding mesh is added/removed from the brainrender scene
        """
        # Get item
        idxs = self.treeView.selectedIndexes()
        if idxs:
            item = idxs[0]
        else:
            return
        item = item.model().itemFromIndex(val)

        # MODIFIED: Get region name (seemingly there is no other value to use for this)
        region = ' '.join(item.tag.split(' ')[:-1])

        # Toggle checkbox
        if not item._checked:
            item.setCheckState(Qt.Checked)
            item._checked = True
        else:
            item.setCheckState(Qt.Unchecked)
            item._checked = False

        checked_parent = self.get_checked_parent(item)
        if checked_parent is not None:
            checked_parent_region = ' '.join(checked_parent.tag.split(' ')[:-1])
        else:
            checked_parent_region = 'root'
        checked_children = self.get_checked_children(item)

        previous_params = get_camera_params(camera=self.scene.plotter.camera)

        # Add/remove mesh
        if get_region_actors(self.scene.actors, region) is None:
            # Add region
            self.scene.add_brain_regions(region,)
            self.add_actors_with_clipping(get_region_actors(self.scene.actors, region), fix=True)

            self.store[checked_parent_region]['PolyDataAppender'].AddInputData(
                self.store[region]['InvertedMesh'])
            if checked_children:
                checked_child_regions = [' '.join(child.tag.split(' ')[:-1]) for child in checked_children]
                for child_region in checked_child_regions:
                    self.store[checked_parent_region]['PolyDataAppender'].RemoveInputData(
                        self.store[child_region]['InvertedMesh'])
                    self.store[region]['PolyDataAppender'].AddInputData(
                        self.store[child_region]['InvertedMesh'])

            self.store[checked_parent_region]['PolyDataAppender'].Update()
            self.store[region]['PolyDataAppender'].Update()

        else:
            # remove regions and update list
            # FIXED: it now properly removes the actor
            act = get_region_actors(self.scene.actors, region)
            self.scene.actors.remove(act)
            del self.actors[region]
            remove_from_list(self.actors_list, region)

            self.store[checked_parent_region]['PolyDataAppender'].RemoveInputData(
                self.store[region]['InvertedMesh'])
            if checked_children:
                checked_child_regions = [' '.join(child.tag.split(' ')[:-1]) for child in checked_children]
                for child_region in checked_child_regions:
                    self.store[checked_parent_region]['PolyDataAppender'].AddInputData(
                        self.store[child_region]['InvertedMesh'])

            self.store[checked_parent_region]['PolyDataAppender'].Update()

            clipped_actor = self.store[region]['ClippedMesh']
            cap_actor = self.store[region]['Cap']
            self.scene.plotter.remove([clipped_actor, cap_actor])
            del self.store[region]

        self.scene.plotter.camera.SetFocalPoint(self.get_reference_point())
        set_camera_params(self.scene.plotter.camera, previous_params)
        self.update_clippers(force_update=True)

        # Update hierarchy's item font
        item.toggle_active()

        # Update brainrender scene
        self._update()

    # adapted from https://stackoverflow.com/questions/41949370/collect-all-items-in-qtreeview-recursively
    def search_for_item_by_text(self, root, search_text):
        def recurse(parent, text):
            for row in range(parent.rowCount()):
                for column in range(parent.columnCount()):
                    child = parent.child(row, column)
                    if text in child.tag:
                        return child
                    if child.hasChildren():
                        result = recurse(child, text)
                        if result is not None:
                            return result

        if root is not None:
            return recurse(root, search_text)

    # modified from https://stackoverflow.com/questions/41949370/collect-all-items-in-qtreeview-recursively
    def get_checked_children(self, root):
        def recurse(parent):
            for row in range(parent.rowCount()):
                for column in range(parent.columnCount()):
                    child = parent.child(row, column)
                    if child._checked:
                        yield child
                    if child.hasChildren() and not child._checked:
                        yield from recurse(child)

        if root is not None:
            yield from recurse(root)

    def get_checked_parent(self, root):
        def recurse(child):
            parent = child.parent()
            if parent is not None:
                if parent._checked:
                    return parent
                else:
                    return recurse(parent)
            else:
                return None

        if root is not None:
            return recurse(root)

