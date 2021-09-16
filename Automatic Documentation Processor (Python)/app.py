from offline_pyrat_parser.ui import UI
from offline_pyrat_parser.apputils.file_parser import FileParser
from offline_pyrat_parser.apputils.report_creator import ReportCreator
from offline_pyrat_parser.apputils.documentation_creator import DocumentationCreator
import sys


def except_hook(cls, exception, traceback):
    sys.__excepthook__(cls, exception, traceback)


class App(UI, FileParser, ReportCreator, DocumentationCreator):
    _version = "v1.0.3 (22.03.2021)"

    def __init__(self, **kwargs):
        """
            Initialise the pyqt5 app.
        """

        sys.excepthook = except_hook

        UI.__init__(self, **kwargs)
        FileParser.__init__(self)
        ReportCreator.__init__(self)
        DocumentationCreator.__init__(self)

        buttons_funcs = dict(
            parse_pyrat_table=self.parse_pyrat_table,
            create_report=self.create_report,
            create_documentation=self.create_documentation,
        )

        for btn, fun in buttons_funcs.items():
            self.buttons[btn].clicked.connect(fun)

        action_funcs = dict(
            water_control_action=self.water_control_switch,
            create_pdf_action=self.translate_folder_to_pdf,
            print_drug_sheets=self.create_drug_sheets,
        )

        for act, fun in action_funcs.items():
            self.actions[act].triggered.connect(fun)

    # ----------------------------------- Close ---------------------------------- #
    def onClose(self):
        pass
