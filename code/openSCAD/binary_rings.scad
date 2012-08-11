module binary_ring(radius, bits, ridge_width, height) {
  for(i=[0:2:pow(2,bits)]) {
    union() {
      for(angle=[ i*360/pow(2,bits) : pow(2,-bits/3) : (i+1)*360/pow(2,bits)] ) {
        translate([radius*cos(angle), radius*sin(angle)])
          rotate(angle)
          cube(size=[ridge_width,1,height]);
      }
    }
  }
}

module measurement_arm(inner_hole, height, radius, bits, spacing) {
  union() {
    // the post to hold it together
    cylinder(r=inner_hole-1, h=height+inner_hole*6);
    // a skirt to keep it from falling down on all-zero reading
    translate([0,0,height/2])
      cylinder(r=inner_hole+1, h=1);
    translate([0,-inner_hole/2,height+inner_hole]) {
      cube(size=[large_radius+ridge_width, inner_hole, inner_hole*2]);
      // some ridges to guide wires
      for(i=[0:bits]) {
        translate([spacing*i,-inner_hole,0])
          cube([15,3*inner_hole,inner_hole*2]);
      }
    }
  }
}

total_bits = 6;
ridge_width = 15;
inner_hole = 5;
buffer = 5;
large_radius = total_bits*(ridge_width+buffer-1);
height = 10;

difference() {
  union() {
    color("Azure")
      cylinder(r=large_radius+ridge_width, h=height/2);
    for(i=[0:total_bits-1]) {
      color("DarkSlateGray")
        binary_ring(radius=large_radius-(ridge_width+buffer)*i,
                    bits=total_bits-i,
                    ridge_width=ridge_width,
                    height=height);
    }
  }
  cylinder(r=inner_hole, h=height, center=true);
}

translate([0,large_radius+total_bits*ridge_width,inner_hole])
  rotate(a=[90,0,0])
    color("MediumPurple")
      measurement_arm(inner_hole, height, radius, total_bits, ridge_width+buffer);
