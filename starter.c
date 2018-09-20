#include <stdio.h>
#include <stdlib.h> 
#include <stddef.h>
#include <unistd.h>
#include <sys/wait.h>
#include <stdbool.h>

int main()
{
    while (true)
    {
        printf("Starting web servr\r\n");
        int status;
        switch(fork()) {
            case -1: 
                printf("Error in fork\r\n");
                break;
            case  0:   // Child process
                execl("Server", "Server", "", (char*)NULL);
                //execl("crasher", "crasher", "", (char*)NULL);
                break;
            default:   // parent process waiting on child to stop
                wait(&status);
                break;
        }
    }
}