/// Rounds up a specified number to the next multiple of X.
///
/// # Arguments
///
/// * `number` - The number to round up.
/// * `multiple` - The multiple the number should be rounded to.
pub fn round_up<T: Into<usize>>(number: usize, multiple: T) -> usize {
    let multiple = multiple.into();
    if multiple == 0 {
        return number;
    }

    let remainder = number % multiple;
    if remainder == 0 {
        return number;
    }

    number + multiple - remainder
}

/// Rounds down a specified number to the previous multiple of X.
///
/// # Arguments
///
/// * `number` - The number to round down.
/// * `multiple` - The multiple the number should be rounded to.
pub fn round_down<T: Into<usize>>(number: usize, multiple: T) -> usize {
    let multiple = multiple.into();
    if multiple == 0 {
        return number;
    }

    let remainder = number % multiple;
    if remainder == 0 {
        return number;
    }

    number - remainder
}

/// Returns smaller of the two values.
///
/// # Arguments
///
/// * `a` - First value.
/// * `b` - Second value.
#[allow(dead_code)]
pub fn min(a: usize, b: usize) -> usize {
    a.min(b)
}

/// Adds the two values, but caps the result at MaxValue if it overflows.
///
/// # Arguments
///
/// * `a` - First value.
/// * `b` - Second value.
pub fn add_with_overflow_cap(a: usize, b: usize) -> usize {
    a.saturating_add(b)
}

/// Subtracts the two values, but caps the result at MinValue if it overflows.
///
/// # Arguments
///
/// * `a` - First value.
/// * `b` - Second value.
pub fn subtract_with_underflow_cap(a: usize, b: usize) -> usize {
    a.saturating_sub(b)
}
