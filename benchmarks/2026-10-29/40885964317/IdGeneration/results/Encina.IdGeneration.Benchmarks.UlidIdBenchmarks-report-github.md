```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,087.0 ns |  25.59 ns | 15.23 ns |  1.18 |    0.02 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   879.2 ns |  14.19 ns |  9.38 ns |  0.96 |    0.02 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   919.0 ns |  22.15 ns | 14.65 ns |  1.00 |    0.02 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |         |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,093.1 ns | 309.12 ns | 16.94 ns |  1.29 |    0.02 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   836.7 ns |  19.93 ns |  1.09 ns |  0.99 |    0.00 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   844.8 ns |  59.50 ns |  3.26 ns |  1.00 |    0.00 |      40 B |        1.00 |
