using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace geojsonCombine
{

	public class Program
	{
		
		public class ZoneRingPoint
		{
			public double x;
			public double y;

			public ZoneRingPoint(double x, double y)
			{
				this.x = x;
				this.y = y;
			}
		};
		
		public class FilePair
		{
			public string file1;
			public string file2;
			
			public FilePair(string file1, string file2)
			{
				this.file1 = file1;
				this.file2 = file2;
			}
		};
		
		public class Ring
		{
			public List<ZoneRingPoint> points;
			public int length;
			public ZoneRingPoint centroid;
			
			public Ring()
			{
				points = new List<ZoneRingPoint>();
				length = 0;	
			}
			
			public void add(ZoneRingPoint p)
			{
				this.points.Add(p);
				this.length = this.points.Count;
				this.centroid = getCentroid(points);
			}
			
			public void add(List<ZoneRingPoint> r)
			{
				foreach(ZoneRingPoint p in r)
				{
					this.points.Add(p);
				}
			}
			
			private ZoneRingPoint getCentroid(List<ZoneRingPoint> polygon)
			{	
				double avgx = polygon.Average(x => x.x);
				double avgy = polygon.Average(x => x.y);
				ZoneRingPoint c = new ZoneRingPoint(avgx, avgy);
				return c;
			}
		};
	
		public static double HaversineDistance(ZoneRingPoint pos1, ZoneRingPoint pos2)
		{
			double R = 3958.76;
			var lat1 = pos1.y * Math.PI / 180;
			var lat2 = pos2.y * Math.PI / 180;
			var lng1 = pos1.x * Math.PI / 180;
			var lng2 = pos2.x * Math.PI / 180;
			var dlat = lat2 - lat1;
			var dlng = lng2 - lng1;
			
			var a = Math.Sin(dlat/2)*Math.Sin(dlat/2) + Math.Cos(lat1)*Math.Cos(lat2)*Math.Sin(dlng/2)*Math.Sin(dlng/2);
			var c = 2*Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
			return R*c;
		}
	
		static string[]  getFiles()
		{
			// Get files, sorted by area.
			string[] files = Directory.GetFiles("files", "*.geojson");
			files = sortFiles(files);
			return files;
		}

		static double INF = 10000;

		static double orientation(ZoneRingPoint p, ZoneRingPoint q, ZoneRingPoint r)
		{
			double val = (q.y - p.y) * (r.x - q.x) -
					  (q.x - p.x) * (r.y - q.y);

			if (val == 0)
			{
				return 0; // colinear 
			}
			return (val > 0) ? 1 : 2; // clock or counterclock wise 
		}
		
		static ZoneRingPoint getCentroid(List<ZoneRingPoint> polygon)
		{
			// Get centroid of shape
			double avgx = polygon.Average(x => x.x);
			double avgy = polygon.Average(x => x.y);
			ZoneRingPoint c = new ZoneRingPoint(avgx, avgy);
			return c;
		}
		static ZoneRingPoint getCentroid(List<ZoneRingPoint> p1, List<ZoneRingPoint> p2)
		{
			// Get combined centroid of both shapes.
			double avgx1 = p1.Average(x => x.x);
			double avgy1 = p1.Average(x => x.y);
			double avgx2 = p2.Average(x => x.x);
			double avgy2 = p2.Average(x => x.y);
			double avgx = (avgx1 + avgx2) /2;
			double avgy = (avgy1 + avgy2) /2;
			ZoneRingPoint c = new ZoneRingPoint(avgx, avgy);
			return c;
		}

		static void saveGeojson(List<ZoneRingPoint> polygon, string filename)
		{
			// Saves a polygon object to a geojson file.
			filename += ".geojson";
			string spoint;
			string filetext = "{\"type\": \"FeatureCollection\",\n\"name\": \"" + "none" + "\",\n\"crs\": {\n \"type\": \"name\", \"properties\": { \"name\": \"urn:ogc:def:crs:OGC:1.3:CRS84\" } },\"features\": [{ \"type\": \"Feature\", \"properties\": { \"Name\": \"none\", \"description\": null, \"timestamp\": null, \"begin\": null, \"end\": null, \"altitudeMode\": null, \"tessellate\": -1, \"extrude\": 0, \"visibility\": -1, \"drawOrder\": null, \"icon\": null }, \"geometry\": { \"type\": \"Polygon\", \"coordinates\": [ [";
			int i=0;
			foreach(ZoneRingPoint p in polygon)
			{
				if(i==0)
				{
					spoint = "[" + String.Format("{0:0.000000}", p.x) + ", " + String.Format("{0:0.000000}", p.y) + "]";
				}
				else 
				{
					spoint = ",[" + String.Format("{0:0.000000}", p.x) + ", " + String.Format("{0:0.000000}", p.y) + "]";
				}
				filetext += spoint;
				i++;
			}
			filetext += "]]}}]}";
			File.WriteAllText(filename, filetext);
			return;
			
		}
		
		static List<ZoneRingPoint> loadGeojson(string filename)
		{
			// Reads a valid geojson polygon and returns a ZoneRingPoint object.
			List<ZoneRingPoint> newPolygon = new List<ZoneRingPoint>();
			string jsonString = File.ReadAllText(filename);
			
			using (JsonDocument document = JsonDocument.Parse(jsonString))
			{
				JsonElement root = document.RootElement;
				JsonElement features = root.GetProperty("features");
				JsonElement geometry = features[0].GetProperty("geometry");
				JsonElement pointsElement = geometry.GetProperty("coordinates");
				foreach (JsonElement point in pointsElement[0].EnumerateArray())
				{
					newPolygon.Add(new ZoneRingPoint(point[0].GetDouble(), point[1].GetDouble()));
				}
			}
			
			return newPolygon;
		}
		
		static List<ZoneRingPoint> stretchPolygonVertical(List<ZoneRingPoint> polygon)
		{
			// Inputs a polygon and returns a slightly vertically stretched version of the polygon.
			double rise, run, sx, sy;
			ZoneRingPoint centroid = getCentroid(polygon);
			List<ZoneRingPoint> stretched = new List<ZoneRingPoint>();
			foreach (ZoneRingPoint p in polygon)
			{
				rise = p.y - centroid.y;
				run = p.x - centroid.x;
				sx = p.x;
				sy = p.y + 0.01*rise;
				stretched.Add(new ZoneRingPoint(sx, sy));
			}
			//saveGeojson(stretched, String.Format("{0:0.000000}", polygon[0].x));
			return stretched;
		}
		
		static List<ZoneRingPoint> stretchPolygonHorizontal(List<ZoneRingPoint> polygon)
		{
			// Inputs a polygon and returns a slightly horizontally stretched version of the polygon.
			double rise, run, sx, sy;
			ZoneRingPoint centroid = getCentroid(polygon);
			List<ZoneRingPoint> stretched = new List<ZoneRingPoint>();
			foreach (ZoneRingPoint p in polygon)
			{
				rise = p.y - centroid.y;
				run = p.x - centroid.x;
				sx = p.x + 0.01*run;
				sy = p.y;
				stretched.Add(new ZoneRingPoint(sx, sy));
			}
			//saveGeojson(stretched, String.Format("{0:0.000000}", polygon[0].x));
			return stretched;
		}
		
		static List<ZoneRingPoint> stretchPolygon(List<ZoneRingPoint> polygon)
		{
			// Inputs a polygon and returns a slightly enlarged version of the polygon.
			double rise, run, sx, sy;
			ZoneRingPoint centroid = getCentroid(polygon);
			List<ZoneRingPoint> stretched = new List<ZoneRingPoint>();
			foreach (ZoneRingPoint p in polygon)
			{
				rise = p.y - centroid.y;
				run = p.x - centroid.x;
				sx = p.x + 0.01*run;
				sy = p.y + 0.01*rise;
				stretched.Add(new ZoneRingPoint(sx, sy));
			}
			//saveGeojson(stretched, String.Format("{0:0.000000}", polygon[0].x));
			return stretched;
		}
		
		static List<ZoneRingPoint> stretchPolygon(List<ZoneRingPoint> polygon, ZoneRingPoint centroid)
		{
			// Inputs a polygon and returns a slightly enlarged version of the polygon around a given centroid.
			double rise, run, sx, sy;
			List<ZoneRingPoint> stretched = new List<ZoneRingPoint>();
			foreach (ZoneRingPoint p in polygon)
			{
				rise = p.y - centroid.y;
				run = p.x - centroid.x;
				sx = p.x + 0.01*run;
				sy = p.y + 0.01*rise;
				stretched.Add(new ZoneRingPoint(sx, sy));
			}
			//saveGeojson(stretched, String.Format("{0:0.000000}", polygon[0].x));
			return stretched;
		}

		static int findNearestPoint(ZoneRingPoint p1, ZoneRingPoint p0, List<ZoneRingPoint> otherRing, List<ZoneRingPoint> invalidPoints)
		{
			// Returns the index of the closest (valid) point on the "other shape" to two given points on the "current shape".
			double distance;
			double closest_d = 1000000.0;
			int index = 0;
			int n = 0;
			foreach(ZoneRingPoint p2 in otherRing)
			{
				// Distance between p1&p2, and p0&p2, averaged.
				distance = (HaversineDistance(p1, p2) + HaversineDistance(p0,p2)) /2; 
				if(0.0 < distance && distance < closest_d && !invalidPoints.Contains(p2))
				{
					// Keep track of the shortest distance and the index of the point.
					index = n;
					closest_d = distance;
				}
				n++;
			}
			return index;
		}
		
		static void removeFiles(string file1, string file2)
		{
			// Remove two files... This is called after file1 and file2 have been successfully combined into a new ring.
			Task removefiles = Task.Run( () => {
				if (File.Exists(file1))
				{
					File.Delete(file1);
				}
				if (File.Exists(file2))
				{
					File.Delete(file2);
				}
			} );
			removefiles.Wait();
			return;
		}

		static bool onSegment(ZoneRingPoint p, ZoneRingPoint q, ZoneRingPoint r)
		{
			if (q.x <= Math.Max(p.x, r.x) &&
				q.x >= Math.Min(p.x, r.x) &&
				q.y <= Math.Max(p.y, r.y) &&
				q.y >= Math.Min(p.y, r.y))
			{
				return true;
			}
			return false;
		}

		static bool doIntersect(ZoneRingPoint p1, ZoneRingPoint q1, ZoneRingPoint p2, ZoneRingPoint q2)
		{
				// Find the four orientations needed for  
				// general and special cases 
			double o1 = orientation(p1, q1, p2);
			double o2 = orientation(p1, q1, q2);
			double o3 = orientation(p2, q2, p1);
			double o4 = orientation(p2, q2, q1);

			// General case 
			if (o1 != o2 && o3 != o4)
			{
				return true;
			}

			// Special Cases 
			// p1, q1 and p2 are colinear and 
			// p2 lies on segment p1q1 
			if (o1 == 0 && onSegment(p1, p2, q1))
			{
				return true;
			}

			// p1, q1 and p2 are colinear and 
			// q2 lies on segment p1q1 
			if (o2 == 0 && onSegment(p1, q2, q1))
			{
				return true;
			}

			// p2, q2 and p1 are colinear and 
			// p1 lies on segment p2q2 
			if (o3 == 0 && onSegment(p2, p1, q2))
			{
				return true;
			}

			// p2, q2 and q1 are colinear and 
			// q1 lies on segment p2q2 
			if (o4 == 0 && onSegment(p2, q1, q2))
			{
				return true;
			}

			// Doesn't fall in any of the above cases 
			return false;
		}

		static bool isInside(List<ZoneRingPoint> polygon, int n, ZoneRingPoint p)
		{

			// There must be at least 3 vertices in polygon[] 
			if (n < 3)
			{
				return false;
			}

			// Create a point for line segment from p to infinite 
			ZoneRingPoint extreme = new ZoneRingPoint(INF, p.y);

			// Count intersections of the above line  
			// with sides of polygon 
			int count = 0, i = 0;
			do
			{
				int next = (i + 1) % n;

				// Check if the line segment from 'p' to  
				// 'extreme' intersects with the line  
				// segment from 'polygon[i]' to 'polygon[next]' 
				if (doIntersect(polygon[i],
								polygon[next], p, extreme))
				{
					// If the point 'p' is colinear with line  
					// segment 'i-next', then check if it lies  
					// on segment. If it lies, return true, otherwise false 
					if (orientation(polygon[i], p, polygon[next]) == 0)
					{
						return onSegment(polygon[i], p,
										 polygon[next]);
					}
					count++;
				}
				i = next;
			} while (i != 0);

			// Return true if count is odd, false otherwise 
			return (count % 2 == 1); // Same as (count%2 == 1) 
		}

		static bool canCombine(List<ZoneRingPoint> shapeA, List<ZoneRingPoint> shapeB)
		{
			// Looks for a place where the rings can be combined and returns true if it finds one, otherwise returns false.
			bool overlap = false;
			List<ZoneRingPoint> stretchedA = stretchPolygon(shapeA);
			List<ZoneRingPoint> stretchedB = stretchPolygon(shapeB);
			int sizeA = stretchedA.Count -1;
			int sizeB = stretchedB.Count -1;
			
			foreach(ZoneRingPoint p in stretchedA)
			{
				if (isInside(stretchedB, sizeB, p))
				{
					overlap = true;
					return overlap;
				}
			}
			foreach(ZoneRingPoint p in stretchedB)
			{
				if (isInside(stretchedA, sizeA, p))
				{
					overlap = true;
					break;
				}
			}
			return overlap;
		}
		
		static int getFarthestIndex(List<ZoneRingPoint> shapeA, List<ZoneRingPoint> shapeB)
		{
			// Returns the index of the farthest point on shape A from any point on shape B
			double farthest = 0.0;
			double d;	// distance
			ZoneRingPoint pA;
			ZoneRingPoint pB; // Point on shape a, point on shape b.
			int sizeA = shapeA.Count -1;
			int sizeB = shapeB.Count -1;
			int farthestIndex = 0;
			
			for (int m=0; m < sizeA; m++)
			{
				for(int n=0; n < sizeB; n++)
				{
					pA = shapeA[m];
					pB = shapeB[n];
					d = HaversineDistance(pA, pB);
					if(d > farthest)
					{
						farthest = d;
						farthestIndex = m;
					}
				}
			}
			return farthestIndex;
		}
		
		static int getFirstIndex(List<ZoneRingPoint> shapeA, List<ZoneRingPoint> shapeB, List<ZoneRingPoint> invalidPoints)
		{
			int startIndex = -1;
			
			// The first index is the point that is farthest from any point on the other shape. This helps avoid some infinite loops or incorrect shapes.
			startIndex = getFarthestIndex(shapeA, shapeB);
			return startIndex;
		}
		
		static double getArea(List<ZoneRingPoint> shape)
		{
			double area = 0.0;
			for(int i=0; i+1<shape.Count; i++)
			{
				if(i+1<shape.Count)
				{
					area += (shape[i].x)*(shape[i+1].y) - (shape[i].y)*(shape[i+1].x);
				}
				else
				{
					area += (shape[i].x)*(shape[0].y) - (shape[i].y)*(shape[0].x);
				}
			}
			area = Math.Abs(area/2);
			return area;	
		}
		
		static string [] sortFiles(string [] files)
		{
			// There seemed to be less issues when combining files of similar size... So I sort the files by area before combining. "QuickSort" algorithm is used.
			double [] areas = new double[files.Length];
			int low = 0;
			int high = files.Length-1;
			List<List<ZoneRingPoint>> rings =  new List<List<ZoneRingPoint>>();
			
			for(int i=0;i<files.Length; i++)
			{
				rings.Add(loadGeojson(files[i]));
				areas[i]=getArea(rings[i]);
				
			}
			
			/* This function takes last element as pivot, places
			   the pivot element at its correct position in sorted
				array, and places all smaller (smaller than pivot)
			   to left of pivot and all greater elements to right
			   of pivot */
			int partition (string[] files, List<List<ZoneRingPoint>> rings, double[] areas, int low, int high)
			{
				// pivot (Element to be placed at right position)
				string buffer1;
				double buffer2;
				double pivot = getArea(rings[high]);  
				List<ZoneRingPoint> buffer3;
				int i = (low - 1);  // Index of smaller element and indicates the 
							   // right position of pivot found so far

				for (int j = low; j <= high-1; j++)
				{
					// If current element is smaller than the pivot
					if (getArea(rings[j]) < pivot)
					{
						i++;  						// increment index of smaller element
						// swap arr[i] and arr[j]
						buffer1 = files[i];
						files[i] = files[j];
						files[j] = buffer1;
						
						buffer2 = areas[i];
						areas[i] = areas[j];
						areas[j] = buffer2;
						
						
						buffer3 = rings[i];
						rings[i] = rings[j];
						rings[j] = buffer3;
						
					}
					
				}
				//swap arr[i + 1] and arr[high])
				buffer1 = files[i+1];
				files[i+1] = files[high];
				files[high] = buffer1;
				
				buffer2 = areas[i+1];
				areas[i+1] = areas[high];
				areas[high] = buffer2;
					
				buffer3 =rings[i+1];
				rings[i+1] = rings[high];
				rings[high] = buffer3;
				rings[high] = buffer3;
				return (i + 1);
			}
			
			/* low  --> Starting index,  high  --> Ending index */
			void quickSort(string[] files, List<List<ZoneRingPoint>> rings, double[] areas, int low,int high)
			{
				if (low < high)
				{
					/* pi is partitioning index, arr[pi] is now
					   at right place */
					int pi = partition(files, rings, areas, low, high);

					quickSort(files, rings, areas, low, pi - 1);  // Before pi
					quickSort(files, rings, areas, pi + 1, high); // After pi
				}
			}
			
			quickSort(files, rings, areas, low, high);
			return files;
			
		}
		
		static void addPoint(List<ZoneRingPoint> newShape, ZoneRingPoint p)
		{
			// Adds a given point to the new shape.
			newShape.Add(new ZoneRingPoint(p.x, p.y));
			return;
		}

		static List<ZoneRingPoint> getInvalidPoints(List<ZoneRingPoint>[] stretchedShapes, List<ZoneRingPoint>[] shapes, List<ZoneRingPoint> invalidPoints)
		{
			int n; // Counter for iterating through shapes.
			Console.WriteLine(stretchedShapes[1]);
			int lengthA = stretchedShapes[0].Count;
			int lengthB = stretchedShapes[1].Count;
			for(int i=0;i<2;i++)
			{
				// Check each point on both shapes. Mark the point as invalid if:
				// a) the point is inside the other shape, or
				// b) both neighboring points are inside the other shape.
				
				if(i==0)
				{
					n = 0;
					foreach (ZoneRingPoint p in stretchedShapes[i])
					{
						if(isInside(stretchedShapes[i+1], stretchedShapes[i+1].Count, p) || (isInside(stretchedShapes[i+1], stretchedShapes[i+1].Count-1, stretchedShapes[i][n<lengthA-1?n+1:0]) && isInside(stretchedShapes[i+1], stretchedShapes[i+1].Count, stretchedShapes[i][n>0?n-1:lengthA-1])))
						{
							if (!invalidPoints.Contains(shapes[i][n]))
							{
								invalidPoints.Add(shapes[i][n]);
							}
						}
						if (n < stretchedShapes[i].Count-2)
						{
							n++;
						} else
						{
							n = 0;
						}
					}
				}
				if(i==1)
				{
					n = 0;
					foreach (ZoneRingPoint p in stretchedShapes[i])
					{
						if(isInside(stretchedShapes[i-1], stretchedShapes[i-1].Count, p) || (isInside(stretchedShapes[i-1], stretchedShapes[i-1].Count, stretchedShapes[i][n<lengthB-1?n+1:0]) && isInside(stretchedShapes[i-1], stretchedShapes[i-1].Count, stretchedShapes[i][n>0?n-1:lengthB-1])))
						{
							if (!invalidPoints.Contains(shapes[i][n]))
							{
								Console.WriteLine("line 386: found a point on shape B inside shape A.");
								invalidPoints.Add(shapes[i][n]);
							}
						}
						if (n < stretchedShapes[i].Count-2)
						{
							n++;
						} else
						{
							n = 0;
						}
							
					}
				}
			}
			Console.WriteLine("Line 390. number of invalid points:");
			Console.WriteLine(invalidPoints.Count);
			return invalidPoints;
		}

		static bool Combine(string file1, string file2)
		{
			int firstIndex, counter;
			int SHAPE_A = 0;
			int SHAPE_B = 1;
			int currentShape = SHAPE_A;
			int otherShape = SHAPE_B;
			List<ZoneRingPoint> newShape = new List<ZoneRingPoint>();
			List<ZoneRingPoint> invalidPoints = new List<ZoneRingPoint>();
			
			// Load file1 and file2 into an array. SHAPE_A is index 0 and SHAPE_B is index 1.
			List<ZoneRingPoint> [] shapes = {loadGeojson(file1), loadGeojson(file2)};
			
			// Load the enlarged shapes into their own arrays as well with the same indexing scheme (shape a is 0, shape b is 1).
			ZoneRingPoint centroidAB = getCentroid(shapes[SHAPE_A], shapes[SHAPE_B]);
			
			// first one is stretched slightly from the combined centroid. The second one is stretched from the centers independently. Also look at only horizontally stretched and vertically stretched. This is to (help) make sure no invalid points are overlooked because there are so many different situations and it only takes one invalid point to throw off the algorithm.
			List<ZoneRingPoint> [] stretchedShapes1 = {stretchPolygon(shapes[SHAPE_A], centroidAB), stretchPolygon(shapes[SHAPE_B], centroidAB)};
			List<ZoneRingPoint> [] stretchedShapes2 = {stretchPolygon(shapes[SHAPE_A]), stretchPolygon(shapes[SHAPE_B])};
			List<ZoneRingPoint> [] stretchedShapesVertical = {stretchPolygonVertical(shapes[SHAPE_A]), stretchPolygonVertical(shapes[SHAPE_B])};
			List<ZoneRingPoint> [] stretchedShapesHorizontal = {stretchPolygonHorizontal(shapes[SHAPE_A]), stretchPolygonHorizontal(shapes[SHAPE_B])};
			
			
			// Store the number of points in each shape.
			int [] length = {shapes[SHAPE_A].Count, shapes[SHAPE_B].Count};
			
			
			if (!canCombine(stretchedShapes2[SHAPE_A], stretchedShapes2[SHAPE_B]) && !canCombine(stretchedShapes1[SHAPE_A], stretchedShapes1[SHAPE_B]))
			{
				// Check if the shapes can be combined... Basically just checks to make sure they are touching.
				Console.WriteLine("Could not combine " + file1 + " and " + file2);
				return false;
			}
			else
			{	
				string filename = "files\\" + Path.GetFileNameWithoutExtension(file1) + Path.GetFileNameWithoutExtension(file2);
				Console.WriteLine(filename);
				ZoneRingPoint p, p0, sp; //Current point, previous point, and stretched current point.
				// Gather all the "invalid" points (ones that cannot appear in the new shape) by taking 4 separate transformations on the original rings and seeing which points overlap into the other shape.
				invalidPoints = getInvalidPoints(stretchedShapes1, shapes, invalidPoints);
				invalidPoints = getInvalidPoints(stretchedShapes2, shapes, invalidPoints);
				invalidPoints = getInvalidPoints(stretchedShapesVertical, shapes, invalidPoints);
				invalidPoints = getInvalidPoints(stretchedShapesHorizontal, shapes, invalidPoints);
				
				if (invalidPoints.Count <= 3)
				{
					// Only combine shapes with more than 3 invalid points. This helps reduce the chance of infinite loops or incorrect shapes.
					Console.WriteLine("Not enough invalid points to combine.");
					return false;
				}
				firstIndex = getFirstIndex(stretchedShapes1[SHAPE_A], stretchedShapes1[SHAPE_B], invalidPoints);
				Console.WriteLine(firstIndex);
				ZoneRingPoint firstPoint = shapes[currentShape][firstIndex];
				bool isEnd;
				Console.WriteLine("Can combine " + file1 + " and " + file2 + ". Starting index :");
				Console.WriteLine(firstIndex);
				// Add first point 
				addPoint(newShape, shapes[currentShape][firstIndex]);
				
				Console.WriteLine("shape A length: ");
				Console.WriteLine(length[SHAPE_A]);
				
				// This for loop starts at firstIndex+1 on Shape A, and once the counter == firstIndex it goes through one last iteration. It also loops back to the first point of the "current" ring once it has reached the end so the counter never goes out of bounds.
				for(counter = firstIndex+1; (counter != firstIndex || currentShape != SHAPE_A); counter += (counter < length[currentShape]-1) ? 1 : (length[currentShape]-1)*-1)
				{
					// There are still sometimes situations where we get stuck between two points so if the array is longer than it should be, just fail over to prevent an infinite loop.
					if (newShape.Count > length[SHAPE_A]+length[SHAPE_B])
					{
						Console.WriteLine("Could not combine shape " + file1 + " with shape " + file2 + ".");
						return false;
					}
					// first check to see if the point is "valid" or not i.e. eligible to be added to shape C.
					// Add first point 
					isEnd = (counter == firstIndex && currentShape == SHAPE_A);
					p = shapes[currentShape][counter];
					p0 = shapes[currentShape][counter-1 >= 0?counter-1:length[currentShape]-1];
					sp = stretchedShapes2[currentShape][counter];
					
					if (newShape.Contains(p))
					{
						// If the new shape already contains the current point, do nothing with it and continue to next point.
						// The new shape should only contain one of each point (with the exception of the first and last points)
						continue;
					}
					if (invalidPoints.Contains(p))
					{
						// We have reached an invalid point... So now we move the counter to the point on the "other" shape that is:
						// a) nearest to both the current and previous points, and
						// b) not an invalid point.
						counter = findNearestPoint(p, p0, shapes[otherShape], invalidPoints)-1;
						// Swap "current shape" and "other shape" and continue to next iteration.
						if (currentShape == SHAPE_A)
						{
							currentShape = SHAPE_B;
							otherShape = SHAPE_A;
							continue;
						} else
						{
							currentShape = SHAPE_A;
							otherShape = SHAPE_B;
						}
						continue;
					}
					else
					{
						addPoint(newShape, shapes[currentShape][counter]);

					}
				}
				
				isEnd = (counter == firstIndex && currentShape == SHAPE_A);
				if (isEnd)
					{
						// Add the final point, which is the same as the first point.
						addPoint(newShape, shapes[currentShape][counter]);
					}
				if (newShape.Count < length[SHAPE_A]/2 || newShape.Count < length[SHAPE_B]/2)
				{
					// One last error check... If we've circled back around to the beginning and the polygon is suspiciously smaller than the originals, count it as a fail.
					Console.WriteLine("Failed to combine " + file1 + " and " + file2);
					return false;
				}
				else
				{
					// Save new polygon, delete the originals, and return true to indicate success.
					saveGeojson(newShape, filename);
					removeFiles(file1, file2);
					return true;
				}
			}
	

		}

		// This function recursively combines all files
		
		static FilePair filePair;
		static public List<FilePair> noMatch = new List<FilePair>();
		static bool success;

		public static void combineRings()
		{
			// Recursively checks the files folder for rings that can be combined and combines them.
			string[] files = getFiles();
			foreach(string f in files)
			{
				Console.WriteLine(f);
			}
			
			if (files.Length == 1)
			{
				return;
			}
			
			foreach (string file1 in files)
			{
				foreach(string file2 in files)
				{
					filePair = new FilePair(file1, file2);
					
					if (files.Length == 1)
					{
						return;
					}
					if(file1 == file2) 
					{
						Console.WriteLine(file1 + " and " + file2 + " are equal.");
						continue;
					}
					else if (noMatch.Contains(filePair))
					{
						Console.WriteLine(file1 + " and " + file2 + " are not touching.");
						continue;
					}
					else
					{
						Console.WriteLine("Combining " + file1 + " and " + file2);
						success = Combine(file1, file2);
						if (success)
						{
							Console.WriteLine(file1 + " and " + file2 + " combined successfully.");
							//files = getFiles();
							combineRings();
							
						}
						else
						{
							Console.WriteLine(file1 + " and " + file2 + " cannot be joined.");	
							noMatch.Add(new FilePair(file1, file2));
							success = Combine(file2, file1);
							if (success)
							{
								Console.WriteLine(file2 + " and " + file1 + " combined successfully.");
								//files = getFiles();
								combineRings();
							}
							else
							{
								Console.WriteLine(file2 + " and " + file1 + " cannot be joined.");	
								noMatch.Add(new FilePair(file2, file1));
								if(files.Length == 2)
								{
									return;
								}
								else
								{
									continue;
								}
							}
						}
						
					}					
				}
			}
		}

		public static void Main(string[] args)
		{	
			combineRings();	
		}
	}
}