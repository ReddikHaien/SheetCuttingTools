# Introduction
This is an library & grasshopper plugin for sheet cutting. 

# Structure
The lilbrary is structured into the following parts
```mermaid
flowchart RL
subgraph Actions
    direction TB
    M([Model])
    A[Segmentation]
    B[Flattening]
    C[Geometry making]
    E([Sheet instructions])
    M --> A --> B --> C --> E
end

subgraph Behaviors
    direction TB
    B1[Edge Filters]
    B2[Segmentation Constraints]
    B3[Flattened Segmentation Constraints]
    B4[Polygon Scorers]
    B1 ~~~ B2 ~~~ B3 ~~~ B4
end
Behaviors --> Actions
```

`Actions` are components that act directly upon the input models. They are grouped into three categories.
1. Segmentation
2. Flattening
3. Geometry making

Actions can be modified by providing them with `Behaviors`. Behaviors are components that provides additional information to Action components. Current behaviour kinds are
1. Edge filters
2. Polygon scorers
3. Segmentation constraints
4. Flattened segmentation constriants


## Actions
### Segmentation
The segmentation components are responsible for creating a rough segmentation of a model. This can also be done using the flattening components. But serves as a simpler method of creating smaller segments before flattening them out. E.g. radially dividing a vase, or constraint a segmentation to only produce convex segments. The segmentation components are designed to be nested

#### Greedy segmentator
The greedy segmentator is a "first-come-first-served" based polygon segmentation algorithm. It tries to expand a segment as long as all constraints are fulfilled. It starts on a new segment once it can't extend a segment anymore. This segmentation component produces large, uneven sheets, though can work Ok on simpler geometry.

#### Cake slicer segmentator
The cake slicer segmentator is a segmentation component that divides a component radially into even segments (Like a cake!)

### Flattening
The flattening components are responsible for converting models/segments into 2D representations that can be folded back into the original shape. How a shape is flattened out is based on the specific component, e.g. the greedy flattener produces large and uneven segments, while a different implementation might produce a better result.

### Geometry Making

Geometry making components are responsible for producing the final result that can be fed into a sheet cutter. These components uses flattened segments, and based on the componets/behaviors, produced geometry suited for different kinds of materials. E.g. the Paper geometry maker is more suited for thin foldable materials (like paper).

The main modules are Segmentation and the Geometry making. Segmentation converts the input model into segments that can be further processed. Geometry making is not currently implemented, but will be responsible for producing geometry that is fit for sheet cutting. Some examples of geometry making's responsibility is lattice hinges, integral joints, locking mechanism, etc...

Segmentation and geometry making is configured by using behaviors. Behaviors are small functionalitites that act as filters, constraints and requirements on different parts of the segmentation and geometry pipeline. Behaviours represents a family of different subtypes. Currently the following behaviour types are available
- Polygon selectors
	- Polygon selectors are used to score polygons in a sheet. They can be used to select the initial polygon in segment creation. When multiple polygon selectors are in use. The average value is used as the final score.
- Edge filtes
	- Edge filters are, as the name suggest, used to filter out which edges are applicable for use. This is used by one of the segment builders to select wich edges can be used to build hinges. Multiple edge filters are combined with the AND operator. 
- Segment constraints
	- Segment constraints are used to determine if a polygon can be added to a segment.
		- Some examples of segment constraints are dimensions, aspect ratio and self intersections


# Demonstration

For demonstrating purposes, the following mesh will be used:
![[Pasted image 20250120192738.png]]

# ReactiveSegmentationBuilder

The ReactiveSegmentationBuilder is a greedy algorithm that will do a "best-fit" approach to build segments. Each segment will be expanded until they are limited by the constraints specified. This can lead to unbalanced segments where some might consist of 90% of the polygons, with a few small segments to fill the gaps. The component requires at least one behaviour to function. Given a uselsess component(Nickname is MinEdgeLength). The component produces the following segment
![[Pasted image 20250120194214.png]]
Here you can see one large segment(red).
![[Pasted image 20250120194421.png]]
The unfolded mesh looks like this
![[Pasted image 20250120194714.png]]
Somewhat usesless. Lets fix it

## SegmentConstaints
### SegmentIntersectionConstraint
SegmentIntersectionConstraint is responsible for preventing self intersecting segments from being formed. It checks that the unfolded representation of the segment is free from overlapping polygongs.
With the above mesh and the following grasshopper config:
![[Pasted image 20250120194951.png]]
8 segments are produced with the following shapes
1. ![[Pasted image 20250120195130.png]]
2. ![[Pasted image 20250120195202.png]]
3. ![[Pasted image 20250120195218.png]]
4. ![[Pasted image 20250120195456.png]]

The remaining sections are similar to 3 with few   with few polygons. The resulting segmentated mesh looks like this
![[Pasted image 20250120195420.png]]
The first segment is still the largest due to the "best-fit" approach. But now it's possible to produce every segment without overlaps.

### SegmentDimensionConstraint
The SegmentDimensionConstraint locks the maximum size of the segments. This can be used to ensure that each segment can fit within a given boundary, e.g. a A4 sheet of paper.'
Given the above grasshopper script with the dimension constraint set to an A4 sheet
![[Pasted image 20250120200121.png]]
9 segments are produced with the following mesh:
![[Pasted image 20250120200212.png]]
Segment 1 (red) is still large, but another section is of equal size. This is due to the constraint preventing the segment from expanding beyond our requirements.
The red segment now looks like this
![[Pasted image 20250120200358.png]]
As you can see, the segment perfectly fits into a sheet of paper.

### SegmentAspectRatioConstraint
SegmentAspectRatioConstraint tries to keep the aspect ratio of the segment within a specified treshold. For example it can be 2x1, 3x4, 1x1, etc.
The component has 3 inputs
- aspect ratio
	- This is the target aspect ratio of the segment, calculated as w/h
- variation
	- This is how much the aspect ratio can vary in the segment, variation is +- the target aspect ratio
- activation treshold
	- This is an unsigned value indicating when the constraint should take full effect. The treshold is applied as `min((number of polygons)/(activation treshold), 1)` meaning that the treshold will increase in strength as the segment grows. The reason this input is provided is due to small segments having bad aspect ratio. This input then allows the segment to reach an desireable size before applying the aspect ratio check.

# TO COME
## Geometry
will come a component with logic from the old version of the unwrapper

## Voronoi
a simple 3d voroni model intersection for constraints

## Paintbrush
shall draw up idea on paper.