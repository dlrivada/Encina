```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method            | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------ |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|----------:|------------:|
| Generate_ToString | Job-YFEFPZ | 10             | Default     | 1,220.7 ns |  20.30 ns | 12.08 ns |  1.22 |     120 B |        3.00 |
| NewUlid_Direct    | Job-YFEFPZ | 10             | Default     |   984.0 ns |   6.04 ns |  4.00 ns |  0.98 |      40 B |        1.00 |
| Generate          | Job-YFEFPZ | 10             | Default     |   999.4 ns |   5.01 ns |  3.31 ns |  1.00 |      40 B |        1.00 |
|                   |            |                |             |            |           |          |       |           |             |
| Generate_ToString | ShortRun   | 3              | 1           | 1,218.2 ns | 182.62 ns | 10.01 ns |  1.22 |     120 B |        3.00 |
| NewUlid_Direct    | ShortRun   | 3              | 1           |   985.5 ns |  34.97 ns |  1.92 ns |  0.99 |      40 B |        1.00 |
| Generate          | ShortRun   | 3              | 1           |   996.9 ns | 107.52 ns |  5.89 ns |  1.00 |      40 B |        1.00 |
