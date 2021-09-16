import pandas as pd
import numpy as np
import cv2
import re
from utility_tools import read_json
import scipy.stats
from matplotlib.path import Path
from scipy.interpolate import interp1d
from scipy import optimize
from utility_tools import valid_kwargs
pd.options.mode.chained_assignment = None


def point_transform_2d(point, transform_matrix):
    """
    A helper function that performs the affine transformation of a point by a transformation matrix

    :param numpy.ndarray point: The 2D point to be transformed
    :param numpy.ndarray transform_matrix: The affine transformation matrix to be applied to the point.
    :return: point transformed by transform_matrix
    :rtype: numpy.ndarray
    """
    transformed_point = np.dot(transform_matrix, np.append(point, 1).T)
    return transformed_point[0:2] / transformed_point[2]


def warp_pandas_coordinates(point_pd, transform_matrix):
    """
    A helper function that calls point_transform_2d on either a pandas Series or DataFrame of 2D points to transform
    them with an affine transformation matrix.

    :param pandas.DataFrame or pandas.Series point_pd: Pandas Series or DataFrame containing 2D points
    :param transform_matrix: The affine transformation matrix to be applied to the points
    :return: Series or DataFrame with transformed points
    :rtype: pandas.DataFrame or pandas.Series
    """
    if type(point_pd) == pd.DataFrame:
        apply_func = pd.DataFrame.applymap
    elif type(point_pd) == pd.Series:
        apply_func = pd.Series.apply
    return apply_func(point_pd, lambda x: point_transform_2d(x, transform_matrix))


def deeplabcut_df_to_marker_df(deeplabcut_df):
    """
    Transform a pandas DataFrame parsed from the .csv-output of DeepLabCut to a table of points.
    The input-table contains a multi-index at the top, with the sub-columns (x, y, likelihood) for each marker.
    The output-table will contain only a simple index, with each marker as a single column and all values below as
    tuples (x,y). Likelihood information will be dropped.

    :param pandas.DataFrame deeplabcut_df: Raw DataFrame parsed from a .csv-file written by DeepLabCut
    :return: DataFrame containing all markers as simple 2D points.
    :rtype: pandas.DataFrame
    """
    marker_dict = {}
    for col in deeplabcut_df.columns.levels[0]:
        sub_cols = deeplabcut_df[col]
        marker_dict[col] = list(zip(sub_cols.x, sub_cols.y))
    marker_df = pd.DataFrame.from_dict(marker_dict).applymap(np.array)
    marker_df.index = deeplabcut_df.index
    return marker_df


def get_deeplabcut_likelihood_filter(deeplabcut_df, threshold=0.95):
    """
    Get a boolean mask indicating whether each marker in a DeepLabCut-DataFrame has a likelihood-entry above the
    threshold.

    :param pandas.DataFrame deeplabcut_df: Raw DeepLabCut DataFrame with multi-index and sub-columns (x, y, likelihood).
    :param float threshold: The minimum likelihood threshold
    :return: Boolean mask, with True where likelihood > threshold
    :rtype: pandas.DataFrame
    """
    filter_df = {}
    for marker, df_marker in deeplabcut_df.groupby(level=[0], axis=1):
        filter_df[marker] = list(
            (df_marker.xs("likelihood", level=1, drop_level=False, axis=1) > threshold)[marker]["likelihood"])
    return pd.DataFrame.from_dict(filter_df)


def filter_deeplabcut_df_by_likelihood(deeplabcut_df, likelihood_threshold=0.95):
    """
    Helper function that applies the boolean mask from deeplabcut_marker_above_likelihood to filter the entries in a
    DeepLabCut-DataFrame.

    :param deeplabcut_df: Raw DeepLabCut-DataFrame with multi-index and sub-columns (x, y, likelihood).
    :param likelihood_threshold: The minimum likelihood threshold
    :return: DeepLabCut-DataFrame with all values below threshold replaced by NaN
    :rtype: pandas.DataFrame
    """
    filter_df = get_deeplabcut_likelihood_filter(deeplabcut_df, likelihood_threshold)
    return deeplabcut_df.where(filter_df.loc[:, deeplabcut_df.columns.droplevel(1)].values, np.nan)


