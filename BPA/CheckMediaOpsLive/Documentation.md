# MediaOps.LIVE Configuration

## Best Practice

This BPA checks the configuration of the MediaOps.LIVE solution and collects performance statistics.

## Meta data

* Name: MediaOps.LIVE Configuration
* Description: Detects invalid configuration in the MediaOps.LIVE solution.
* Author: Skyline Communications
* Default schedule: Daily

## Results

### Success

No inconsistencies have been detected in the system.

Result message: `No incorrect configurations detected in the system.`

### Error

This BPA can detect the following configuration issues:

Endpoints
- Invalid transport type
- Invalid element
- Invalid control element
 
Virtual signal groups
- Invalid levels
- Invalid endpoints
 
Mediation elements
- Element has active alarms
 
Connection handler scripts
- A configured connection handler scripts doesn't exist (anymore)
- Scripts has syntax errors and/or cannot be compiled

### Warning

This BPA can detect the following potential configuration issues:

Endpoints
- Not assigned to a virtual signal group
 
Virtual signal groups
- Doesn't have any endpoints assigned
 
Connection handler scripts
- Not used by any mediated element
- Mediated element doesn't have a connection handler script assigned

### Statistics

This BPA also collects and reports the following statistics:
- Number of Levels
- Number of Transport Types
- Number of Source Endpoints
- Number of Destination Endpoints
- Number of Source Virtual Signal Groups
- Number of Destination Virtual Signal Groups
- Connection handler script executions (per script)
- Connection handler script failures (per script)
- Connection handler script last failure time (per script)

### Not Executed

These are the messages that can appear when the test fails to execute for unexpected reasons, and cannot provide a conclusive report because of this:

* "Could not execute test (*[message]*)." (on unexpected exceptions)

The test result details contain the full exception text, if available.

## Impact when issues detected

- Impact: Operation of the MediaOps.LIVE solution could be affected by this problem.
- Action: Please contact your system administrator for support.

## Limitations

* Requires the MediaOps.LIVE solution to be deployed.
