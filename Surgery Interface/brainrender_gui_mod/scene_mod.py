import vtk
from vedo.mesh import Mesh
from brainrender.Utils.data_io import load_mesh_from_file
from brainrender_gui.widgets.actors_list import update_actors_list

class MyInteractorStyle(vtk.vtkInteractorStyleTrackballCamera):
    def __init__(self, parent):
        self.parent = parent
        self.parent_interactor = parent.scene.plotter.interactor
        self.reference_point_button = parent.buttons["switch_reference_point"]
        self.pipette_dict = parent.pipette_dict
        self.AddObserver("KeyPressEvent", self.keyPressEvent)
        self.AddObserver("CharEvent", self.charEvent)

    def charEvent(self, obj, event):
        key = self.parent_interactor.GetKeyCode()
        if key in ('2', '8', '4', '6', '9', '3'):
            # override these keys, we are using them ourselves
            return
        else:
            super(MyInteractorStyle, self).OnChar()

    def keyPressEvent(self, obj, event):
        key = self.parent_interactor.GetKeySym()
        if key == '2':
            self.pipette_dict['pipette'].x(self.pipette_dict['pipette'].x() + 100)
        elif key == '8':
            self.pipette_dict['pipette'].x(self.pipette_dict['pipette'].x() - 100)
        elif key == '6':
            self.pipette_dict['pipette'].z(self.pipette_dict['pipette'].z() + 100)
        elif key == '4':
            self.pipette_dict['pipette'].z(self.pipette_dict['pipette'].z() - 100)
        elif key == '3':
            self.pipette_dict['pipette'].y(self.pipette_dict['pipette'].y() + 100)
        elif key == '9':
            self.pipette_dict['pipette'].y(self.pipette_dict['pipette'].y() - 100)

        reference_point = self.pipette_dict['pipette'].polydata().GetPoint(self.pipette_dict['pipette_tip_id'])
        self.pipette_dict['ink_blob_source'].SetCenter(reference_point)
        self.pipette_dict['ink_blob_source'].Update()

        point1 = [reference_point[0], reference_point[1] + self.pipette_dict['length_under_tip'],
                  reference_point[2]]
        point2 = [reference_point[0], reference_point[1] + self.pipette_dict['length_above_tip'],
                  reference_point[2]]
        self.pipette_dict['laser_source'].SetPoint1(point1)
        self.pipette_dict['laser_source'].SetPoint2(point2)
        self.pipette_dict['laser_source'].Update()

        if self.reference_point_button.text() == 'Pipette tip':
            self.parent.update_clippers(force_update=True)
            self.parent.scene.plotter.camera.SetFocalPoint(self.parent.get_reference_point())

        # Fake a button press to force canvas update
        self.parent_interactor.MiddleButtonPressEvent()
        self.parent_interactor.MiddleButtonReleaseEvent()

        self.parent.update_status_bar()

        return


