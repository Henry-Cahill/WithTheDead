/// Scripts that exercise the Java host for the editor's self-test (Assets/Editor/JavaBuild.cs, CloneZ > Java > Self
/// Test). Never part of a play session or a build.
module withthedead.selftest {
    requires withthedead.api;

    provides withthedead.api.GameScript with withthedead.selftest.Probe, withthedead.selftest.Faulty;
}
