#include <stdio.h>
#include <stdlib.h>
#include <unistd.h> /* for fork */
#include <sys/types.h> /* for pid_t */
#include <sys/wait.h> /* for wait */

void Screenshot() {

    pid_t pid=fork();

    if (pid==0) { 
        static char *argv[]={"screencapture","-t","jpg", "-x", "tmp.jpg", "-r",NULL};
        execv("/usr/sbin/screencapture",argv);
        exit(127); 
    }
    else { 
        waitpid(pid,0,0);
    }
    
}
