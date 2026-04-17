I dette projekt (GeometryViewer3B) er princippet fra GeometryViewer1 brugt til at lave en general purpose GeometryCanvas,
som har de 2 properties Lines og WorldWindow, der bindes til en ViewModel. Her holdes worldwindow konstant, og viewet
vedligeholder en transformation, der mapper det ind i den viewport, der gør sig gældende, dvs når man resizer vinduet,
så strækkes linierne.