# https://www.statology.org/modified-z-score/
# https://hwbdocuments.env.nm.gov/Los%20Alamos%20National%20Labs/TA%2054/11587.pdf
def get_modified_zscore_filter(input_series, threshold=3.5):
    """
    Get a boolean mask indicating whether each position in a pandas Series has a modified zscore above the
    threshold. The modified zscore is calculated according to
    https://hwbdocuments.env.nm.gov/Los%20Alamos%20National%20Labs/TA%2054/11587.pdf.

    :param pandas.Series input_series: Pandas Series of numeric values.
    :param threshold: The minimum zscore threshold
    :return: Boolean mask, with True where zscore > threshold
    :rtype: pandas.Series
    """
    series_median = input_series.median(skipna=True)
    series_absolute_deviation = (input_series - series_median).abs()
    series_mad = series_absolute_deviation.median(skipna=True)  # mad = Median Absolute Deviation
    series_modified_zscore = 0.6745 * (input_series - series_median) / series_mad
    return series_modified_zscore.abs().ge(threshold)


def filter_series_by_modified_zscore(input_series, z_threshold=3.5):
    """
    Helper function that applies the boolean mask from get_modified_zscore_filter to filter a pandas Series.

    :param input_series: Pandas Series to be filtered
    :param z_threshold: Z-score above which the filter is to be applied
    :return: Series filtered by the modified zscore < z_threshold
    :rtype: pandas.Series
    """
    filtered_series = input_series.where(~get_modified_zscore_filter(input_series, z_threshold))
    return filtered_series


def smooth_series_by_rolling_mean(input_series, smoothing_window=31, min_periods=1):
    """
    Smooth a pandas Series over a rolling window.

    :param pandas.Series input_series: Raw series to be smoothed
    :param int smoothing_window: Size of the window over which the input series is to be smoothed
    :param int min_periods: Minimum number of values required in a window to calculate the local mean
    :return: Smoothed series
    :rtype: pandas.Series
    """
    return input_series.rolling(smoothing_window, min_periods=min_periods).mean(skipna=True).shift(
        -np.ceil(smoothing_window / 2).astype(int))


def get_rolling_zscore_filter(input_series, threshold=3, window_size=121, min_periods=1, nan_to_true=False):
    """
    Get a boolean mask indicating whether each position in a pandas Series has a rolling zscore above the threshold.

    :param pandas.Series input_series: Pandas Series of numeric values
    :param float threshold: The minimum zscore threshold
    :param int window_size: Size of the rolling window used to calculate the mean and standard deviation
    :param int min_periods: Minimum number of values required in a window to calculate the local zscore
    :param bool nan_to_true: Whether to translate any undefined z-scores to a True entry in the filter
    :return: Boolean mask, with True where local zscore > threshold
    :rtype: pandas.Series
    """
    # shift suggested by https://stackoverflow.com/a/47165379
    series_mean = input_series.rolling(window_size, min_periods=min_periods).mean(skipna=True).shift(1)
    series_std = input_series.rolling(window_size, min_periods=min_periods).std(skipna=True).shift(1)
    series_score = (input_series - series_mean) / series_std
    series_filter = series_score.abs().ge(threshold)
    if nan_to_true:
        series_filter[series_score.isnull().any(axis=1)] = True
    return series_filter


def filter_series_by_rolling_zscore(input_series, z_threshold=3, window_size=121, min_periods=1):
    """
    Helper function that applies the boolean mask from get_rolling_zscore_filter to filter a pandas Series.

    :param pandas.Series input_series: Pandas Series to be filtered
    :param float z_threshold: Z-score above which the filter is to be applied
    :param int window_size: Size of the rolling window used to calculate the mean and standard deviation
    :param int min_periods: Minimum number of values required in a window to calculate the local zscore
    :return: Series filtered by the rolling zscore < z_threshold
    :rtype: pandas.Series
    """
    filtered_series = input_series.where(
        ~get_rolling_zscore_filter(input_series, z_threshold, window_size, min_periods))
    return filtered_series


