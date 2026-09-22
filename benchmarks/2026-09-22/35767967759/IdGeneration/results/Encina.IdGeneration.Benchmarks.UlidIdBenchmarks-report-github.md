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
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,002.7 ns |   5.01 ns |  3.31 ns |  1.23 |    0.01 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   804.4 ns |   6.75 ns |  4.47 ns |  0.99 |    0.01 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   815.1 ns |   5.84 ns |  3.05 ns |  1.00 |    0.00 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |         |           |             |
| Generate_ToString | ShortRun   | 3              | 1           |   999.2 ns | 115.73 ns |  6.34 ns |  1.20 |    0.02 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   800.3 ns |  32.24 ns |  1.77 ns |  0.96 |    0.01 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   831.9 ns | 237.27 ns | 13.01 ns |  1.00 |    0.02 |      40 B |        1.00 |
