# Fancy QSlider taken from: https://thesmithfam.org/blog/2010/03/10/fancy-qslider-stylesheet/

additional_styles = """
QCheckBox {
    background-color: BGCOLOR;
    color: TXTCOLOR;
    border-radius: 8px;
    padding: 6px;
    font-size: 14pt;
    margin: 0px 24px;
    min-width: 50px;
}
QPushButton#CreditsButton {
    background-color: FGCOLOR;
    min-width: 200px;
}
QStatusBar {
    color: TXTCOLOR;
    background-color: BGCOLOR;
    font-size: 14pt;
}

QSlider::groove:horizontal {
    border: 1px solid #bbb;
    background: white;
    height: 10px;
    border-radius: 4px;
}

QSlider::sub-page:horizontal {
    background: qlineargradient(x1: 0, y1: 0,    x2: 0, y2: 1,
        stop: 0 #66e, stop: 1 #bbf);
    background: qlineargradient(x1: 0, y1: 0.2, x2: 1, y2: 1,
        stop: 0 #bbf, stop: 1 #55f);
    border: 1px solid #777;
    height: 10px;
    border-radius: 4px;
}

QSlider::add-page:horizontal {
    background: #fff;
    border: 1px solid #777;
    height: 10px;
    border-radius: 4px;
}

QSlider::handle:horizontal {
    background: qlineargradient(x1:0, y1:0, x2:1, y2:1,
        stop:0 #eee, stop:1 #ccc);
    border: 1px solid #777;
    width: 13px;
    margin-top: -2px;
    margin-bottom: -2px;
    border-radius: 4px;
}

QSlider::handle:horizontal:hover {
    background: qlineargradient(x1:0, y1:0, x2:1, y2:1,
        stop:0 #fff, stop:1 #ddd);
    border: 1px solid #444;
    border-radius: 4px;
}
"""