def filter_deeplabcut_df_by_rolling_zscore(deeplabcut_df, z_threshold=3, window_size=121, min_periods=1):
    """
    Helper function that applies the boolean mask from get_rolling_zscore_filter to filter a DeepLabCut-DataFrame.

    :param pandas.DataFrame deeplabcut_df: DeepLabCut-DataFrame to be filtered
    :param float z_threshold: Z-score above which the filter is to be applied
    :param int window_size: Size of the rolling window used to calculate the mean and standard deviation
    :param int min_periods: Minimum number of values required in a window to calculate the local zscore
    :return: DataFrame filtered per marker by the rolling zscore < z_threshold
    :rtype: pandas.Series
    """
    markers = deeplabcut_df.columns.get_level_values(level=0).unique()
    filter_df = pd.DataFrame(index=deeplabcut_df.index, columns=deeplabcut_df.columns.get_level_values(0).unique())
    for marker in markers:
        filter_df[marker] = get_rolling_zscore_filter(deeplabcut_df[marker][["x", "y"]], z_threshold, window_size,
                                                      min_periods, nan_to_true=True).any(axis=1)
    return deeplabcut_df.where(~filter_df.loc[:, deeplabcut_df.columns.droplevel(1)].values, np.nan)


def filter_pupil_df_by_modified_zscore(pupil_df, z_threshold=3.5, filtered_features=["x", "y"], **kwargs):
    markers = [col for col in pupil_df.columns.get_level_values(level=0).unique() if col.startswith("Pupil")]
    filter_df = None
    for feature in filtered_features:
        feature_filter = pupil_df.xs(((marker, feature) for marker in markers), axis=1).apply(
            lambda x: get_modified_zscore_filter(x, z_threshold=z_threshold), axis=1)
        if filter_df is None:
            filter_df = pd.DataFrame(index=pupil_df.index, columns=markers, data=feature_filter.values)
        else:
            filter_df = filter_df | feature_filter.values
    for skipped_marker in set(pupil_df.columns.get_level_values(0).unique()).difference(set(filter_df.columns)):
        filter_df[skipped_marker] = False
    return pupil_df.where(~filter_df.loc[:, pupil_df.columns.droplevel(1)].values, np.nan)


def filter_deeplabcut_df_by_limit_polygon(deeplabcut_df, polygon_points, marker_subset=None):
    filter_df = pd.DataFrame(index=deeplabcut_df.index, columns=deeplabcut_df.columns.get_level_values(0).unique())
    if marker_subset is None:
        markers = deeplabcut_df.columns.get_level_values(level=0).unique()
    else:
        markers = marker_subset
        columns_not_in_markers = [col for col in deeplabcut_df.columns.get_level_values(level=0).unique()]
        filter_df.loc[:, columns_not_in_markers] = True
        filter_df[columns_not_in_markers] = filter_df[columns_not_in_markers].astype('bool')
    for marker in markers:
        x, y = deeplabcut_df[(marker, "x")], deeplabcut_df[(marker, "y")]
        filter_series = pd.Series(list(zip(x, y)))
        path = Path(polygon_points)
        filter_df[marker] = path.contains_points(np.vstack(filter_series)).astype(bool)
    return deeplabcut_df.where(filter_df.loc[:, deeplabcut_df.columns.droplevel(1)].values, np.nan)


# https://stackoverflow.com/a/52020098
def interpolate_points_2d(points, interpolated_count, interpolate_range=[0, 1], method='slinear', extrapolate=True):
    distance = np.cumsum(np.sqrt(np.sum(np.diff(points, axis=0) ** 2, axis=1)))
    distance = np.insert(distance, 0, 0) / distance[-1]

    alpha = np.linspace(interpolate_range[0], interpolate_range[1], interpolated_count)

    interpolator = interp1d(distance, points, kind=method, axis=0, fill_value="extrapolate" if extrapolate else np.nan)
    return interpolator(alpha)


def interpolate_nan_markers_deeplabcut_df(deeplabcut_df, append_likelihood=True, extrapolate=True,
                                          limit_direction='both', **kwargs):
    markers = deeplabcut_df.columns.get_level_values(level=0).unique()
    interpolated_data = []
    for marker in markers:
        points = deeplabcut_df[marker][["x", "y"]]
        interpolated_points = points.interpolate(extrapolate=extrapolate, limit_direction=limit_direction, **kwargs)
        if append_likelihood:
            interpolated_points = np.column_stack([interpolated_points, deeplabcut_df[marker]["likelihood"]])
        interpolated_data.append(interpolated_points)
    interpolated_data = np.hstack(interpolated_data)
    return pd.DataFrame(data=interpolated_data, index=deeplabcut_df.index,
                        columns=deeplabcut_df.columns if append_likelihood else deeplabcut_df.drop("likelihood", axis=1,
                                                                                                   level=1))


