```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,169.9 ns |   4.74 ns | 2.82 ns |  1.20 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   955.3 ns |   8.21 ns | 4.89 ns |  0.98 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   975.6 ns |   2.83 ns | 1.87 ns |  1.00 |      40 B |        1.00 |
|                   |            |                |             |            |           |         |       |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,176.3 ns | 148.03 ns | 8.11 ns |  1.22 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   950.3 ns |  48.83 ns | 2.68 ns |  0.99 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   960.7 ns |  56.95 ns | 3.12 ns |  1.00 |      40 B |        1.00 |
