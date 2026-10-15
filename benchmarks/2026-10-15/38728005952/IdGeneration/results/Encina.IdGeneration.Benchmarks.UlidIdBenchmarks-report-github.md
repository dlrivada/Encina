```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,193.9 ns |  24.00 ns | 15.88 ns |  1.23 |    0.02 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   959.9 ns |   3.33 ns |  1.98 ns |  0.99 |    0.00 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   973.1 ns |   3.67 ns |  2.43 ns |  1.00 |    0.00 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |         |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,188.8 ns |  96.65 ns |  5.30 ns |  1.21 |    0.01 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   969.3 ns | 276.60 ns | 15.16 ns |  0.99 |    0.01 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   978.5 ns |  80.66 ns |  4.42 ns |  1.00 |    0.01 |      40 B |        1.00 |
