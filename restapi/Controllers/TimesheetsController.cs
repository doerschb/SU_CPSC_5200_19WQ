using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using restapi.Models;

namespace restapi.Controllers
{
    [Route("[controller]")]
    public class TimesheetsController : Controller
    {
        [HttpGet]
        [Produces(ContentTypes.Timesheets)]
        [ProducesResponseType(typeof(IEnumerable<Timecard>), 200)]
        public IEnumerable<Timecard> GetAll()
        {
            return Database
                .All
                .OrderBy(t => t.Opened);
        }

        [HttpGet("{id}")]
        [Produces(ContentTypes.Timesheet)]
        [ProducesResponseType(typeof(Timecard), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetOne(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null) 
            {
                return Ok(timecard);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Produces(ContentTypes.Timesheet)]
        [ProducesResponseType(typeof(Timecard), 200)]
        public Timecard Create([FromBody] DocumentResource resource)
        {
            var timecard = new Timecard(resource.Resource);

            var entered = new Entered() { Resource = resource.Resource };

            timecard.Transitions.Add(new Transition(entered));

            Database.Add(timecard);

            return timecard;
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        public IActionResult DeleteLine(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard == null)
            {
                return NotFound();
            }

            if (timecard.Status != TimecardStatus.Cancelled && timecard.Status != TimecardStatus.Draft)
            {
                    return StatusCode(409, new InvalidStateError() { });
            }

            Database.Delete(id);
            return Ok();
        }

        [HttpGet("{id}/lines")]
        [Produces(ContentTypes.TimesheetLines)]
        [ProducesResponseType(typeof(IEnumerable<AnnotatedTimecardLine>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetLines(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                var lines = timecard.Lines
                    .OrderBy(l => l.WorkDate)
                    .ThenBy(l => l.Recorded);

                return Ok(lines);
            }
            else
            {
                return NotFound();
            }
        }
        //lineId must be an int in the URL
        [HttpGet("{timecardId}/lines/{lineId}")]
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(Timecard), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetOne(string timecardId, string lineId)
        {
            Timecard timecard = Database.Find(timecardId);

            if (timecard != null) 
            { 
                var match = timecard.Lines.FirstOrDefault(x => x.LineNumber.ToString() == lineId);
                if (match == null)
                    return NotFound();
                
                return Ok(match);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{id}/lines")] //could also be PUT
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(AnnotatedTimecardLine), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        public IActionResult AddLine(string id, [FromBody] TimecardLine timecardLine)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                var annotatedLine = timecard.AddLine(timecardLine);

                return Ok(annotatedLine);
            }
            else
            {
                return NotFound();
            }
        }

        //lineNumber is the lineId for now. (GUID can be used later)
        //lineId must be an integer for the conversion, to string properties to hold.
        [HttpPatch("{timecardId}/lines/{lineId}")]       
        [Produces(ContentTypes.TimesheetLine)]        
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        [ProducesResponseType(typeof(UpdateError), 409)]
        [ProducesResponseType(typeof(MissingLineError), 409)]
        public IActionResult UpdateLine(string timecardId, string lineId, [FromBody] TimecardLine timecardLine)
        {
            Timecard timecard = Database.Find(timecardId);      

            if (timecard == null)
            {
                return NotFound();            
            }
            else if (timecard.Status != TimecardStatus.Draft) {
                return StatusCode(409, new UpdateError() { });
            }
            else 
            {
                //find the line that matches lineId
                var match = timecard.Lines.FirstOrDefault(x => x.LineNumber.ToString() == lineId);
                if(match != null)
                {
                    timecard.UpdateLine(timecard.Lines.IndexOf(match), timecardLine);
                }
                else
                {
                    return StatusCode(409, new MissingLineError() { });
                }                   
                //FIXME: UpdateLine updates all fields to match those passed into the request, so later versions should
                //          allow for some fields to be left blank. null cases are also not accounted for in this version.
                //          The likely next iteration might include some kind of overloading and/or checking each field
                //          in timecardLine, and flagging (with a -1, perhaps) and checking for that flag upon setting, 
                //          where nulls would be flagged as -1 as well, so as not to be updated. Removal of information
                //          is not possible in this scheme.
            }

            if (timecard.Lines.Count < 1)
            {
                return StatusCode(409, new EmptyTimecardError() { });
            }

            return Ok();
        }

        //lineNumber is the lineId for now. (GUID can be used later)
        //lineId must be an integer for the conversion, to string properties to hold.
        [HttpPost("{timecardId}/lines/{lineId}")]        
        [Produces(ContentTypes.TimesheetLine)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        [ProducesResponseType(typeof(UpdateError), 409)]
        [ProducesResponseType(typeof(MissingLineError), 409)]
        public IActionResult ReplaceLine(string timecardId, string lineId, [FromBody] TimecardLine timecardLine)
        {
            Timecard timecard = Database.Find(timecardId);      

            if (timecard == null)
            {
                return NotFound();            
            }
            else if (timecard.Status != TimecardStatus.Draft) {
                return StatusCode(409, new UpdateError() { });
            }
            else 
            {
                //find the line that matches lineId
                var match = timecard.Lines.FirstOrDefault(x => x.LineNumber.ToString() == lineId);
                if(match != null)   {
                    int index = (timecard.Lines.IndexOf(match));
                    timecard.ReplaceLine(index, timecardLine);
                }
                 else
                {
                    return StatusCode(409, new MissingLineError() { });
                }  

               // var annotatedLine = timecard.(timecardLine);
            }

            if (timecard.Lines.Count < 1)
            {
                return StatusCode(409, new EmptyTimecardError() { });
            }

            return Ok();
        }

        [HttpGet("{id}/transitions")]
        [Produces(ContentTypes.Transitions)]
        [ProducesResponseType(typeof(IEnumerable<Transition>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetTransitions(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                return Ok(timecard.Transitions);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost("{id}/submittal")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        [ProducesResponseType(typeof(InvalidResource), 409)]
        public IActionResult Submit(string id, [FromBody] DocumentResource resource)//resource must be same
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                
                if (resource.Resource == null || timecard.Resource != resource.Resource){
                    return StatusCode(409, new InvalidResource(){});
                }
                if (timecard.Status != TimecardStatus.Draft)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }

                if (timecard.Lines.Count < 1)
                {
                    return StatusCode(409, new EmptyTimecardError() { });
                }
                var submittal = new Submittal(){Resource = resource.Resource};
                var transition = new Transition(submittal, TimecardStatus.Submitted);
                timecard.Transitions.Add(transition);
                return Ok(transition);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/submittal")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetSubmittal(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Submitted)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Submitted)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);                                        
                }
                else 
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }
        //needs reason
        [HttpPost("{id}/cancellation")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        [ProducesResponseType(typeof(InvalidResource), 409)]
        public IActionResult Cancel(string id, [FromBody] Cancellation cancellation)//resource should be specified
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status != TimecardStatus.Draft && timecard.Status != TimecardStatus.Submitted)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }
                if (cancellation.Resource ==null)//can be same or different person
                {
                    return StatusCode(408, new InvalidResource(){});
                }
                if (timecard.Lines.Count < 1)
                {
                    return StatusCode(409, new EmptyTimecardError() { });
                }
                var transition = new Transition(cancellation, TimecardStatus.Cancelled);
                timecard.Transitions.Add(transition);
                return Ok(transition);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/cancellation")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetCancellation(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Cancelled)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Cancelled)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);                                        
                }
                else 
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }
        //needs reason; contract for now to put in reason + resource
        [HttpPost("{id}/rejection")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
         [ProducesResponseType(typeof(InvalidResource), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        public IActionResult Close(string id, [FromBody] Rejection rejection)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                 if (rejection.Resource == null || timecard.Resource == rejection.Resource){//resource should be different
                    return StatusCode(409, new InvalidResource(){});
                }
                if (timecard.Status != TimecardStatus.Submitted)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }
                if (timecard.Lines.Count < 1)
                {
                    return StatusCode(409, new EmptyTimecardError() { });
                }
                var transition = new Transition(rejection, TimecardStatus.Rejected);
                timecard.Transitions.Add(transition);
                return Ok(transition);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/rejection")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetRejection(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Rejected)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Rejected)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);                                        
                }
                else 
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }
        
        [HttpPost("{id}/approval")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(InvalidStateError), 409)]
        [ProducesResponseType(typeof(EmptyTimecardError), 409)]
        [ProducesResponseType(typeof(InvalidResource), 409)]
        public IActionResult Approve(string id, [FromBody] Approval approval)//resource should be different
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                 if (approval.Resource == null || timecard.Resource == approval.Resource){
                    return StatusCode(409, new InvalidResource(){});
                }
                if (timecard.Status != TimecardStatus.Submitted)
                {
                    return StatusCode(409, new InvalidStateError() { });
                }
                var transition = new Transition(approval, TimecardStatus.Approved);
                timecard.Transitions.Add(transition);
                return Ok(transition);
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/approval")]
        [Produces(ContentTypes.Transition)]
        [ProducesResponseType(typeof(Transition), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(typeof(MissingTransitionError), 409)]
        public IActionResult GetApproval(string id)
        {
            Timecard timecard = Database.Find(id);

            if (timecard != null)
            {
                if (timecard.Status == TimecardStatus.Approved)
                {
                    var transition = timecard.Transitions
                                        .Where(t => t.TransitionedTo == TimecardStatus.Approved)
                                        .OrderByDescending(t => t.OccurredAt)
                                        .FirstOrDefault();

                    return Ok(transition);                                        
                }
                else 
                {
                    return StatusCode(409, new MissingTransitionError() { });
                }
            }
            else
            {
                return NotFound();
            }
        }        
    }
}
