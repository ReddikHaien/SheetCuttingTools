# Change log
this is the change log for this repo, based on https://keepachangelog.com/en/1.0.0/
The project used [semantic versioning](https://semver.org/). 

## Unreleased

### Added
- Added Initial behavior project
  - Added Behavior type `IEdgeFilter`
    - Added filter for min edge length
  - Added Behavior type `IPolygonScorer`
    - Added scorer for distance from model center
  - Added Behavior type `ISegmentationConstraint`
    - Added constraint on segment dimension
  - Added Behavior type `IFlattenedSegmentConstraint`
    - Added constraint on self intersection

- Added Initial unrolling project
  - Added greedy unroller algorithm

- Added Initial segmentation project
  - Added Greedy segmentation algorithm
  - Added Radial slicer algorithm

- Added Initial geometry making project
  - Added geometry making for paper based structures (glue taps)

### Changed

### Removed

