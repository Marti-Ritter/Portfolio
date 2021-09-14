import base64
import datetime
import io

from MyPlots import *

import plotly.graph_objects as go

import dash
from dash.dependencies import Input, Output, State
import dash_core_components as dcc
import dash_html_components as html
import dash_bootstrap_components as dbc
import dash_table

import pandas as pd

external_stylesheets = [dbc.themes.BOOTSTRAP]

app = dash.Dash(__name__, external_stylesheets=external_stylesheets, assets_folder='./assets')
app.config.suppress_callback_exceptions = True  # because we create a callback based on a not yet existing element

server = app.server

app.layout = html.Div([
    dcc.Upload(
        id='upload-data',
        children=html.Div([
            'Drag and Drop or ',
            html.A('Select Files')
        ]),
        style={
            'width': '98%',
            'height': '60px',
            'lineHeight': '60px',
            'borderWidth': '1px',
            'borderStyle': 'dashed',
            'borderRadius': '5px',
            'textAlign': 'center',
            'margin': '1%'
        },
        # Allow multiple files to be uploaded
        multiple=True
    ),
    html.Div(id='output-data-upload'),
])

plot_functions = {
    'volcano_plot': build_volcano_dict,
    'ma_plot': build_ma_dict,
}


def parse_contents(contents, filename, date):
    content_type, content_string = contents.split(',')

    decoded = base64.b64decode(content_string)
    try:
        if 'csv' in filename:
            # Assume that the user uploaded a CSV file
            df = pd.read_csv(io.StringIO(decoded.decode('utf-8')), index_col=0)
        elif 'xls' in filename:
            # Assume that the user uploaded an excel file
            df = pd.read_excel(io.BytesIO(decoded))
        else:
            # Assume the user is stupid
            raise Exception('Wrong file format.')
    except Exception as e:
        print(e)
        return html.Div([
            'There was an error processing this file.'
        ]), 1   # return a 1 if an error occurred during parsing

    initial_frame = 0
    initial_plot = 'volcano_plot'

    list_of_files = df['experiment'].unique().tolist()
    slider_marks = dict((str(index), str(value)) for index, value in enumerate(list_of_files))

    initial_df = df[df['experiment'] == list_of_files[initial_frame]]

    fig_dict = build_volcano_dict(initial_df)
    fig = go.Figure(fig_dict)

    return dbc.Container([
        # storage div (hidden)
        html.Div([
            dcc.Store(id='store',
                      storage_type='memory',
                      data={
                          'current_file': slider_marks[str(initial_frame)],
                          'current_figure': initial_plot,
                          'available_files': list_of_files
                      }),
            dash_table.DataTable(
                id="upload_table",
                data=df.to_dict('records'),
                columns=[{'name': i, 'id': i} for i in df.columns],
            ),
        ],
            style={'display': 'none'}),
        dbc.Row([
            dbc.Col([
                dcc.Tabs(
                    id="plot_selector", value='volcano_plot',
                    parent_className='custom-tabs',
                    className='custom-tabs-container',
                    children=[
                        dcc.Tab(label='Volcano Plot', value='volcano_plot',
                                className='custom-tab', selected_className='custom-tab--selected'),
                        dcc.Tab(label='MA Plot', value='ma_plot',
                                className='custom-tab', selected_className='custom-tab--selected'),
                    ],
                    style={'height': '5vh'}
                ),
                dcc.Graph(
                    id='active_plot',
                    animate=True,
                    animation_options={
                        'transition': {
                            'duration': 750,
                            'ease': 'cubic-in-out',
                        },
                    },
                    config={'scrollZoom':True},
                    figure=fig,
                    style={'height': '85vh'}
                ),
                html.Div([
                    html.Div(id='slider_label'),
                    dcc.Slider(
                        id='slider',
                        dots=True,
                        value=initial_frame,
                        min=0,
                        max=len(list_of_files) - 1,
                        step=1,
                        marks=slider_marks,
                    ),
                ],
                    style={'height': '10vh', 'width': '40vw', 'margin-left': '5vw'}
                ),
            ],
                width=6,
                className="vh-100",
                style={'border': '1px solid lightgrey', 'padding': '0px'},
            ),
            dbc.Col([
                html.Div([
                    html.H5(filename),
                    html.H6(datetime.datetime.fromtimestamp(date)),

                    dash_table.DataTable(
                        id="display_table",
                        data=df[df['experiment'] == slider_marks[str(initial_frame)]].to_dict('records'),
                        columns=[{'name': i, 'id': i} for i in df.columns],
                        fixed_columns={'headers': True, 'data': 1},
                        fixed_rows={'headers': True},
                        style_cell={'textAlign': 'right', 'minWidth': '140px'},
                        style_table={'maxHeight': '40vh', 'minWidth': '100%'},
                    ),
                ],
                    style={'height': '50vh'}
                ),
                html.Div([
                    html.H5(
                        id='selection_label',
                    ),

                    dash_table.DataTable(
                        id="selection_table",
                        data=pd.DataFrame('', index=[0], columns=df.columns).to_dict('records'),
                        columns=[{'name': i, 'id': i} for i in df.columns],
                        fixed_columns={'headers': True, 'data': 1},
                        fixed_rows={'headers': True},
                        style_cell={'textAlign': 'right', 'minWidth': '140px'},
                        style_table={'maxHeight': '40vh', 'minWidth': '100%'},
                    ),
                ],
                    style={'height': '50vh'}
                )
            ],
                width=6,
                className="vh-100",
                style={'border': '1px solid lightgrey'},
            )
        ],
        className="vh-100")
    ],
        className="vh-100",
        fluid=True
    ), 0    # return a 0 if this file has been successfully parsed