def interpolate_nan_markers_loom_df(deeplabcut_df, extrapolate=True, limit_direction='both', **kwargs):
    markers = deeplabcut_df.columns.get_level_values(level=0).unique()
    interpolated_data = []
    for marker in markers:
        points = deeplabcut_df[marker][["x", "y"]]
        interpolated_points = points.interpolate(extrapolate=extrapolate, limit_direction=limit_direction, **kwargs)
        interpolated_data.append(interpolated_points)
    interpolated_data = np.hstack(interpolated_data)
    return pd.DataFrame(data=interpolated_data, index=deeplabcut_df.index,
                        columns=deeplabcut_df.columns)


def smooth_deeplabcut_df_by_rolling_mean(deeplabcut_df, smoothing_window=31, min_periods=1):
    markers = deeplabcut_df.columns.get_level_values(level=0).unique()
    smooth_df = pd.DataFrame(index=deeplabcut_df.index, columns=deeplabcut_df.columns)
    for marker in markers:
        smooth_df[marker] = deeplabcut_df[marker][["x", "y"]].rolling(smoothing_window, min_periods=min_periods).mean(
            skipna=True).shift(-int(smoothing_window / 2))

    return smooth_df


def clean_deeplabcut_df(deeplabcut_df, **kwargs):
    likelihood_filtered_df = filter_deeplabcut_df_by_likelihood(deeplabcut_df,
                                                                **valid_kwargs(filter_deeplabcut_df_by_likelihood,
                                                                               kwargs))
    z_filtered_df = filter_deeplabcut_df_by_rolling_zscore(likelihood_filtered_df,
                                                           **valid_kwargs(filter_deeplabcut_df_by_rolling_zscore,
                                                                          kwargs))
    return z_filtered_df


def load_marker_df_from_deeplabcut_csv(csv_path, excluded_markers=[], **kwargs):
    input_df = pd.read_csv(csv_path, index_col=0, header=[1, 2]).drop(excluded_markers, axis=1, level=0)
    input_df.columns = input_df.columns.remove_unused_levels()
    input_df = preprocess_deeplabcut_df(input_df, **kwargs)
    marker_df = deeplabcut_df_to_marker_df(input_df)
    return marker_df


def preprocess_deeplabcut_df(deeplabcut_df, clean_data=True, pupil_preprocess=False, interpolate_data=True, **kwargs):
    output_df = deeplabcut_df.copy()
    if pupil_preprocess:
        output_df = filter_pupil_df_by_modified_zscore(output_df, **kwargs)
    if clean_data:
        output_df = clean_deeplabcut_df(output_df, **kwargs)
    if interpolate_data:
        output_df = interpolate_nan_markers_deeplabcut_df(output_df, **kwargs)
    return output_df


def affine_transform_pandas_markers(marker_pd, source_corner_points, rectangle_corner_points, plot_transform=False):
    transform_matrix = cv2.getPerspectiveTransform(source_corner_points[0:4], rectangle_corner_points[0:4])
    transformed_marker_df = warp_pandas_coordinates(marker_pd, transform_matrix)

    if plot_transform:
        import matplotlib.pyplot as plt
        x = np.linspace(0, 1024, 10)
        y = np.linspace(0, 1280, 10)
        X, Y = np.meshgrid(x, y)
        plt.scatter(X, Y)
        x_y_points = zip(X.ravel(), Y.ravel())
        x_y_points_trans = np.vstack([point_transform_2d(point, transform_matrix) for point in x_y_points])
        X_trans, Y_trans = x_y_points_trans[:, 0], x_y_points_trans[:, 1]
        plt.scatter(X_trans, Y_trans)
        plt.show()

        inverse_matrix = cv2.getPerspectiveTransform(rectangle_corner_points[0:4], source_corner_points[0:4])
        x = np.linspace(0, 250, 10)
        y = np.linspace(0, 290, 10)
        X, Y = np.meshgrid(x, y)
        plt.scatter(X, Y)
        x_y_points = zip(X.ravel(), Y.ravel())
        x_y_points_trans = np.vstack([point_transform_2d(point, inverse_matrix) for point in x_y_points])
        X_trans, Y_trans = x_y_points_trans[:, 0], x_y_points_trans[:, 1]
        plt.scatter(X_trans, Y_trans)
        plt.show()

    return transformed_marker_df


