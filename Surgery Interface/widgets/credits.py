from qtpy.QtWidgets import QDialog, QLineEdit, QPushButton, QLabel, QVBoxLayout
from brainrender_gui.style import style, update_css
from importlib import metadata


class CreditsWindow(QDialog):
    left = 250
    top = 250
    width = 400
    height = 300

    def __init__(self, main_window, palette):
        """
            Creates a new window for credits.

            Arguments:
            ----------

            main_window: reference to the App's main window
            palette: main_window's palette, used to style widgets
        """
        super().__init__()
        self.setWindowTitle("Credits")
        self.ui()
        self.main_window = main_window
        self.setStyleSheet(update_css(style, palette))

    def ui(self):
        """
            Define UI's elements
        """
        self.setGeometry(self.left, self.top, self.width, self.height)

        layout = QVBoxLayout()

        # Brainrender
        brainrender_meta = metadata.metadata('brainrender')
        brainrender_credits = QLabel(self)
        brainrender_credits.setText(
            f"{brainrender_meta['Name']} ({brainrender_meta['Version']}) by {brainrender_meta['Author']}\n"
            f"{brainrender_meta['Summary']}\nHomepage: {brainrender_meta['Home-page']}")
        brainrender_credits.setObjectName("PopupLabel")

        # Brainrender GUI
        # Package is not distributed
        gui_credits = QLabel(self)
        gui_credits.setText(
            "Brainrender-GUI (not published) by Federico Claudi and Luigi Petrucco\n"
            "A GUI built on brainrender: visualise brain regions, neurons and labelled cells.\n"
            "Github: https://github.com/brainglobe/bg-brainrender-gui"
        )
        gui_credits.setObjectName("PopupLabel")

        # Vedo
        vedo_meta = metadata.metadata('vedo')
        vedo_credits = QLabel(self)
        vedo_credits.setText(
            f"{vedo_meta['Name']} ({vedo_meta['Version']}) by {vedo_meta['Author']}\n"
            f"{vedo_meta['Summary']}\nHomepage: {vedo_meta['Home-page']}")
        vedo_credits.setObjectName("PopupLabel")

        # VTK
        vtk_meta = metadata.metadata('vtk')
        vtk_credits = QLabel(self)
        vtk_credits.setText(
            f"{vtk_meta['Name']} ({vtk_meta['Version']}) by {vtk_meta['Author']}\n"
            f"{vtk_meta['Summary']}\nHomepage: {vtk_meta['Home-page']}")
        vtk_credits.setObjectName("PopupLabel")

        # Implementation, Clipping, Pipette
        remaining_credits = QLabel(self)
        remaining_credits.setText(
            "Implementation of clipping, pipette and modifications to layout by Marti Ritter\n"
            "Project repository: https://gin.g-node.org/larkumlab/Marti_larkumsoftware/src/projects/injection_interface"
        )
        remaining_credits.setObjectName("PopupLabel")

        layout.addWidget(brainrender_credits)
        layout.addStretch(2)

        layout.addWidget(gui_credits)
        layout.addStretch(2)

        layout.addWidget(vedo_credits)
        layout.addStretch(2)

        layout.addWidget(vtk_credits)
        layout.addStretch(2)

        layout.addWidget(remaining_credits)

        self.setLayout(layout)
        self.show()
