import pandas as pd

_colors = [
    '#1f77b4',  # muted blue
    '#ff7f0e',  # safety orange
    '#2ca02c',  # cooked asparagus green
    '#d62728',  # brick red
    '#9467bd',  # muted purple
    '#8c564b',  # chestnut brown
    '#e377c2',  # raspberry yogurt pink
    '#7f7f7f',  # middle gray
    '#bcbd22',  # curry yellow-green
    '#17becf'  # blue-teal
]


def build_volcano_dict(dataset):
    plot_order = ('Astrocytes', 'Microglia', 'Myelinating Oligodendrocytes', 'Newly Formed Oligodendrocyte',
                  'Oligodendrocyte Precursor Cell', 'Endothelial Cells')
    abbreviations = ('Astrocytes', 'Microglia', 'Myelin. Oligodend.', 'Newly Formed Oligodend.',
                     'Oligodend. Precursor Cell', 'Endoth. Cells')

    # make figure
    fig_dict = {
        "data": [],
        "layout": {},
    }

    # fill in most of layout
    fig_dict["layout"]["xaxis"] = {"title": "log<sub>2</sub> fold change", "type": "linear", "range": (-5, 5),
                                   "tickvals": [x for x in range(-5, 5+1)]}
    fig_dict["layout"]["yaxis"] = {"title": "-log<sub>10</sub> P<sub>adj</sub>", "type": "log", "range": (0, -12),
                                   "tickvals": [pow(10, x) for x in range(0, -12, -1)],
                                   "ticktext": [-x for x in range(0, -12, -1)]}
    fig_dict["layout"]["hovermode"] = "closest"
    fig_dict["layout"]["clickmode"] = 'event+select'

    # make data
    for i, celltype in enumerate(plot_order):
        dataset_by_celltype = dataset[dataset["most_abundant"] == celltype]

        data_dict = {
            "x": list(dataset_by_celltype["log2FoldChange"]),
            "y": list(dataset_by_celltype["padj"]),
            "customdata": list(dataset_by_celltype["gene_id"]),
            "type": "scatter",
            "mode": "markers",
            "text": [f'Name: {a}<br>baseMean: {b}' for a, b in
                     zip(list(dataset_by_celltype["gene_name"]),
                         list(dataset_by_celltype["baseMean"]))],
            "marker": {'opacity': 1.0,
                       'color': _colors[i]},
            "name": celltype,
            "legendgroup": celltype,
            "showlegend": False
        }
        fig_dict["data"].append(data_dict)

        legend_entry_dict = {
            "x": [None],
            "y": [None],
            "type": "scatter",
            "mode": "markers",
            "marker": {'color': _colors[i]},
            "name": celltype,
            "legendgroup": celltype,
            "showlegend": True
        }
        fig_dict["data"].append(legend_entry_dict)

    return fig_dict


def build_ma_dict(dataset):
    def _fading(padj):
        sequence = padj.apply(lambda x: 0.2 if pd.isna(x) else 1.0 - 0.6 * (x >= 0.01))
        return list(sequence)

    plot_order = ('Astrocytes', 'Microglia', 'Myelinating Oligodendrocytes', 'Newly Formed Oligodendrocyte',
                  'Oligodendrocyte Precursor Cell', 'Endothelial Cells')
    abbreviations = ('Astrocytes', 'Microglia', 'Myelin. Oligodend.', 'Newly Formed Oligodend.',
                     'Oligodend. Precursor Cell', 'Endoth. Cells')

    # make figure
    fig_dict = {
        "data": [],
        "layout": {},
    }

    # fill in most of layout
    fig_dict["layout"]["xaxis"] = {"title": "baseMean", "type": "log", "range": (-2, 6),
                                   "tickvals": (0.1, 10, 1000, 100000)}
    fig_dict["layout"]["yaxis"] = {"title": "log2FoldChange", "type": "linear", "range": (-5, 5),
                                   "tickvals": [x for x in range(-5, 5+1)],
                                   "ticktext": [x for x in range(-5, 5+1)]}
    fig_dict["layout"]["hovermode"] = "closest"

    # make data
    for i, celltype in enumerate(plot_order):
        dataset_by_celltype = dataset[dataset["most_abundant"] == celltype]

        data_dict = {
            "x": list(dataset_by_celltype["baseMean"]),
            "y": list(dataset_by_celltype["log2FoldChange"]),
            "customdata": list(dataset_by_celltype["gene_id"]),
            "type": "scatter",
            "mode": "markers",
            "text": [f'Name: {a}<br>padj: {b}' for a, b in zip(list(dataset_by_celltype["gene_name"]),
                                                               list(dataset_by_celltype["padj"]))],
            "marker": {'opacity': _fading(dataset_by_celltype['padj']),
                       'color': _colors[i]},
            "name": celltype,
            "legendgroup": celltype,
            "showlegend": False
        }
        fig_dict["data"].append(data_dict)

        legend_entry_dict = {
            "x": [None],
            "y": [None],
            "type": "scatter",
            "mode": "markers",
            "marker": {'color': _colors[i]},
            "name": celltype,
            "legendgroup": celltype,
            "showlegend": True
        }
        fig_dict["data"].append(legend_entry_dict)

    return fig_dict