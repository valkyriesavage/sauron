include <switch.scad>

module switch_ring(num_switches, inner_diameter, outer_diameter) {
  difference() {
    linear_extrude(height=switch_height)
      circle(r=outer_diameter/2);
    for(angle=[0 : (360/num_switches) : 360]) {
      translate([cos(angle) * (inner_diameter-switch_width)/2,
                 sin(angle) * (inner_diameter-switch_width)/2,
                 0]) {
        rotate(angle) {
          switch();
        }
      }
    }
  }
}

	switch_ring(8, 4*switch_width, 7*switch_width);
