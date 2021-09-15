from qtpy.QtWidgets import (
    QMainWindow,
    QLabel,
    QVBoxLayout,
    QPushButton,
    QWidget,
    QHBoxLayout,
    QProgressBar,
    QScrollArea,
    QMenu,
    QAction,
)

from qtpy.QtCore import Qt, QThreadPool, Signal, Slot
from qtpy import QtGui

from offline_pyrat_parser.style import palette, style, update_css

from pkg_resources import resource_filename


class UI (QMainWindow):
    buttons = {}

    button_names = [
        "parse pyrat table",
        "create report",
        "create documentation",
    ]

    actions = {}

    progress_signal = Signal(int)
    status_signal = Signal(str)
    output_signal = Signal(str)
    finished_signal = Signal()

    def __init__(self, **kwargs):
        super(UI, self).__init__()

        # set the title and icon of main window
        self.setWindowTitle(f"Offline Pyrat Parser {self._version}")

        logo_path = resource_filename("offline_pyrat_parser", f"icons/logo.jpg")
        self.setWindowIcon(QtGui.QIcon(logo_path))

        # Create UI
        #self.get_icons()
        self.initUI()
        self.make_status_bar()

        self.setStyleSheet(update_css(style, palette))

        self.progress_signal.connect(self.set_status_bar_progress)
        self.status_signal.connect(self.set_status_bar_text)
        self.output_signal.connect(self.write_to_output)
        self.finished_signal.connect(self.enable_buttons)

        self.thread_pool = QThreadPool()

        water_menu = QMenu("Water Control", self)

        water_control_action = QAction('Simulate water control', water_menu, checkable=True)
        water_control_action.setChecked(True)
        self.water_control = True
        self.actions["water_control_action"] = water_control_action
        water_menu.addAction(water_control_action)

        self.menuBar().addMenu(water_menu)

        utilities_menu = QMenu("Additional functions", self)

        create_pdf_action = QAction("Translate folder to .pdf (Windows only)", utilities_menu)
        self.actions["create_pdf_action"] = create_pdf_action
        utilities_menu.addAction(create_pdf_action)

        drug_sheets_action = QAction("Print drug sheets", utilities_menu)
        self.actions["print_drug_sheets"] = drug_sheets_action
        utilities_menu.addAction(drug_sheets_action)

        self.menuBar().addMenu(utilities_menu)

    def water_control_switch(self, value):
        self.water_control = value

    def make_status_bar(self):
        self.status_bar_progress = QProgressBar()
        self.status_bar_progress.setValue(0)
        self.statusBar().addWidget(self.status_bar_progress, stretch=1)

        self.status_bar_text = QLabel('Ready')
        self.status_bar_text.setObjectName('StatusLabel')
        self.statusBar().addWidget(self.status_bar_text, stretch=1)

        self.statusBar().setContentsMargins(20, 0, 0, 0)

    @Slot(int)
    def set_status_bar_progress(self, value):
        self.status_bar_progress.setValue(value)

    @Slot(str)
    def set_status_bar_text(self, text):
        self.status_bar_text.setText(text)

    @Slot(str)
    def write_to_output(self, string):
        self.output.setText(string)

    def disable_buttons(self):
        for button in self.buttons.values():
            button.setDisabled(True)
        self.menuBar().setDisabled(True)

    @Slot()
    def enable_buttons(self):
        for button in self.buttons.values():
            button.setDisabled(False)
        self.menuBar().setDisabled(False)

    def make_buttons(self):
        # Create layout, add canvas and buttons

        # make layout
        layout = QVBoxLayout()

        # Add buttons
        for bname in self.button_names:
            btn = QPushButton(bname.capitalize(), self)
            btn.setObjectName(bname.replace(" ", "_"))
            self.buttons[bname.replace(" ", "_")] = btn
            layout.addWidget(btn)

        layout.setContentsMargins(15, 0, 15, 0)

        # make widget
        widget = QWidget()
        widget.setLayout(layout)

        return widget

    def make_output(self):
        # Create layout, add canvas and buttons
        layout = QHBoxLayout()

        self.output = QLabel()
        self.output.setAlignment(Qt.AlignLeft)
        self.output.setObjectName("Output")

        scroll_area = QScrollArea()
        scroll_area.setWidgetResizable(True)
        scroll_area.setViewportMargins(0, 0, 10, 10)
        scroll_area.setWidget(self.output)

        layout.addWidget(scroll_area)

        # make widget
        widget = QWidget()
        widget.setLayout(layout)

        return widget

    def initUI(self):
        """
            Define UI elements of the app's main window
        """
        # Make overall layout
        main_layout = QHBoxLayout()

        main_layout.addWidget(self.make_buttons(), stretch=1)
        main_layout.addWidget(self.make_output(), stretch=2)

        # Create main window widget
        main_widget = QWidget()
        main_widget.setObjectName("MainWidget")
        main_widget.setLayout(main_layout)
        self.setCentralWidget(main_widget)


if __name__ == '__main__':
    test = UI()