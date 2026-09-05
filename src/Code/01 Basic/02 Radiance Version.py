ghenv.Component.Message = """FlahaGrow 0.0
Radiance Version"""



import subprocess

version = None

if _radiance:
    try:
        result = subprocess.run(["rcontrib", "-version"], capture_output=True, text=True)

        if result.returncode == 0:
            version = result.stdout.strip()
        else:
            version = "Radiance found but version command failed."
    except Exception as e:
        version = f"Error: {str(e)}"
