I dette projekt (GeometryViewer3) er der følgende forbedringer i forhold til GeometryViewer2:

* WorldWindow size følger Viewport size
* Cursor (seneste, dvs også efter at man har forladt viewporten) position vises i gui'en. Bemærk i den forbindelse,
  hvordan der både er en ~"dependency property" (CursorWorldPositionProperty og CursorWorldPosition) på GeometryCanvas
  og så en property med et backing field og som rejser OnPropertyChanged events (CursorWorldPosition) på viewmodelniveau.
* Support for panning ved venstreklik og drag