class SceneMod:
    def __init__(self):
        """
            Collections of functions to control the
            addition of regions meshes to the brainrender
            Scene for the GUI
        """
        self.root = self.scene.root
        self.root_com = self.root.centerOfMass()

        initial_plane = vtk.vtkPlane()
        initial_plane.SetOrigin((0, 0, 100000))
        norm_vector = (0, 0, 1)
        initial_plane.SetNormal(norm_vector)

        self.clip_plane = initial_plane

        self.add_pipette()
        self.add_skull()

        self.previous_norm_vector = None
        self.current_reference_point = 'root'

        for actor in self.scene.actors:
            self.add_actors_with_clipping(actor)

        self.scene.plotter.camera.SetFocalPoint(self.root_com)

        self.update_clippers()

        self.auto_clip = self.auto_clip_checkbox.isChecked()

        return

    def add_to_store(self, *actors, fix=False):
        for actor in actors:
            actor_polydata = actor.polydata()

            if fix:
                polydata_cleaner = vtk.vtkCleanPolyData()
                polydata_cleaner.SetInputData(actor_polydata)
                polydata_cleaner.SetTolerance(0.003)
                polydata_cleaner.ConvertLinesToPointsOn()
                polydata_cleaner.ConvertPolysToLinesOn()
                polydata_cleaner.ConvertStripsToPolysOn()
                polydata_cleaner.PointMergingOn()
                polydata_cleaner.Update()
                actor_polydata = polydata_cleaner.GetOutput()

                polydata_holefiller = vtk.vtkFillHolesFilter()
                polydata_holefiller.SetInputData(actor_polydata)
                polydata_holefiller.SetHoleSize(500)
                polydata_holefiller.Update()
                actor_polydata = polydata_holefiller.GetOutput()

                '''
                polydata_decimator = vtk.vtkQuadricDecimation()
                polydata_decimator.SetInputData(actor_polydata)
                polydata_decimator.SetTargetReduction(0.05)
                polydata_decimator.Update()
                actor_polydata = polydata_decimator.GetOutput()

                polydata_fixer = vtk.vtkPolyDataNormals()
                polydata_fixer.SetInputData(polydata_holefiller.GetOutput())
                polydata_fixer.AutoOrientNormalsOn()
                polydata_fixer.Update()
                actor_polydata = polydata_fixer.GetOutput()
                '''

            appender = vtk.vtkAppendPolyData()
            appender.AddInputData(actor_polydata)
            appender.Update()

            inverter = vtk.vtkPolyDataNormals()
            inverter.SetInputData(actor_polydata)
            inverter.FlipNormalsOn()
            inverter.Update()
            inverted_actor = inverter.GetOutput()

            # clipping
            clipper = vtk.vtkClipPolyData()
            clipper.GenerateClipScalarsOn()
            clipper.SetInputData(appender.GetOutput())
            clipper.SetClipFunction(self.clip_plane)
            clipper.InsideOutOn()
            clipper.Update()

            # create clipped actor
            clipped_polydata = clipper.GetOutput()
            clipped_actor = Mesh(clipped_polydata, c=actor.c(), alpha=actor.alpha())
            clipped_actor.name = f'{actor.name}_clipped'

            # create cutter
            cutter = vtk.vtkCutter()
            cutter.SetInputData(appender.GetOutput())
            cutter.SetCutFunction(self.clip_plane)
            cutter.Update()

            # fill contour
            contour = vtk.vtkContourTriangulator()
            contour.SetInputConnection(cutter.GetOutputPort())

            contourMapper = vtk.vtkPolyDataMapper()
            contourMapper.SetInputConnection(contour.GetOutputPort())

            # create cap
            cap_polydata = contour.GetOutput()
            cap_actor = Mesh(None, c=actor.c(), alpha=actor.alpha())
            cap_actor.name = f'{actor.name}_cap'
            cap_property = cap_actor.GetProperty()
            cap_property.LightingOff()
            cap_property.SetAmbient(1)
            cap_property.SetDiffuse(0)
            cap_property.SetSpecular(0)
            cap_actor.SetMapper(contourMapper)

            vector_text = vtk.vtkVectorText()
            vector_text.SetText(actor.name)
            text_mapper = vtk.vtkPolyDataMapper()
            text_mapper.SetInputConnection(vector_text.GetOutputPort())
            text_actor = Mesh(None, c='black', alpha=1.)
            text_actor.SetMapper(text_mapper)
            text_actor.SetCamera(self.scene.plotter.camera)
            text_actor.SetScale(100, 100, 100)
            text_actor.VisibilityOff()
            self.scene.plotter.add(actors=[text_actor])

            self.store[actor.name] = {
                'OriginalMesh': actor,
                'InvertedMesh': inverted_actor,
                'PolyDataAppender': appender,
                'PlaneClipper': clipper,
                'PlaneCutter': cutter,
                'ClippedMesh': clipped_actor,
                'Cap': cap_actor,
                'Label': text_actor,
            }

            if fix:
                feature_edges = vtk.vtkFeatureEdges()
                feature_edges.SetInputData(clipper.GetOutput())
                feature_edges.NonManifoldEdgesOn()
                feature_edges.ManifoldEdgesOff()
                feature_edges.FeatureEdgesOff()
                feature_edges.BoundaryEdgesOff()
                feature_edges.ColoringOn()
                feature_edges.Update()
                edge_actor = Mesh(feature_edges.GetOutput(), c='yellow')
                edge_actor.lineWidth(5)
                edge_property = edge_actor.GetProperty()
                edge_property.LightingOff()
                edge_property.SetAmbient(1)
                edge_property.SetDiffuse(0)
                edge_property.SetSpecular(0)
                self.scene.plotter.add(actors=[edge_actor])
                self.store[actor.name]['Edges'] = feature_edges

    def add_actors_with_clipping(self, *actors, **kwargs):
        for actor in actors:
            self.add_to_store(actor, **kwargs)

            self.scene.plotter.remove(actor)
            clipped_actor = self.store[actor.name]['ClippedMesh']
            cap_actor = self.store[actor.name]['Cap']
            self.scene.plotter.add(actors=[clipped_actor, cap_actor])

    def CameraModifiedCallback(self, *args):
        if self.auto_clip:
            self.update_clippers()

    def update_clippers(self, force_update=False):
        camera_direction = self.scene.plotter.camera.GetDirectionOfProjection()
        dominant_direction = camera_direction.index(max(camera_direction, key=lambda y: abs(y)))
        norm_vector = [x and (1, -1)[x > 0] * (x == camera_direction[dominant_direction]) for x in
                       camera_direction]
        # dominant_direction = camera_direction.index(max(camera_direction, key=lambda y: abs(y)))
        if norm_vector != self.previous_norm_vector or force_update:
            clip_plane = vtk.vtkPlane()
            clip_plane.SetOrigin([x for x in self.get_reference_point()])
            clip_plane.SetNormal(norm_vector)

            for actor_name, actor_dict in self.store.items():
                actor_dict['PlaneClipper'].SetClipFunction(clip_plane)
                actor_dict['PlaneClipper'].Update()
                actor_dict['PlaneCutter'].SetCutFunction(clip_plane)
                actor_dict['PlaneCutter'].Update()

                if 'Edges' in actor_dict.keys():
                    actor_dict['Edges'].Update()

                self.update_label(actor_name, norm_vector)

            self.previous_norm_vector = norm_vector

    def update_label(self, actor_name, normal):
        actor_dict = self.store[actor_name]
        cutter_output = actor_dict['PlaneCutter'].GetOutput()
        if cutter_output.GetNumberOfPoints() > 0:
            new_position = []
            cutter_bounds = cutter_output.GetBounds()
            for i in range(3):
                if normal[i] != 0:
                    new_position.append(self.get_reference_point()[i] + normal[i] * 300)
                else:
                    new_position.append((cutter_bounds[i * 2] + cutter_bounds[i * 2 + 1]) / 2)
            actor_dict['Label'].SetPosition(new_position)
            actor_dict['Label'].SetOrientation(normal)
        else:
            actor_dict['Label'].alpha(0)

    def get_reference_point(self):
        if self.current_reference_point == 'root':
            return self.root_com
        else:
            return self.pipette_dict['pipette'].polydata().GetPoint(self.pipette_dict['pipette_tip_id'])

    def add_pipette(self):
        # Add the pipette
        #             'bregma': [4029.746337890625, 1057.472412109375, 5692.672912597656],
        pipette_dict = {
            'bregma': [5329.746337890625, 1057.472412109375, 5692.672912597656],
            'length_under_tip': 6500,
            'length_above_tip': -3500,
        }

        pipette_mesh = load_mesh_from_file("brainrender_gui_mod/assets/injector_pipette.stl")
        pipette_mesh.name = 'pipette'
        pipette_dict['pipette'] = self.scene.plotter.add(pipette_mesh)
        pipette_dict['pipette'].rotateX(90)
        pipette_dict['pipette'].c("aqua")
        pipette_dict['pipette'].scale([1000, 1000, 1000])
        pipette_dict['pipette'].SetPosition(pipette_dict['bregma'])

        # really, truly lazy coding, but screw it, it only runs once
        pipette_points = pipette_dict['pipette'].polydata()
        lowest_point_id = 0
        for point_id in range(0, pipette_points.GetNumberOfPoints()):
            this_point = pipette_points.GetPoint(point_id)
            lowest_point = pipette_points.GetPoint(lowest_point_id)
            if this_point[1] > lowest_point[1]:
                lowest_point_id = point_id
        pipette_dict['pipette_tip_id'] = lowest_point_id

        # Add the pipette laser
        pipette_dict['laser_source'] = vtk.vtkLineSource()
        reference_point = pipette_points.GetPoint(pipette_dict['pipette_tip_id'])
        point1 = [reference_point[0], reference_point[1] + pipette_dict['length_under_tip'], reference_point[2]]
        point2 = [reference_point[0], reference_point[1] + pipette_dict['length_above_tip'], reference_point[2]]
        pipette_dict['laser_source'].SetPoint1(point1)
        pipette_dict['laser_source'].SetPoint2(point2)
        pipette_dict['laser_source'].Update()

        laser_tube = vtk.vtkTubeFilter()
        laser_tube.SetInputConnection(pipette_dict['laser_source'].GetOutputPort())
        laser_tube.SetRadius(100)
        laser_tube.SetNumberOfSides(16)

        laser_mapper = vtk.vtkPolyDataMapper()
        laser_mapper.SetInputConnection(laser_tube.GetOutputPort())

        laser_actor = Mesh(None, c="GreenYellow", alpha=0.25)
        laser_actor.SetMapper(laser_mapper)

        pipette_dict['laser_actor'] = self.scene.plotter.add(laser_actor)

        pipette_dict['ink_blob_source'] = vtk.vtkSphereSource()
        pipette_dict['ink_blob_source'].SetThetaResolution(64)
        pipette_dict['ink_blob_source'].SetPhiResolution(64)
        pipette_dict['ink_blob_source'].SetCenter(reference_point)
        pipette_dict['ink_blob_source'].SetRadius(0)
        pipette_dict['ink_blob_source'].Update()

        ink_mapper = vtk.vtkPolyDataMapper()
        ink_mapper.SetInputConnection(pipette_dict['ink_blob_source'].GetOutputPort())

        ink_actor = Mesh(None, c="Navy", alpha=0.5)
        ink_actor.SetMapper(ink_mapper)

        pipette_dict['ink_blob_actor'] = self.scene.plotter.add(ink_actor)

        self.pipette_dict = pipette_dict

    def add_skull(self):
        # Add the skull
        skull = load_mesh_from_file("brainrender_gui_mod/assets/skull.stl")
        skull.name = 'skull'
        skull.c("ivory").alpha(1)
        # Align skull and brain (scene.root)
        skull_com = skull.centerOfMass()
        root_com = self.root.centerOfMass()

        skull.origin(skull_com)
        skull.rotateY(90).rotateX(180)
        skull.x(root_com[0] - skull_com[0])
        skull.y(root_com[1] - skull_com[1])
        skull.z(root_com[2] - skull_com[2])
        skull.x(3750)
        skull.rotateZ(-27)
        skull.y(7700)
        skull.scale([1300, 1550, 1100])

        self.add_actors_with_clipping(skull)

        self.actors[skull.name] = self.atuple(
            skull, True, skull.color(), skull.alpha()
        )

        update_actors_list(self.actors_list, self.actors)
        self._update()
