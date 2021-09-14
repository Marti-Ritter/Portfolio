import inspect
import json


def write_json(location, dictionary):
    with open(location, 'w') as f:
        json.dump(dictionary, f)


def read_json(location):
    with open(location, 'r') as f:
        dictionary = json.load(f)
    return dictionary


# https://stackoverflow.com/a/12627202
def get_default_args(func):
    signature = inspect.signature(func)
    return {
        k: v.default
        for k, v in signature.parameters.items()
        if v.default is not inspect.Parameter.empty
    }


def valid_kwargs(func, kwargs):
    valid_keys = kwargs.keys() & get_default_args(func).keys()
    return {k: kwargs[k] for k in valid_keys}
