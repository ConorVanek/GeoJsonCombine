# GeoJsonCombine
A c# program to combine neighboring GeoJson polygons.

The goal of this program is to recursively combine neighboring polygons into one shape until there are no more shapes that can be combined. Here are the steps:

For each shape A in the directory…
1.	Find a shape B that is next to it, i.e., a shape that can be combined with it. If there is not one, do nothing and move on to the next shape.
2.	Create an empty shape C. This will be the new geojson file.
3.	Start at the first point from the beginning of shape A that is not directly next to shape B.
4.	For each point until the starting point is returned to:
a.	Check if the point is inside the other shape. If no, add the point to shape C. If yes, move to the nearest point on the other shape that is not inside the current shape and continue from there.
5.	If you have reached the end of the current shape, loop back to the beginning.
6.	Keeping track of the first point you started with, if the current point equals the starting point you have finished the new shape C.
7.	Save shape C as a geojson and delete shapes A and B.
8.	Refresh the working directory and repeat until no more shapes can be combined.
We will need:
•	A selector variable. If 0, we are adding points for shape A. If 1, we are adding points from shape B.
•	A counter variable that keeps track of the current position in the shape
•	A Haversine function to find the distance between two shapes.
•	A Function that finds the closest point on the other shape to a given point of the current shape
•	A function that stretches a polygon from the centroid slightly and returns the stretched polygon… This is needed because the shapes don’t intersect (they neighbor each other), so by enlarging them slightly then we can easily traverse all the points that are not inside the other shape.
