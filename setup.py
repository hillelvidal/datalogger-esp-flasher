"""
Setup script for ESP32 Firmware Flasher
"""

from setuptools import setup, find_packages
from pathlib import Path

# Read README for long description
readme_file = Path(__file__).parent / "README.md"
long_description = readme_file.read_text(encoding="utf-8") if readme_file.exists() else ""

# Read requirements
requirements_file = Path(__file__).parent / "requirements.txt"
requirements = []
if requirements_file.exists():
    requirements = requirements_file.read_text().strip().split('\n')
    requirements = [req.strip() for req in requirements if req.strip() and not req.startswith('#')]

setup(
    name="esp32-datalogger-flasher",
    version="1.0.0",
    author="ESP32 Flasher Team",
    author_email="support@esp32flasher.com",
    description="A user-friendly tool for flashing firmware to ESP32-based datalogger devices",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/yourusername/datalogger-esp-flasher",
    packages=find_packages(),
    classifiers=[
        "Development Status :: 4 - Beta",
        "Intended Audience :: Developers",
        "Intended Audience :: End Users/Desktop",
        "Topic :: Software Development :: Embedded Systems",
        "Topic :: System :: Hardware",
        "License :: OSI Approved :: MIT License",
        "Programming Language :: Python :: 3",
        "Programming Language :: Python :: 3.7",
        "Programming Language :: Python :: 3.8",
        "Programming Language :: Python :: 3.9",
        "Programming Language :: Python :: 3.10",
        "Programming Language :: Python :: 3.11",
        "Operating System :: OS Independent",
        "Environment :: Console",
    ],
    python_requires=">=3.7",
    install_requires=requirements,
    extras_require={
        "dev": [
            "pytest>=7.0.0",
            "pytest-cov>=4.0.0",
            "black>=22.0.0",
            "flake8>=5.0.0",
            "mypy>=0.991",
        ],
    },
    entry_points={
        "console_scripts": [
            "esp32-flasher=src.main:main",
            "datalogger-flasher=src.main:main",
        ],
    },
    include_package_data=True,
    package_data={
        "": ["*.md", "*.txt", "*.bin"],
        "firmware": ["*.bin"],
    },
    project_urls={
        "Bug Reports": "https://github.com/yourusername/datalogger-esp-flasher/issues",
        "Source": "https://github.com/yourusername/datalogger-esp-flasher",
        "Documentation": "https://github.com/yourusername/datalogger-esp-flasher#readme",
    },
    keywords="esp32 firmware flash embedded iot datalogger esptool",
    zip_safe=False,
)
