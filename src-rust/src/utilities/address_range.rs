/// Defines a physical address range with a minimum and maximum address.
pub struct AddressRange {
    pub start_pointer: usize,
    pub end_pointer: usize,
}

impl AddressRange {
    /// Constructs a new AddressRange.
    ///
    /// # Arguments
    ///
    /// * `start_pointer` - The starting address of the range.
    /// * `end_pointer` - The ending address of the range.
    pub fn new(start_pointer: usize, end_pointer: usize) -> Self {
        Self {
            start_pointer,
            end_pointer,
        }
    }

    /// Calculates the size of the address range.
    ///
    /// # Returns
    ///
    /// * The size of the range, calculated as the difference between the end pointer and the start pointer.
    pub fn size(&self) -> usize {
        self.end_pointer - self.start_pointer
    }

    /// Checks if the other address range is completely inside this address range.
    ///
    /// # Arguments
    ///
    /// * `other` - The other address range to check against.
    ///
    /// # Returns
    ///
    /// * `true` if the other address range is contained entirely inside this one, `false` otherwise.
    pub fn contains(&self, other: &AddressRange) -> bool {
        other.start_pointer >= self.start_pointer && other.end_pointer <= self.end_pointer
    }

    /// Checks if the other address range intersects this address range, i.e. start or end of this range falls inside other range.
    ///
    /// # Arguments
    ///
    /// * `other` - The other address range to check for overlap.
    ///
    /// # Returns
    ///
    /// * `true` if there are any overlaps in the address ranges, `false` otherwise.
    pub fn overlaps(&self, other: &AddressRange) -> bool {
        self.point_in_range(other.start_pointer)
            || self.point_in_range(other.end_pointer)
            || other.point_in_range(self.start_pointer)
            || other.point_in_range(self.end_pointer)
    }

    /// Checks if a number "point", is between min and max of this address range.
    ///
    /// # Arguments
    ///
    /// * `point` - The point to test.
    ///
    /// # Returns
    ///
    /// * `true` if the point is within this address range, `false` otherwise.
    fn point_in_range(&self, point: usize) -> bool {
        point >= self.start_pointer && point <= self.end_pointer
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn contains_should_be_true_when_other_range_is_inside() {
        let range = AddressRange::new(100, 200);
        let other_range = AddressRange::new(120, 180);

        assert!(range.contains(&other_range));
    }

    #[test]
    fn contains_should_be_false_when_other_range_is_not_inside() {
        let range = AddressRange::new(100, 200);
        let other_range = AddressRange::new(80, 220);

        assert!(!range.contains(&other_range));
    }

    #[test]
    fn overlaps_should_be_true_when_other_range_overlaps() {
        let range = AddressRange::new(100, 200);
        let other_range = AddressRange::new(150, 220);

        assert!(range.overlaps(&other_range));
    }

    #[test]
    fn overlaps_should_be_false_when_other_range_does_not_overlap() {
        let range = AddressRange::new(100, 200);
        let other_range = AddressRange::new(300, 400);

        assert!(!range.overlaps(&other_range));
    }

    #[test]
    fn overlaps_should_be_true_when_ranges_are_same() {
        let range = AddressRange::new(100, 200);
        let other_range = AddressRange::new(100, 200);

        assert!(range.overlaps(&other_range));
    }

    #[test]
    fn overlaps_should_be_true_when_one_range_is_fully_inside_other() {
        let range = AddressRange::new(100, 200);
        let other_range = AddressRange::new(120, 180);

        assert!(range.overlaps(&other_range));
    }

    #[test]
    fn overlaps_should_be_true_when_ranges_are_adjacent() {
        let range = AddressRange::new(100, 200);
        let other_range = AddressRange::new(200, 300);

        assert!(range.overlaps(&other_range));
    }

    #[test]
    fn overlaps_should_be_true_when_other_range_starts_inside_range() {
        let range = AddressRange::new(100, 200);
        let other_range = AddressRange::new(150, 250);

        assert!(range.overlaps(&other_range));
    }

    #[test]
    fn overlaps_should_be_true_when_other_range_ends_inside_range() {
        let range = AddressRange::new(100, 200);
        let other_range = AddressRange::new(50, 150);

        assert!(range.overlaps(&other_range));
    }
}
