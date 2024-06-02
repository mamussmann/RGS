# Point Cloud Modeling Tool Readme

## Runtime
Contains all relevant files to the data structure.
The DataConstants.cs file contains all configurable values.
## Tests
Contains unit tests for testing some parts of the data structure.
## Samples
### Benchmark
This example provides a simple way of testing the performance with different configurations. The results with average and median times are written to a json file in the data folder.
### Showcase
Example implementation for possible operations that manipulate a point cloud.
Ply files are loaded/saved relative to the data folder.

Controls:

Edit Mode:
| Key | Description             |
| --- | ----------------------- |
| Q   | Switching to cube mode  |
| F   | Switching to scale mode |
| LMB | Add/draw/scale up       |
| RMB | Remove/draw/scale down  |

Cube Mode:

| Key | Description                |
| --- | -------------------------- |
| LMB | Set Origin for add cube    |
| RMB | Set Origin for remove cube |

Scale Mode:

| Key | Description              |
| --- | ------------------------ |
| LMB | Submit brush size change |
| RMB | Reset brush size         |