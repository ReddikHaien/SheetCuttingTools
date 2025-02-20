# Change log
this is the change log for this repo, based on https://keepachangelog.com/en/1.0.0/
The project used [semantic versioning](https://semver.org/). 


## Unreleased
### Added
### Tools
- Added initial behaviors
  - Added behaviour type `IEdgeFilter`
    - Added `Min edge length filter`
  - Added behavior type `IPolygonScorer`
    - Added `Close to center polygon scorer`
  - Added behavior type `ISegmentConstraint`
    - Added `Segment dimension constraint`
  - Added behavior type `IFlattenedSegmentConstraint`
    - Added `Flattened segment intersection constraint`
    - Added `Flattened segment dimension constraint`

- Added initial rough segmentation tools
  - Added `Cake slicer segmentor`
  - Added `Greedy segmentor`
  - Added `Layer slicer segmentor`

- Added initial flattening segmentation tools
  - Added `Greedy flattening segmentor`
  - Added `Strip segment unroller`

- Added initial geometry making tools
  - Added `Paper geometry maker`

- Added base types for geometry
  - Added `IGeometry`
  - Added `IFlattenedGeometry`
  - Added `Sheet`

### Rhino
- Added support for converting Brep, Mesh, SubD into `IGeometry`
- Added support for converting `IGeometry` into SubD

### Changed

### Removed

