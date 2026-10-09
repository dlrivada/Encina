```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,111.1 ns |  19.79 ns | 13.09 ns |  1.22 |    0.02 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   907.1 ns |  11.83 ns |  7.83 ns |  1.00 |    0.01 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   907.8 ns |  17.50 ns | 11.58 ns |  1.00 |    0.02 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |         |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,109.2 ns |  84.84 ns |  4.65 ns |  1.24 |    0.02 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   905.2 ns | 104.21 ns |  5.71 ns |  1.01 |    0.02 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   892.4 ns | 351.71 ns | 19.28 ns |  1.00 |    0.03 |      40 B |        1.00 |
