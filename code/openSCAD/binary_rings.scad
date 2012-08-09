module binary_ring(radius, bits, ridge_width, height) {
  for(i=[0:2:pow(2,bits)]) {
    union() {
      for(angle=[ i*360/pow(2,bits) : (i+1)*360/pow(2,bits)] ) {
        translate([radius*cos(angle), radius*sin(angle)])
          rotate(angle)
          cube(size=[ridge_width,1,height]);
      }
    }
  }
}

large_radius = 50;
inner_hole = 5;
ridge_width = 10;
height = 10;

difference() {
  union() {
    cylinder(r=large_radius+10, h=height/2);
    for(i=[0:2]) {
      binary_ring(radius=large_radius-20*i,
                  bits=3-i,
                  ridge_width=ridge_width,
                  height=height);
    }
  }
  cylinder(r=inner_hole, h=height, center=true);
}

translate([0,0,30])
  union() {
    cylinder(r=inner_hole-1, h=height+inner_hole*3);
    translate([0,-inner_hole,height+inner_hole])
      cube(size=[large_radius+ridge_width, inner_hole*2, inner_hole*2]);
  }
