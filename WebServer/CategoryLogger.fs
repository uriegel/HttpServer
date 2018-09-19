namespace WebServer
open Microsoft.Extensions.Logging

type CategoryLogger = {
    log: LogLevel->string->unit
    lowTrace: (unit->string)->unit
}
