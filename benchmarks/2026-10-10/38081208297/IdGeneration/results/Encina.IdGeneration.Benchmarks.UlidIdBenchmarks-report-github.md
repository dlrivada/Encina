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
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,201.4 ns |  24.01 ns | 15.88 ns |  1.24 |    0.02 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   960.1 ns |   5.05 ns |  3.00 ns |  0.99 |    0.00 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   971.6 ns |   2.59 ns |  1.72 ns |  1.00 |    0.00 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |         |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,193.7 ns | 207.92 ns | 11.40 ns |  1.23 |    0.01 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   965.4 ns |  30.65 ns |  1.68 ns |  1.00 |    0.00 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   967.6 ns |  51.22 ns |  2.81 ns |  1.00 |    0.00 |      40 B |        1.00 |
