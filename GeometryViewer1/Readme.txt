Jeg var i dialog med ChatGpt om hvordan man implementerer en geometry viewer, der understøtter panning og zooming,
og hvor linier tegnes med en tykkelse på én pixel uanset magnification. Efter at have eksperimenteret en del med
forskellige teknikker såsom følgende:

* Operere med en transformationsmatrix, der vedligeholdes i viewmodellen
* Operere med en value converter, der beregner en StrokeThickness ved at invertere scaling
* Konvertere world koordinater til view koordinater i viewmodellen, så viewet bare binder til viewport-koordinater

ChatGpt fik overbevist mig, om, at viewmodellen kun bør beskæftige sig med world koordinater, og at konvertering til
view koordinater bør foregå i view laget. Rent faktisk bør selve konceptet viewport kun være kendt i view laget.

Dette projekt (GeometryViewer1) demonstrerer princippet i, hvordan man i sin xaml-kode kan kan operere med en
specialisering af et FrameworkElement, som overrider metoden OnRender.