I dette projekt (GeometryViewer3) er der følgende forbedringer i forhold til GeometryViewer2:

* WorldWindow size følger Viewport size, når man resizer vinduet
* Cursor position vises i gui'en, og når musen forlader viewet, vises den ikke. Bemærk i den forbindelse,
  hvordan der både er en ~"dependency property" (CursorWorldPositionProperty og CursorWorldPosition) på GeometryCanvas
  og så en property med et backing field og som rejser OnPropertyChanged events (CursorWorldPosition) på viewmodelniveau.
* Support for panning ved venstreklik og drag