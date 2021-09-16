from qtpy.QtWidgets import QFileDialog
from offline_pyrat_parser.apputils.report_functions import *
from offline_pyrat_parser.apputils.utility_objects import Worker
from common_parser_elements.utility_objects import *
from pkg_resources import resource_filename
import xlsxwriter
import sys
import os
import traceback


class ReportCreator:
    def __init__(self):
        return

    def create_report(self):
        output_path = resource_filename("offline_pyrat_parser", "output")
        file_name = QFileDialog.getOpenFileName(self, 'Select Parsed Dataset', output_path, "Parsed dataset (*.pds)")
        if file_name[0]:
            self.disable_buttons()
            worker = Worker(self.print_report, file_name[0])
            self.thread_pool.start(worker)

    def pretty_printing_to_xlsx(self, workbook, local_format_dict, input_df_list, residues_markers_df_list, sheet_name_list,
                                autofit=True, legend=True):
        total_length = sum([len(x) for x in input_df_list])
        total_index = 0

        for sheet_index in range(len(input_df_list)):
            input_df = input_df_list[sheet_index]
            residues_markers_df = residues_markers_df_list[sheet_index]
            sheet_name = sheet_name_list[sheet_index]

            worksheet = workbook.add_worksheet(sheet_name)

            table_row = 0
            table_column = 0

            if legend:
                worksheet.write(table_row, table_column, 'Legend:', local_format_dict['cell_text']['default'])
                table_column += 1
                for key in local_format_dict['cell_text']:
                    if key == 'default':
                        worksheet.write(table_row, table_column, key, local_format_dict['cell_text'][key])
                    else:
                        worksheet.write(table_row, table_column, key.name, local_format_dict['cell_text'][key])
                    table_column += 1
                table_row += 1
                table_column = 0

            for index_name in input_df.index.names:
                worksheet.write(table_row, table_column, index_name, local_format_dict['index']['default'])
                table_column += 1

            for header in input_df.columns:
                worksheet.write(table_row, table_column, header, local_format_dict['index']['default'])
                table_column += 1

            table_row += 1

            for user, section in input_df.groupby(level=0):
                if len(section.index) > 1:
                    worksheet.merge_range('A%s:A%s' % (table_row + 1, table_row + len(section.index)), user,
                                          local_format_dict['index']['default'])
                else:
                    worksheet.write(table_row, 0, user, local_format_dict['index']['default'])
                for ID, row in section.loc[user].iterrows():
                    _progress = int((total_index + 1) / total_length * 50)
                    '''
                    print('\r' + str(_progress * '█' + (50 - _progress) * ' ' + '%i/%i' % (total_index + 1, total_length)),
                          end='')
                    '''
                    self.status_signal.emit('Creating report %s' % ID)
                    self.progress_signal.emit(int((total_index + 1.) / total_length * 100))
                    global_format = 'default'
                    if 'global' in residues_markers_df.at[(user, ID), 'markers'].keys() and \
                            residues_markers_df.at[(user, ID), 'markers']['global']:
                        global_format = residues_markers_df.at[(user, ID), 'markers']['global']
                    table_column = 1
                    worksheet.write(table_row, table_column, ID, local_format_dict['index'][global_format])
                    table_column += 1
                    for column, value in row.iteritems():
                        if pd.notnull(value):
                            cell_format = 'default'
                            if column in residues_markers_df.at[(user, ID), 'markers']:
                                cell_format = residues_markers_df.at[(user, ID), 'markers'][column]
                            if input_df[column].dtype == 'datetime64[ns]':
                                worksheet.write_datetime(table_row, table_column, value,
                                                         local_format_dict['cell_date'][cell_format])
                            else:
                                if type(value) == dict:
                                    worksheet.write(table_row, table_column, str(value),
                                                    local_format_dict['cell_text'][cell_format])
                                else:
                                    worksheet.write(table_row, table_column, value,
                                                    local_format_dict['cell_text'][cell_format])
                        else:
                            cell_format = 'default'
                            if column in residues_markers_df.at[(user, ID), 'markers']:
                                cell_format = residues_markers_df.at[(user, ID), 'markers'][column]
                            worksheet.write(table_row, table_column, '', local_format_dict['cell_text'][cell_format])
                        table_column += 1
                    table_row += 1
                    total_index += 1

            if autofit:
                for i, width in enumerate(get_col_widths(input_df)):
                    worksheet.set_column(i, i, max(9, min(width, 30)))

    def print_report(self, dataset_location):
        current_id = ''

        try:
            self.output_signal.emit('Processing...')
            overview_order = ['exp', 'start', 'end', 'death', 'suffering', 'original', 'standardized',
                              'logs', 'errors']
            procedure_order = ['User', 'Name', 'Date', 'Logs', 'Errors']
            medication_order = ['User', 'Name', 'Date', 'Amount', 'Concentration', 'Logs', 'Errors']

            parsed_dict = pd.read_pickle(dataset_location)
            df_overview = parsed_dict['overview']

            convert_dict = {
                'start': 'datetime64[ns]',
                'end': 'datetime64[ns]'
            }  # using dictionary to convert specific columns
            df_overview = df_overview.astype(convert_dict)

            df_overview['ID'] = df_overview.index

            df_markers = df_overview['markers'].apply(pd.Series)
            df_overview['_empty'] = df_markers['global'] == Markers.Missing
            df_overview['_unused'] = df_markers['global'] == Markers.Unused
            if 'user' in df_markers.columns:
                df_overview['_unknown'] = df_markers['user'].isin([Markers.Unknown, Markers.Estimate])
            else:
                df_overview['_unknown'] = False
            df_overview_reformed = df_overview.set_index(keys=['user', 'ID']).sort_index()
            df_markers_reformed = df_overview_reformed['markers'].apply(pd.Series)
            df_markers_reformed['user'] = df_markers_reformed.index.get_level_values(0)

            print_df_list = []
            marker_df_list = []
            sheet_names = []

            df_empty = df_overview_reformed[df_overview_reformed['_empty']]
            df_remaining = df_overview_reformed[~df_overview_reformed['_empty']]
            df_unused = df_remaining[df_remaining['_unused']]
            df_remaining = df_remaining[~df_remaining['_unused']]
            df_unknown = df_remaining[df_remaining['_unknown']]
            df_known = df_remaining[~df_remaining['_unknown']]

            print_df_list.append(df_known[overview_order])
            marker_df_list.append(df_overview_reformed[['markers', 'residues']])
            sheet_names.append('Known Users')

            print_df_list.append(df_unknown[overview_order])
            marker_df_list.append(df_overview_reformed[['markers', 'residues']])
            sheet_names.append('Unknown Users')

            print_df_list.append(df_unused[overview_order])
            marker_df_list.append(df_overview_reformed[['markers', 'residues']])
            sheet_names.append('Unused Mice')

            empty_order = [x for x in df_empty.columns if x[0].isupper()]
            print_df_list.append(df_empty[empty_order])
            marker_df_list.append(df_overview_reformed[['markers', 'residues']])
            sheet_names.append('Empty Comments')

            df_procedures = parsed_dict['procedures']

            convert_dict = {
                'Date': 'datetime64[ns]'
            }  # using dictionary to convert specific columns
            df_procedures = df_procedures.astype(convert_dict)

            print_df_list.append(df_procedures[procedure_order])
            marker_df_list.append(df_procedures[['markers', 'residues']])
            sheet_names.append('All Procedures')

            df_medication = parsed_dict['medication']

            convert_dict = {
                'Date': 'datetime64[ns]'
            }  # using dictionary to convert specific columns
            df_medication = df_medication.astype(convert_dict)

            print_df_list.append(df_medication[medication_order])
            marker_df_list.append(df_medication[['markers', 'residues']])
            sheet_names.append('All Medications')

            output_path = resource_filename("offline_pyrat_parser", f"output")
            if not os.path.exists(output_path):
                os.makedirs(output_path)

            original_filename = dataset_location.split('/')[-1].split('_', maxsplit=2)[-1].split('.')[0]
            # Create a workbook and iterate through worksheets.
            workbook = xlsxwriter.Workbook(output_path + rf'\report_{original_filename}.xlsx')

            local_format_dict = {}

            for cell_type in XLSX_FORMAT_DICT:
                local_format_dict[cell_type] = {}
                for marker in XLSX_FORMAT_DICT[cell_type]:
                    local_format_dict[cell_type][marker] = workbook.add_format(XLSX_FORMAT_DICT[cell_type][marker])

            generate_marker_statistics(workbook, local_format_dict, df_markers_reformed)

            generate_overview_statistics(workbook, local_format_dict, df_markers_reformed, df_overview, df_procedures,
                                         df_medication)

            self.pretty_printing_to_xlsx(workbook, local_format_dict, input_df_list=print_df_list,
                                         residues_markers_df_list=marker_df_list, sheet_name_list=sheet_names)

            workbook.close()

            self.output_signal.emit('Report finished!')
            self.status_signal.emit('Ready')

        except Exception:
            exc_type, exc_value, tb = sys.exc_info()
            exception_string = ''
            if tb is not None:
                prev = tb
                curr = tb.tb_next
                while curr is not None:
                    prev = curr
                    curr = curr.tb_next
                variables = prev.tb_frame.f_locals
                filtered_variables = {x: variables[x] for x in variables if x not in ('self', 'kwargs')}
                filtered_string = ''.join((f'{key}: {value}\n' for key, value in filtered_variables.items()))
                exception_string += f'An error occurred while working with these variables:\n{filtered_string}\n'
            exception_string += traceback.format_exc()
            self.output_signal.emit(exception_string)
            self.status_signal.emit(f'An error occurred while writing {current_id}')

        finally:
            self.finished_signal.emit()