def normalize_marker_df(marker_df, reference_point_dict, target_points):
    normalized_df_list = []
    for index_span, reference_points in reference_point_dict.items():
        start, end = index_span
        df_span = marker_df.iloc[int(start):int(end), :]
        corrected_frames = affine_transform_pandas_markers(df_span, reference_points, target_points)
        normalized_df_list.append(corrected_frames)
    return pd.concat(normalized_df_list).sort_index()


def load_reference_point_dict(marker_json_paths, markers_to_extract):
    reference_point_dict = {}
    for marker_json_path in marker_json_paths:
        marker_json_file = marker_json_path.rsplit("\\", 1)[-1].rsplit("/", 1)[-1]
        start, end = re.search(r"(\d+)to(\d+)", marker_json_file).groups()
        marker_dict = read_json(marker_json_path)
        marker_points = np.array([marker_dict[marker] for marker in markers_to_extract], dtype='float32')
        reference_point_dict[(start, end)] = marker_points
    return reference_point_dict


def points_to_kde_array(points, grid_xlim, grid_ylim, grid_shape=(None, None)):
    if grid_shape[0] is None:
        x_size = abs(int(np.diff(grid_xlim)))
    else:
        x_size = grid_shape[0]
    if grid_shape[1] is None:
        y_size = abs(int(np.diff(grid_ylim)))
    else:
        y_size = grid_shape[1]
    x_grid_resolution = (grid_xlim[1] - grid_xlim[0]) / x_size
    y_grid_resolution = (grid_ylim[1] - grid_ylim[0]) / y_size
    x_grid, y_grid = np.mgrid[grid_xlim[0]:grid_xlim[1]:x_grid_resolution, grid_ylim[0]:grid_ylim[1]:y_grid_resolution]
    positions = np.vstack([x_grid.ravel(), y_grid.ravel()])
    points_kde = scipy.stats.gaussian_kde(np.array(points).T)
    return np.reshape(points_kde(positions).T, x_grid.shape).T


# https://stackoverflow.com/a/26757297
def cart2pol(x, y):
    rho = np.sqrt(x ** 2 + y ** 2)
    phi = np.arctan2(y, x)
    return rho, phi


def pol2cart(rho, phi):
    x = rho * np.cos(phi)
    y = rho * np.sin(phi)
    return x, y


def relative_polar_position(origin_pt, destination_pt):
    rho, phi = cart2pol(*(destination_pt - origin_pt))
    return rho, phi


def get_dataframe_by_point_column_in_polygon(input_df, point_col, polygon_points):
    path = Path(polygon_points)
    col_in_polygon = path.contains_points(np.vstack(input_df[point_col]))
    return input_df[col_in_polygon].copy()


# No idea if this is accurate enough
def circular_average(radians, weights=None):
    if weights is None:
        radian_weights = [1.] * len(radians)
    else:
        radian_weights = weights
    point_coords = np.array([pol2cart(weight, radian) for weight, radian in zip(radian_weights, radians)])
    point_average = np.mean(point_coords, axis=0)
    return cart2pol(*point_average)[1]


def deeplabcut_calculate_com(input_pd):
    if type(input_pd) == pd.DataFrame:
        result = input_pd.apply(lambda x: np.mean(x), axis=1)
    elif type(input_pd) == pd.Series:
        result = np.mean(input_pd.values)
    return result


def deeplabcut_calculate_view_angle(input_pd):
    if type(input_pd) == pd.DataFrame:
        result = input_pd.apply(
            lambda x: relative_polar_position(np.mean(x[["left_ear", "right_ear"]].values), x["nose"]), axis=1)
    elif type(input_pd) == pd.Series:
        result = relative_polar_position(np.mean(input_pd[["left_ear", "right_ear"]]), input_pd["nose"])
    return result


def calculate_circle_from_series(input_series, minimum_points=4):
    x, y = np.vstack(input_series.values).T

    x_valid = ~np.isnan(x)
    y_valid = ~np.isnan(y)
    both_valid = x_valid & y_valid

    if np.count_nonzero(x_valid) >= minimum_points and np.count_nonzero(y_valid) >= minimum_points:
        return calculate_circle(x[both_valid], y[both_valid])
    else:
        return [np.nan, np.nan], np.nan


