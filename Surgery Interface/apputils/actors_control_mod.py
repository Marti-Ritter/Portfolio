from brainrender_gui.apputils.actors_control import ActorsControl

class ActorsControlMod(ActorsControl):
    def __init__(self):
        """
            Collection of functions to control actors properties
            and related widget in the GUI
        """
        return

    def actor_list_clicked(self, index):
        super().actor_list_clicked(index)
        # Set label checkbox to current visibility state of label
        self.label_checkbox.setChecked(self.store[self.actors_list.currentItem().text()]['Label'].GetVisibility())
