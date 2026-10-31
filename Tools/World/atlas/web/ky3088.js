// EPSG:3088 (NAD83 / Kentucky Single Zone): the world store's grid, a Lambert conformal conic on GRS80 in metres east and
// north, exactly as Tools/World/worldbuild.py's KentuckySingleZone projects it; here both ways, for the atlas page and
// (in node) its tests. OpenStreetMap's and the imagery's WGS84 go in and out as NAD83: the two differ by about a metre.
(function (root) {
  "use strict";
  const A = 6378137.0, F = 1 / 298.257222101, E = Math.sqrt(2 * F - F * F), RAD = Math.PI / 180;
  const LAT1 = (37 + 5 / 60) * RAD, LAT2 = (38 + 40 / 60) * RAD, LAT0 = (36 + 20 / 60) * RAD;
  const LON0 = -(85 + 45 / 60) * RAD, X0 = 1500000, Y0 = 1000000;
  const m = (lat) => Math.cos(lat) / Math.sqrt(1 - Math.pow(E * Math.sin(lat), 2));
  const t = (lat) => {
    const es = E * Math.sin(lat);
    return Math.tan(Math.PI / 4 - lat / 2) / Math.pow((1 - es) / (1 + es), E / 2);
  };
  const N = (Math.log(m(LAT1)) - Math.log(m(LAT2))) / (Math.log(t(LAT1)) - Math.log(t(LAT2)));
  const AF = (A * m(LAT1)) / (N * Math.pow(t(LAT1), N));
  const RHO0 = AF * Math.pow(t(LAT0), N);

  /** Grid metres [east, north] for a longitude and latitude in degrees. */
  function toGrid(lon, lat) {
    const rho = AF * Math.pow(t(lat * RAD), N), theta = N * (lon * RAD - LON0);
    return [X0 + rho * Math.sin(theta), Y0 + RHO0 - rho * Math.cos(theta)];
  }

  /** [longitude, latitude] in degrees for grid metres east and north. */
  function toLonLat(x, y) {
    const dx = x - X0, dy = RHO0 - (y - Y0);
    const tt = Math.pow(Math.hypot(dx, dy) / AF, 1 / N);
    let lat = Math.PI / 2 - 2 * Math.atan(tt);
    for (let i = 0; i < 15; i++) {
      const es = E * Math.sin(lat);
      const next = Math.PI / 2 - 2 * Math.atan(tt * Math.pow((1 - es) / (1 + es), E / 2));
      const done = Math.abs(next - lat) < 1e-13;
      lat = next;
      if (done) break;
    }
    return [(Math.atan2(dx, dy) / N + LON0) / RAD, lat / RAD];
  }

  const api = { toGrid, toLonLat };
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  else root.Ky3088 = api;
})(this);