def deeplabcut_calulate_pupil_center_and_radius(input_pd, minimum_points=4):
    pupil_cols = [f"Pupil_{i + 1}" for i in range(8) if f"Pupil_{i + 1}"]
    if type(input_pd) == pd.DataFrame:
        input_cols = input_pd.columns
    elif type(input_pd) == pd.Series:
        input_cols = input_pd.index
    valid_cols = list(set(pupil_cols).intersection(input_cols))
    processing_pd = input_pd[valid_cols]
    if not processing_pd.empty and len(valid_cols) >= minimum_points:
        if type(input_pd) == pd.DataFrame:
            result = processing_pd.apply(calculate_circle_from_series, axis=1, minimum_points=minimum_points)
        elif type(input_pd) == pd.Series:
            result = calculate_circle_from_series(processing_pd, minimum_points=minimum_points)
    else:
        if type(input_pd) == pd.DataFrame:
            result = pd.Series(index=processing_pd.index, data=len(processing_pd.index) * [[[np.nan, np.nan], np.nan]])
        elif type(input_pd) == pd.Series:
            result = ([np.nan, np.nan], np.nan)
    return result


def deeplabcut_calculate_pupil_stats(input_pd, **kwargs):
    pupil_stat_pd = pd.DataFrame(deeplabcut_calulate_pupil_center_and_radius(input_pd).to_list(),
                                 columns=["pupil_position", "pupil_diameter"], index=input_pd.index)
    eye_center = (input_pd["right_corner"] + input_pd["left_corner"]) / 2
    eye_width = deeplabcut_calculate_eye_width(input_pd)
    pupil_stat_pd["pupil_position"] = (pupil_stat_pd["pupil_position"] - eye_center) / (eye_width / 2)
    pupil_stat_pd["pupil_diameter"] = pupil_stat_pd["pupil_diameter"] / eye_width
    return pupil_stat_pd


def deeplabcut_calculate_eye_width(input_pd, **kwargs):
    if type(input_pd) == pd.DataFrame:
        result = (input_pd["right_corner"] - input_pd["left_corner"]).apply(lambda x: np.linalg.norm(x))
    elif type(input_pd) == pd.Series:
        result = np.linalg.norm(input_pd["right_corner"] - input_pd["left_corner"])
    return result


def deeplabcut_calculate_eye_center(input_pd, **kwargs):
    if type(input_pd) == pd.DataFrame:
        result = np.median(np.vstack(input_pd["right_corner"] - input_pd["left_corner"]))
    elif type(input_pd) == pd.Series:
        result = input_pd["right_corner"] - input_pd["left_corner"]
    return result


def calculate_circle(x_vals, y_vals, return_residue=False):
    # from scipy cookbook
    def calc_R(xc, yc):
        """ calculate the distance of each data points from the center (xc, yc) """
        return np.sqrt((x_vals - xc) ** 2 + (y_vals - yc) ** 2)

    def f(c):
        """ calculate the algebraic distance between the 2D points and the mean circle centered at c=(xc, yc) """
        Ri = calc_R(*c)
        return Ri - Ri.mean()

    def Df(c):
        """ Jacobian of f_2b
        The axis corresponding to derivatives must be coherent with the col_deriv option of leastsq"""
        xc, yc = c
        df_dc = np.empty((len(c), x_vals.size))

        Ri = calc_R(xc, yc)
        df_dc[0] = (xc - x_vals) / Ri  # dR/dxc
        df_dc[1] = (yc - y_vals) / Ri  # dR/dyc
        df_dc = df_dc - df_dc.mean(axis=1)[:, np.newaxis]

        return df_dc

    center_estimate = np.mean(x_vals), np.mean(y_vals)
    center, ier = optimize.leastsq(f, center_estimate, Dfun=Df, col_deriv=True)
    radii = calc_R(*center)
    radius = radii.mean()
    result = center, radius
    if return_residue:
        result = *result, sum((radii - radius) ** 2)
    return result


def check_for_na(input_pd):
    if type(input_pd) == pd.DataFrame:
        apply_func = pd.DataFrame.applymap
    elif type(input_pd) == pd.Series:
        apply_func = pd.Series.apply
    return apply_func(input_pd, lambda x: pd.isna(x))