@app.callback([Output('output-data-upload', 'children'),
               Output('upload-data', 'style')],
              [Input('upload-data', 'contents')],
              [State('upload-data', 'filename'),
               State('upload-data', 'last_modified'),
               State('upload-data', 'style')])
def update_output(list_of_contents, list_of_names, list_of_dates, upload_style):
    children = []
    if list_of_contents is not None:
        error_sum = 0
        for c, n, d in zip(list_of_contents, list_of_names, list_of_dates):
            parse_result = parse_contents(c, n, d)
            children.append(parse_result[0])
            error_sum += parse_result[1]
        if error_sum == 0:
            upload_style.update({'display': 'none'})
    return children, upload_style


@app.callback(Output('selection_table', 'data'),
              [Input('active_plot', 'selectedData'),
               Input('display_table', 'data')],
              [State('upload_table', 'data')])
def update_selection_table(selected_points, display_data, uploaded_data):
    if selected_points and selected_points['points']:
        display_df = pd.DataFrame.from_dict(display_data)
        selected_genes = [x['customdata'] for x in selected_points['points']]
        selected_df = display_df[display_df['gene_id'].isin(selected_genes)]
        return selected_df.to_dict('records')
    else:
        return pd.DataFrame('', index=[0], columns=uploaded_data[0].keys()).to_dict('records')


@app.callback(Output('display_table', 'data'),
              [Input('slider', 'value')],
              [State('store', 'data'),
               State('upload_table', 'data')])
def update_display_table(value, store_data, upload_data):
    # fetch full dataset
    uploaded_df = pd.DataFrame.from_dict(upload_data)
    # decide which part to display
    displayed_df = uploaded_df[uploaded_df['experiment'] == store_data['available_files'][value]]
    display_data = displayed_df.to_dict('records')

    return display_data


@app.callback(Output('slider_label', 'children'),
              [Input('slider', 'value')],
              [State('store', 'data')])
def update_slider_label(value, store_data):
    display_file = store_data['available_files'][value]
    return f'Comparison: {display_file}'


@app.callback(Output('selection_label', 'children'),
              [Input('active_plot', 'selectedData')],
              [State('display_table', 'data')])
def update_slider_label(selected_points, upload_data):
    total_points_count = len(upload_data)
    if selected_points and selected_points['points']:
        selected_points_count = len(selected_points['points'])
        selected_percentage = round(float(selected_points_count) / float(total_points_count) * 100, 1)
        return f'Selected points: {selected_points_count} / {total_points_count} ⩯ {selected_percentage}%'

    else:
        return f'Selected points: 0 / {total_points_count} ≙ 0%'


@app.callback(Output('store', 'data'),
              [Input('slider', 'value')],
              [State('store', 'data')])
def update_store(value, store_data):
    store_data.update({'current_file': store_data['available_files'][value]})
    return store_data


@app.callback(Output('active_plot', 'figure'),
              [Input('slider', 'value'),
               Input('plot_selector', 'value')],
              [State('store', 'data'),
               State('upload_table', 'data')])
def update_store(slider_value, plot_value, store_data, upload_data):
    # fetch full dataset
    uploaded_df = pd.DataFrame.from_dict(upload_data)
    # select values from current file
    file_df = uploaded_df[uploaded_df['experiment'] == store_data['available_files'][slider_value]]
    # build figure dictionary from values
    fig_dict = plot_functions[plot_value](file_df)
    return fig_dict


if __name__ == '__main__':
    app.run_server(debug=True)
