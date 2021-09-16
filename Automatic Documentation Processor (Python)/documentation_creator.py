from qtpy.QtWidgets import QFileDialog
from offline_pyrat_parser.apputils.documentation_functions import *
from offline_pyrat_parser.apputils.utility_objects import Worker, defined_values, lab_info
from common_parser_elements.utility_objects import *
from pkg_resources import resource_filename
import json
import os
import traceback
import sys
import comtypes.client


class DocumentationCreator:
    def __init__(self):
        return

    def create_documentation(self):
        output_path = resource_filename("offline_pyrat_parser", "output")
        file_name = QFileDialog.getOpenFileName(self, 'Select Parsed Dataset', output_path, "Parsed dataset (*.pds)")
        if file_name[0]:
            self.disable_buttons()
            worker = Worker(self.print_documentation, file_name[0])
            self.thread_pool.start(worker)

    def translate_folder_to_pdf(self):
        output_path = resource_filename("offline_pyrat_parser", "output")
        dir_name = QFileDialog.getExistingDirectory(self, 'Select directory', output_path)
        if dir_name:
            self.disable_buttons()
            worker = Worker(self.translate_folder_to_pdf_func, dir_name)
            self.thread_pool.start(worker)

    def create_drug_sheets(self):
        output_path = resource_filename("offline_pyrat_parser", "output")
        file_name = QFileDialog.getOpenFileName(self, 'Select Parsed Dataset', output_path, "Parsed dataset (*.pds)")
        if file_name[0]:
            self.disable_buttons()
            worker = Worker(self.print_drug_sheets, file_name[0])
            self.thread_pool.start(worker)

    def translate_folder_to_pdf_func(self, dir_to_translate):
        current_file = ''

        try:
            self.output_signal.emit('Processing...')

            wdFormatPDF = 17
            word = comtypes.client.CreateObject('Word.Application')

            valid_files = [f for f in os.listdir(dir_to_translate) if f.endswith(".docx")]

            total_length = len(valid_files)

            for index, file in enumerate(valid_files):
                self.progress_signal.emit(int((index + 1.) / total_length * 100))
                self.status_signal.emit('Converting %s' % file)

                current_file = file

                in_file = os.path.normpath(rf"{dir_to_translate}/{file}")
                out_file = os.path.normpath(rf"{dir_to_translate}/{file.split('.')[0]}.pdf")

                doc = word.Documents.Open(in_file)
                doc.SaveAs(out_file, FileFormat=wdFormatPDF)
                doc.Close()

            word.Quit()
            self.output_signal.emit(f'Conversion finished!')
            self.status_signal.emit('Ready')

        except Exception as e:
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
            self.status_signal.emit(f'An error occurred while converting {current_file}')

        finally:
            self.finished_signal.emit()

    def print_documentation(self, dataset_location):
        current_id = ''

        try:
            self.output_signal.emit('Processing...')
            parsed_dict = pd.read_pickle(dataset_location)

            # Read data from pickled dataframe
            df_master_overview = parsed_dict['overview']  # grab the pickled master-table
            df_markers = df_master_overview['markers'].apply(pd.Series)
            df_master_overview['used'] = df_markers['global'] != Markers.Unused
            df_master_overview['ID'] = df_master_overview.index

            df_procedure = parsed_dict['procedures']

            df_medication = parsed_dict['medication']

            required_columns = ['start', 'end', 'exp', 'death']

            marker_condition = [not any(marker in [Markers.Missing, Markers.Estimate] for marker in x.values()) for x in
                                df_master_overview['markers']]

            df_relevant = df_master_overview[(df_master_overview['used']) & (
                df_master_overview['user'].isin(defined_values['user'].index)) & marker_condition].dropna(
                subset=required_columns)

            df_unused = df_master_overview[~df_master_overview['used']]
            df_unknown_user = df_master_overview[~df_master_overview['user'].isin(defined_values['user'].index)]
            df_invalid_values = df_master_overview[[not boolean for boolean in marker_condition]]

            # select only those mice which have a end-date and were actually used

            template = 'Standard'

            template_path = resource_filename("offline_pyrat_parser", "Templates")

            with open(template_path + f'\\{template}\\{template}.template') as file:
                template_definition = json.load(file)

            total_length = len(df_relevant)
            index = 0

            output_path = resource_filename("offline_pyrat_parser", "output")
            if not os.path.exists(output_path):
                os.makedirs(output_path)

            for user, user_df in df_relevant.groupby('user'):
                if not os.path.exists(output_path + rf'\{user}'):
                    os.makedirs(output_path + rf'\{user}')
                for i, ID in enumerate(user_df.index):
                    _progress = int((i + 1) / total_length * 50)
                    # print('\r' + str(_progress * '█' + (50 - _progress) * ' ' + '%i/%i' % (i + 1, total_length)), end='')
                    self.progress_signal.emit(int((index + 1.) / total_length * 100))

                    current_id = ID

                    content_dict = dict()
                    content_dict['ID'] = ID
                    content_dict['DOB'] = user_df.at[ID, 'DOB']

                    reduced_strain = []

                    for segment in user_df.at[ID, 'Line / Strain (Name)'].split(' '):
                        if segment \
                                .replace("-", "") \
                                .replace("/", "") \
                                .replace("\\", "") \
                                .replace("(", "") \
                                .replace(")", "") \
                                .replace(".", "") \
                                .replace(":", "") \
                                .isalnum() and "æ" not in segment:
                            reduced_strain.append(segment)
                    reduced_strain = ' '.join(reduced_strain)

                    content_dict['reduced_strain'] = reduced_strain
                    content_dict['starting_weight_gram'] = '%sg' % round(user_df.at[ID, 'real_weight'][0], 1)
                    content_dict['starting_weight'] = round(user_df.at[ID, 'real_weight'][0], 1)
                    content_dict['Sex'] = user_df.at[ID, 'Sex']
                    content_dict['Supplier'] = 'FEM'
                    content_dict['exp'] = user_df.at[ID, 'exp']
                    content_dict['start'] = user_df.at[ID, 'start']
                    content_dict['end'] = user_df.at[ID, 'end']
                    content_dict['death'] = user_df.at[ID, 'death']
                    content_dict['list_water'] = user_df.at[ID, 'water_control_mask']
                    content_dict["list_weight"] = user_df.at[ID, 'real_weight']

                    content_dict["unexpected"] = ""
                    if "unexpected" in user_df.columns and not pd.isna(user_df.at[ID, "unexpected"]):
                        content_dict["unexpected"] += f"{user_df.at[ID, 'unexpected']}"

                    output_string = json.dumps(content_dict,
                                               default=lambda x: x.strftime("%d.%m.%Y") if isinstance(x, datetime.datetime)
                                               else x,)[1:-1].replace(',', '\n')

                    if ID in df_procedure.index:
                        content_dict['procedure'] = df_procedure.loc[ID, :].copy()
                        content_dict['procedure'].loc[:, 'Date'] = content_dict['procedure']['Date'].dt.strftime('%d.%m.%Y')
                        output_string += '\n%s' % (df_procedure.loc[ID, :].to_string())

                    if ID in df_medication.index:
                        content_dict['medication'] = df_medication.loc[ID, :].copy()
                        content_dict['medication'].loc[:, 'Date'] = content_dict['medication']['Date'].dt.strftime('%d.%m.%Y')
                        output_string += '\n%s' % (df_medication.loc[ID, :].to_string())

                    content_dict['user_info'] = defined_values['user'].loc[user_df.at[ID, 'user']].to_dict()
                    content_dict['user_info'].update({'Username': user_df.at[ID, 'user']})

                    if "surgeon" not in user_df.columns or pd.isna(user_df.at[ID, "surgeon"]):
                        content_dict['surgeon_info'] = content_dict['user_info']

                    else:
                        content_dict['surgeon_info'] = defined_values['user'].loc[user_df.at[ID, 'surgeon']].to_dict()
                        content_dict['surgeon_info'].update({'Username': user_df.at[ID, 'surgeon']})

                    self.output_signal.emit(output_string)
                    self.status_signal.emit('Creating surgerysheet %s' % ID)

                    surgery_doc = generate_surgery_sheet(content_dict, template_definition)

                    header_dict = {'PI': defined_values['user'].at[lab_info['PI'], 'Name'],
                                   'Responsible': content_dict['user_info']['Name'],
                                   'responsible_email': content_dict['user_info']['Email']}
                    surgery_doc = generate_header(surgery_doc, header_dict=header_dict)

                    footer_paragraph = surgery_doc.sections[0].footer.paragraphs[0]
                    generate_footer(footer_paragraph)

                    surgery_doc = set_style(surgery_doc, template_definition)

                    surgery_doc.save(output_path + rf'\{user}\{ID}_surgery.docx')

                    score_doc = generate_score_sheet(content_dict, template_definition)
                    if score_doc:
                        self.status_signal.emit('Creating scoresheet %s' % ID)
                        score_doc.save(output_path + rf'\{user}\{ID}_score.docx')

                    index += 1

            skipped_string = ''
            if any((not df_unused.empty, not df_unknown_user.empty, not df_invalid_values.empty)):
                skipped_string += '\nBut some mice were skipped:\n'
            if not df_unused.empty:
                list_string = ', '.join(list(df_unknown_user.index))
                skipped_string += f'- Skipped due to being unused:\n{list_string}\n'
            if not df_unknown_user.empty:
                list_string = ', '.join(list(df_unknown_user.index))
                skipped_string += f'- Skipped due to the user being unknown:\n{list_string}\n'
            if not df_invalid_values.empty:
                list_string = ', '.join(list(df_invalid_values.index))
                skipped_string += f'- Skipped due to some values being missing or estimated:\n{list_string}\n'
            self.output_signal.emit(f'Documentation finished!{skipped_string}')
            self.status_signal.emit('Ready')

        except Exception as e:
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
            self.status_signal.emit(f'An error occurred while printing {current_id}')

        finally:
            self.finished_signal.emit()

    def print_drug_sheets(self, dataset_location):
        current_drug = ''

        try:
            self.output_signal.emit('Processing...')
            parsed_dict = pd.read_pickle(dataset_location)

            # Read data from pickled dataframe
            df_medication = parsed_dict['medication']  # grab the pickled medication-table

            required_columns = ['User', 'Project', 'Date', 'Name', "Amount"]

            marker_condition = [
                not any(marker == Markers.Missing for marker in {k: x[k] for k in required_columns if k in x}.values())
                for x in df_medication["markers"]]

            df_relevant = df_medication[marker_condition].dropna(subset=required_columns).reset_index(drop=False)

            df_unknown_user = df_medication[~df_medication['User'].isin(defined_values['user'].index)]
            df_invalid_values = df_medication[[not boolean for boolean in marker_condition]]

            template = 'Standard'
            template_path = resource_filename("offline_pyrat_parser", "Templates")

            with open(template_path + f'\\{template}\\{template}.template') as file:
                template_definition = json.load(file)

            total_length = len(df_relevant["Name"].unique())
            index = 0

            output_path = resource_filename("offline_pyrat_parser", "output")
            if not os.path.exists(output_path):
                os.makedirs(output_path)

            for drug, drug_df in df_relevant.groupby("Name"):
                self.progress_signal.emit(int((index + 1.) / total_length * 100))

                current_drug = drug

                content_dict = {
                    "drug_df": drug_df,
                    "info_dict": {
                        "drug_name": drug,
                        "charge_amount": '',
                        "charge_date": '',
                        "project": "---",
                        "recipient": "Secretary",
                        "responsible": "Professor",
                        "responsible_user": "Professor_User",
                    }
                }
                self.status_signal.emit('Creating drug sheet for %s' % drug)
                drug_doc = generate_drug_sheet(content_dict, template_definition)
                if drug_doc:
                    drug_doc.save(output_path + rf"\{drug.replace(' / ', '-')}_drug.docx")

                index += 1

            skipped_string = ''
            if not df_unknown_user.empty:
                list_string = ', '.join([rf"{x[0]}/{x[1]}" for x in list(df_unknown_user.index)])
                skipped_string += f'\nSome doses were processed despite the user being unknown:\n{list_string}\n'
            if not df_invalid_values.empty:
                list_string = ', '.join([rf"{x[0]}/{x[1]}" for x in list(df_invalid_values.index)])
                skipped_string += f'\nSome doses were skipped due to missing values:\n{list_string}\n'
            self.output_signal.emit(f'Drug sheets finished!{skipped_string}')
            self.status_signal.emit('Ready')

        except Exception as e:
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
            self.status_signal.emit(f'An error occurred while printing {current_drug}')

        finally:
            self.finished_signal.emit()
