from brainrender_gui.ui import UI
from brainrender_gui.style import style, tree_css, update_css

from brainrender_gui_mod.style_mod import additional_styles

from PyQt5.Qt import Qt

from qtpy.QtWidgets import (
    QLabel,
    QVBoxLayout,
    QPushButton,
    QWidget,
    QHBoxLayout,
    QSlider,
    QCheckBox,
    QSplitter,
)


class UIMod(UI):

    hemisphere_names = {1: "right", 2: "left"}

    def __init__(self, **kwargs):
        # MODIFIED: Hide the top root node, this prevents the user from ever removing all actors,
        # and then trying to set properties
        self.treeView.setRootIndex(self.treeView.model().index(0, 0))

        # ADDED: Fill the status bar with the controls and a button to show the credits
        status_bar_splitter = QSplitter(Qt.Horizontal)
        self.status_bar_position = QLabel("Welcome to the injection interface prototype!")
        self.status_bar_position.setObjectName("PropertyName")
        status_bar_splitter.addWidget(self.status_bar_position)

        self.status_bar_location = QLabel("Controls: 9/3 = Up/Down, 4/6 = Left/Right, 8/2 = Forward/Backward")
        self.status_bar_location.setObjectName("PropertyName")
        status_bar_splitter.addWidget(self.status_bar_location)
        status_bar_splitter.setSizes([1, 1])
        self.statusBar().addWidget(status_bar_splitter)

        btn = QPushButton('Credits', self)
        btn.setObjectName('CreditsButton')
        btn.clicked.connect(self.open_credits_dialog)
        self.statusBar().addPermanentWidget(btn)

        self.label_checkbox.clicked.connect(self.toggle_label)

        self.auto_clip_checkbox.clicked.connect(self.toggle_auto_clip)

        # TODO: Have to fix the original style sheet somehow
        self.treeView.setStyleSheet(update_css(tree_css, self.palette))
        self.statusBar().setStyleSheet(update_css(style + additional_styles, self.palette))
        self.actor_menu.setStyleSheet(update_css(style + additional_styles, self.palette))

        # Doing this once loads the atlas, and reduces later calls to this and similar functions
        self.scene.atlas.structure_from_coords((0, 0, 0))

    def initUI(self):
        super(UIMod, self).initUI()

        # MODIFIED: set the right navbar to a property so it can be toggled
        self.actor_menu = self.centralWidget().children()[1].children()[0]

        # ADDED: actor menu is hidden by default
        self.actor_menu.setHidden(True)

    # ADDED: Top menu bar with two buttons, previous widget gets added below the new top menu
    def make_central_column(self):
        central_column = super().make_central_column()

        layout = QVBoxLayout()
        hlayout = QHBoxLayout()

        btn = QPushButton('Show structures tree', self)
        btn.setObjectName('new_show_structures_tree')
        self.buttons['new_show_structures_tree'] = btn
        hlayout.addWidget(btn)

        btn = QPushButton('Show actor menu', self)
        btn.setObjectName('show_actor_menu')
        self.buttons['show_actor_menu'] = btn
        hlayout.addWidget(btn)

        widget = QWidget()
        widget.setObjectName("Top_buttons")
        widget.setLayout(hlayout)

        layout.addWidget(widget)
        layout.addWidget(central_column)

        widget = QWidget()
        widget.setObjectName("NewCentralColumn")
        widget.setLayout(layout)

        return widget

    def make_right_navbar(self):
        navbar_widget = super().make_right_navbar()
        navbar_layout = navbar_widget.layout()

        # Delete the last 3 elements, which are the old toggle for the treeview, its label, and the final spacer
        # Reverse order, since we take always the last item
        spacer = navbar_layout.takeAt(navbar_layout.count() - 1)
        structure_tree_button = navbar_layout.takeAt(navbar_layout.count() - 1)
        structure_tree_button.widget().setParent(None)
        structure_tree_button_label = navbar_layout.takeAt(navbar_layout.count() - 1)
        structure_tree_button_label.widget().setParent(None)
        del structure_tree_button_label, structure_tree_button, spacer

        self.label_checkbox = QCheckBox("Show Label")
        self.label_checkbox.setTristate(on=False)
        navbar_layout.addWidget(self.label_checkbox)

        # Add label
        lbl = QLabel("Clipping")
        lbl.setObjectName("LabelWithBorder")
        navbar_layout.addWidget(lbl)

        self.auto_clip_checkbox = QCheckBox("Auto-Clip")
        self.auto_clip_checkbox.setTristate(on=False)
        self.auto_clip_checkbox.setChecked(True)
        navbar_layout.addWidget(self.auto_clip_checkbox)

        prop_lbl = QLabel("Reference point")
        prop_lbl.setObjectName("PropertyName")
        navbar_layout.addWidget(prop_lbl)

        # ADDED: Button to change the clipping reference point
        btn = QPushButton('Brain', self)
        btn.setObjectName('switch_reference_point')
        self.buttons['switch_reference_point'] = btn
        navbar_layout.addWidget(btn)

        # Add label
        lbl = QLabel("Injection")
        lbl.setObjectName("LabelWithBorder")
        navbar_layout.addWidget(lbl)

        self.injection_label = QLabel(f"Injection volume: {0 : 3} nl")
        self.injection_label.setObjectName("PropertyName")
        navbar_layout.addWidget(self.injection_label)

        self.injection_slider = QSlider(Qt.Horizontal, self)
        self.injection_slider.setRange(0, 500)
        self.injection_slider.setFocusPolicy(Qt.NoFocus)
        self.injection_slider.setPageStep(5)
        navbar_layout.addWidget(self.injection_slider)

        # add a stretch
        navbar_layout.addStretch()

        return navbar_widget

    def toggle_label(self):
        aname = self.actors_list.currentItem().text()
        checked = self.label_checkbox.isChecked()
        # Toggle checkbox
        if not checked:
            self.store[aname]['Label'].VisibilityOff()
        else:
            self.store[aname]['Label'].VisibilityOn()

        # Fake a button press to force canvas update
        self.scene.plotter.interactor.MiddleButtonPressEvent()
        self.scene.plotter.interactor.MiddleButtonReleaseEvent()

    def toggle_auto_clip(self):
        self.auto_clip = self.auto_clip_checkbox.isChecked()
        if self.auto_clip:
            self.update_clippers()

    def update_status_bar(self):
        # get location of the pipette tip
        pipette_tip_position = self.pipette_dict['pipette'].polydata().GetPoint(self.pipette_dict['pipette_tip_id'])
        # compare this to bregma
        relative_position = [a-b for a, b in zip(pipette_tip_position, self.pipette_dict['bregma'])]

        # Write the position to the status bar
        self.status_bar_position.setText(f"Bregma: {round(-relative_position[0] / 1000, 2):.2f}, "
                                         f"{'Left' if relative_position[2] < 0 else 'Right'}:"
                                         f"{round(abs(relative_position[2]) / 1000, 2):.2f}, "
                                         f"Depth: {round(relative_position[1] / 1000, 2):.2f} (in mm)")

        # Fetch region and hemisphere
        try:
            region = self.scene.atlas.structure_from_coords(pipette_tip_position, microns=True)
            hemisphere = self.scene.atlas.hemisphere_from_coords(pipette_tip_position, microns=True)
            self.status_bar_location.setText(f"Region: {self.scene.atlas.structures[region]['acronym']}, "
                                             f"Hemisphere: {self.hemisphere_names[hemisphere]}")

        except (IndexError, KeyError):
            self.status_bar_location.setText(f"Region and Hemisphere not available")
