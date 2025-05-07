using System;

namespace EasySave.Model.Enums
{
   
    public enum JobState
    {
        PENDING,  
        RUNNING,   
        PAUSED,    
        COMPLETED, 
        FAILED     // Job has failed
    }
}
