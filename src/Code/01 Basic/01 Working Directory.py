import os


if not _root_folder:
    raise ValueError("Root folder path is empty.")


if not os.path.exists(_root_folder):
    os.makedirs(_root_folder)

_root_folder_out = _root_folder



def ensure_folder(parent, folder_name, toggle):
    if not toggle:
        return None

    path = os.path.join(parent, folder_name)

    # Create subfolder if missing
    if not os.path.exists(path):
        os.makedirs(path)

    return path



PIT_ill_folder = ensure_folder(
    _root_folder,
    "point_in_time_illuminance",
    _point_in_time_illuminance
)

PIT_render_folder = ensure_folder(
    _root_folder,
    "point_in_time_render",
    _point_in_time_render
)

ann_ill_folder = ensure_folder(
    _root_folder,
    "annual_illuminance",
    _annual_illuminance
)

electric_ill_folder = ensure_folder(
    _root_folder,
    "electric_illuminance",
    _electric_illuminance
)

spec_PIT_folder = ensure_folder(
    _root_folder,
    "spectral_point_in_time",
    _spectral_point_in_time
)

spec_PIT_ren_folder = ensure_folder(
    _root_folder,
    "spectral_point_in_time_render",
    _spectral_point_in_time_render
)

spec_ann_ill_folder = ensure_folder(
    _root_folder,
    "spectral_annual_illuminance",
    _spectral_annual
)
