from brainrender.Utils.camera import set_camera

import brainrender
from brainrender.Utils.camera import set_camera_params
from brainrender_gui import App
from brainrender_gui.widgets.actors_list import update_actors_list

from brainrender_gui_mod.scene_mod import SceneMod, MyInteractorStyle
from brainrender_gui_mod.widgets.credits import CreditsWindow

from brainrender_gui_mod.ui_mod import UIMod
from brainrender_gui_mod.apputils.regions_control_mod import RegionsControlMod
from brainrender_gui_mod.apputils.actors_control_mod import ActorsControlMod


class AppMod(
    App, SceneMod, UIMod, RegionsControlMod, ActorsControlMod,
):
    store = {}

    fixed_sagittal_camera = dict(
        position=[3998.434612221822, 3745.8134440474814, 48593.38552676014],
        focal=[6587.835, 3849.085, 5688.164],
        viewup=[0.009118000372890485, -0.9999526965279836, -0.003386262779896527],
        distance=42972.44034956067,
        clipping=[36210.473627528314, 58330.894218958376],
    )

    def __init__(self, *args, **kwargs):
        super(AppMod, self).__init__(atlas='allen_mouse_10um', **kwargs)

        # Initialize parent classes
        SceneMod.__init__(self)
        UIMod.__init__(self, **kwargs)
        RegionsControlMod.__init__(self)
        ActorsControlMod.__init__(self)

        # FIXED: if there are any actors at all in the list, select the first one
        # This fixes the error that occurs if the user clicks the properties before an actor is selected
        if self.actors_list.count() > 0:
            self.actors_list.setCurrentRow(0)
            self.actor_list_clicked(0)

        # MODIFIED: set the root alpha in the beginning to 0.5 and write that to the textbox
        self.scene.root.alpha(0.5)
        self.alpha_textbox.setText('0.5')

        # ADDED: Connect the new buttons to their functions
        self.buttons['new_show_structures_tree'].clicked.connect(self.toggle_treeview)
        self.buttons['show_actor_menu'].clicked.connect(self.toggle_actor_menu)
        self.buttons['switch_reference_point'].clicked.connect(self.toggle_reference_point)
        self.injection_slider.valueChanged.connect(self.update_injection_volume)

        self.update_plotter()

    # ADDED: Method to toggle the actor menu (right bar)
    def toggle_actor_menu(self):
        if not self.actor_menu.isHidden():
            self.buttons["show_actor_menu"].setText(
                "Show actor menu"
            )
        else:
            self.buttons["show_actor_menu"].setText(
                "Hide actor menu"
            )

        self.actor_menu.setHidden(not self.actor_menu.isHidden())

    # ADDED: Method to switch the reference point for the clipping
    def toggle_reference_point(self):
        if self.buttons["switch_reference_point"].text() == 'Brain':
            self.buttons["switch_reference_point"].setText(
                'Pipette tip'
            )
            self.current_reference_point = 'pipette_tip'
            self.scene.plotter.camera.SetFocalPoint(self.get_reference_point())
        else:
            self.buttons["switch_reference_point"].setText(
                'Brain'
            )
            self.current_reference_point = 'root'
            self.scene.plotter.camera.SetFocalPoint(self.get_reference_point())
        self.update_clippers(force_update=True)

        # Fake a button press to force canvas update
        self.scene.plotter.interactor.MiddleButtonPressEvent()
        self.scene.plotter.interactor.MiddleButtonReleaseEvent()


    # ADDED: Method to update the simulated injected volume
    def update_injection_volume(self, value):
        self.injection_label.setText(f'Injection volume: {value : 3} nl')
        ink_radius = value * 2
        self.pipette_dict['ink_blob_source'].SetRadius(ink_radius)
        self.pipette_dict['ink_blob_source'].Update()

        # Fake a button press to force canvas update
        self.scene.plotter.interactor.MiddleButtonPressEvent()
        self.scene.plotter.interactor.MiddleButtonReleaseEvent()

    def update_plotter(self):
        self.scene.plotter.camera.AddObserver('ModifiedEvent', self.CameraModifiedCallback)
        self.scene.plotter.interactor.SetInteractorStyle(MyInteractorStyle(self))

    def _update(self):
        """
            Updates the scene's Plotter to add/remove
            meshes
        """
        self.scene.apply_render_style()

        if self.camera_orientation is not None:
            if self.camera_orientation != "sagittal":
                set_camera(self.scene, self.camera_orientation)
            else:
                set_camera_params(self.scene.plotter.camera, self.fixed_sagittal_camera)
            self.camera_orientation = None
            self.scene.plotter.camera.SetFocalPoint(self.get_reference_point())

        # Get actors to render
        self._update_actors()

        # REMOVED: the actors will not be reloaded every single update
        # instead we check which ones were added/removed below
        '''
        to_render = [act for act in self.actors.values() if act.is_visible]

        # Set actors look
        meshes = [act.mesh.c(act.color).alpha(act.alpha) for act in to_render]

        # Add axes
        if self.axes is not None:
            meshes.append(self.axes)

        # update actors rendered
        self.scene.plotter.show(
            *meshes, interactorStyle=0, bg=brainrender.BACKGROUND_COLOR,
        )
        '''

        # ADDED: a quick check whether the plotter was initialized before
        if not self.scene.plotter.initializedPlotter:
            self.scene.plotter.show(interactorStyle=0, bg=brainrender.BACKGROUND_COLOR,)

        # ADDED: make lists of which actors are to render and which ones are already in the scene, to prevent
        # unnecessary updates
        to_show = {key: values for key, values in self.actors.items() if values.is_visible}
        already_rendered = [key for key in self.store.keys()]

        # Render only the new actors
        for actor in already_rendered:
            if actor in to_show:
                self.store[actor]['ClippedMesh'].c(self.actors[actor].color)
                self.store[actor]['ClippedMesh'].alpha(self.actors[actor].alpha)
                self.store[actor]['Cap'].c(self.actors[actor].color)
                self.store[actor]['Cap'].alpha(self.actors[actor].alpha)
            else:
                self.store[actor]['ClippedMesh'].alpha(0)
                self.store[actor]['Cap'].alpha(0)

        # Fake a button press to force canvas update
        self.scene.plotter.interactor.MiddleButtonPressEvent()
        self.scene.plotter.interactor.MiddleButtonReleaseEvent()

        # Update list widget
        update_actors_list(self.actors_list, self.actors)

    def open_credits_dialog(self):
        self.credits_dialog = CreditsWindow(self, self.palette)
