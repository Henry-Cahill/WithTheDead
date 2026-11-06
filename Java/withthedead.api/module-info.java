/// The API the game offers Java scripts. A script is a module that requires this one and provides a
/// [withthedead.api.GameScript]:
///
/// ```java
/// module my.mod {
///     requires withthedead.api;
///     provides withthedead.api.GameScript with my.mod.MyScript;
/// }
/// ```
module withthedead.api {
    exports withthedead.api;
}
